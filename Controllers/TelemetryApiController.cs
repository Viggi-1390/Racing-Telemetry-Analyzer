using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RacingTelemetryAnalyzer.Data;
using RacingTelemetryAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RacingTelemetryAnalyzer.Controllers
{
    [ApiController]
    [Route("api/telemetry")]
    [Route("api/[controller]")]
    public class TelemetryApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TelemetryApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Loads ALL telemetry data upfront into memory for a given session.
        /// </summary>
        [HttpGet("session/{sessionId}")]
        public async Task<IActionResult> GetSessionTelemetry(int sessionId)
        {
            var session = await _context.Sessions
                .Include(s => s.Vehicle)
                .Include(s => s.Track)
                .Include(s => s.Laps)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
            {
                return NotFound(new { message = $"Session ID {sessionId} not found." });
            }

            return await BuildTelemetryResponse(session);
        }

        /// <summary>
        /// Loads latest telemetry session for a vehicle/track.
        /// Handles Audi R8 aliases (e.g. 2029 or 2032) and Monza track aliases (Track 1 or 5).
        /// </summary>
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestTelemetry([FromQuery] int? vehicleId = null, [FromQuery] int? trackId = null)
        {
            var query = _context.Sessions
                .Include(s => s.Vehicle)
                .Include(s => s.Track)
                .Include(s => s.Laps)
                .AsNoTracking();

            if (vehicleId.HasValue)
            {
                var v = await _context.Vehicles.FirstOrDefaultAsync(x => x.VehicleId == vehicleId.Value);
                if (v != null && (v.Name.Contains("R8") || v.Manufacturer.Contains("Audi")))
                {
                    var r8Ids = await _context.Vehicles
                        .Where(x => x.Name.Contains("R8") || x.Manufacturer.Contains("Audi"))
                        .Select(x => x.VehicleId)
                        .ToListAsync();
                    query = query.Where(s => r8Ids.Contains(s.VehicleId));
                }
                else
                {
                    query = query.Where(s => s.VehicleId == vehicleId.Value);
                }
            }

            if (trackId.HasValue)
            {
                var trk = await _context.Tracks.FirstOrDefaultAsync(t => t.TrackId == trackId.Value);
                if (trk != null && trk.Name.ToUpper().Contains("MONZA"))
                {
                    var monzaIds = await _context.Tracks.Where(t => t.Name.ToUpper().Contains("MONZA")).Select(t => t.TrackId).ToListAsync();
                    query = query.Where(s => monzaIds.Contains(s.TrackId));
                }
                else
                {
                    query = query.Where(s => s.TrackId == trackId.Value);
                }
            }

            var session = await query
                .Where(s => s.Laps.Any())
                .OrderByDescending(s => s.SessionId)
                .FirstOrDefaultAsync();

            if (session == null)
            {
                return NotFound(new { message = "No telemetry session found." });
            }

            return await BuildTelemetryResponse(session);
        }

        private async Task<IActionResult> BuildTelemetryResponse(Session session)
        {
            var lapDict = session.Laps.ToDictionary(l => l.LapId, l => l.LapNumber);
            var lapIds = lapDict.Keys.ToList();

            var rawPoints = await _context.TelemetryPoints
                .AsNoTracking()
                .Where(t => lapIds.Contains(t.LapId))
                .OrderBy(t => t.LapId)
                .ThenBy(t => t.Timestamp)
                .Select(t => new
                {
                    t.TelemetryPointId,
                    t.LapId,
                    Distance = t.Distance,
                    Speed = t.Speed,
                    RPM = t.RPM,
                    Gear = t.Gear,
                    Throttle = t.Throttle,
                    Brake = t.Brake,
                    Steering = t.Steering ?? (t.LeanAngle ?? 0.0),
                    Timestamp = t.Timestamp,
                    SuspensionFL = t.SuspensionFL,
                    SuspensionFR = t.SuspensionFR,
                    SuspensionRL = t.SuspensionRL,
                    SuspensionRR = t.SuspensionRR,
                    GLat = t.GLat,
                    GLon = t.GLon,
                    GVert = t.GVert
                })
                .ToListAsync();

            var orderedLaps = session.Laps.OrderBy(l => l.LapNumber).Select(l => new
            {
                LapNumber = l.LapNumber,
                LapId = l.LapId,
                LapTime = l.LapTime.ToString(@"mm\:ss\.fff"),
                LapTimeSec = l.LapTime.TotalSeconds,
                Sector1Time = l.Sector1Time > 0 ? l.Sector1Time.ToString("F3") : "N/A",
                Sector2Time = l.Sector2Time > 0 ? l.Sector2Time.ToString("F3") : "N/A",
                Sector3Time = l.Sector3Time > 0 ? l.Sector3Time.ToString("F3") : "N/A",
                IsValid = l.IsValid
            }).ToList();

            var telemetryData = rawPoints
                .OrderBy(t => lapDict.TryGetValue(t.LapId, out int ln) ? ln : 1)
                .ThenBy(t => t.Timestamp)
                .Select(t => new
                {
                    lap = lapDict.TryGetValue(t.LapId, out int ln) ? ln : 1,
                    lapId = t.LapId,
                    lapDist = t.Distance,
                    speed = t.Speed,
                    rpm = t.RPM,
                    gear = t.Gear,
                    thr = t.Throttle,
                    brk = t.Brake,
                    steer = t.Steering,
                    t = Math.Round(t.Timestamp.TotalSeconds, 3),
                    suspFL = t.SuspensionFL,
                    suspFR = t.SuspensionFR,
                    suspRL = t.SuspensionRL,
                    suspRR = t.SuspensionRR,
                    gLat = t.GLat,
                    gLon = t.GLon,
                    gVert = t.GVert
                })
                .ToList();

            return Ok(new
            {
                SessionId = session.SessionId,
                SessionName = session.Name,
                Vehicle = new { session.Vehicle?.VehicleId, session.Vehicle?.Name, session.Vehicle?.Manufacturer, session.Vehicle?.Class, session.Vehicle?.VehicleType },
                Track = new { session.Track?.TrackId, session.Track?.Name, session.Track?.Length },
                TotalLaps = orderedLaps.Count,
                TotalPoints = telemetryData.Count,
                Laps = orderedLaps,
                Data = telemetryData
            });
        }
    }
}
