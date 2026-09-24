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
        /// Strictly validates vehicleId and trackId if provided to prevent cross-vehicle/cross-track leaks.
        /// </summary>
        [HttpGet("session/{sessionId:int}")]
        public async Task<IActionResult> GetSessionTelemetry(int sessionId, [FromQuery] int? vehicleId = null, [FromQuery] int? trackId = null)
        {
            var session = await _context.Sessions
                .Include(s => s.Vehicle)
                .Include(s => s.Track)
                .Include(s => s.Laps)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null || !session.Laps.Any())
            {
                return NotFound(new { success = false, message = $"Session ID {sessionId} not found or contains no laps." });
            }

            if (vehicleId.HasValue && session.VehicleId != vehicleId.Value)
            {
                return NotFound(new { success = false, message = $"Session {sessionId} does not belong to vehicle {vehicleId.Value}." });
            }

            if (trackId.HasValue && session.TrackId != trackId.Value)
            {
                return NotFound(new { success = false, message = $"Session {sessionId} does not belong to track {trackId.Value}." });
            }

            return await BuildTelemetryResponse(session);
        }

        /// <summary>
        /// Loads latest telemetry session strictly for a specific vehicle and/or track.
        /// Data-driven without vehicle or track specific aliases.
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
                query = query.Where(s => s.VehicleId == vehicleId.Value);
            }

            if (trackId.HasValue)
            {
                query = query.Where(s => s.TrackId == trackId.Value);
            }

            var session = await query
                .Where(s => s.Laps.Any())
                .OrderByDescending(s => s.SessionId)
                .FirstOrDefaultAsync();

            if (session == null)
            {
                return NotFound(new { success = false, message = "No telemetry session found for the requested vehicle and track." });
            }

            return await BuildTelemetryResponse(session);
        }

        private async Task<IActionResult> BuildTelemetryResponse(Session session)
        {
            var lapDict = session.Laps.ToDictionary(l => l.LapId, l => l.LapNumber);
            var lapIds = lapDict.Keys.ToList();

            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadUncommitted);

            var rawPoints = await _context.TelemetryPoints
                .AsNoTracking()
                .Where(t => lapIds.Contains(t.LapId))
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

            await transaction.CommitAsync();

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
                success = true,
                sessionId = session.SessionId,
                sessionName = session.Name,
                vehicle = new { session.Vehicle?.VehicleId, session.Vehicle?.Name, session.Vehicle?.Manufacturer, session.Vehicle?.Class, session.Vehicle?.VehicleType },
                track = new { session.Track?.TrackId, session.Track?.Name, session.Track?.Length },
                totalLaps = orderedLaps.Count,
                totalPoints = telemetryData.Count,
                laps = orderedLaps,
                data = telemetryData
            });
        }
    }
}
