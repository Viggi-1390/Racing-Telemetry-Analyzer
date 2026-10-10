using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RacingTelemetryAnalyzer.Data;
using RacingTelemetryAnalyzer.Models;
using RacingTelemetryAnalyzer.Services;
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
                .Include(s => s.Condition)
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
                .Include(s => s.Condition)
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

            var analysisResults = await _context.AnalysisResults
                .AsNoTracking()
                .Where(a => lapIds.Contains(a.LapId))
                .Select(a => new
                {
                    a.AnalysisResultId,
                    a.LapId,
                    a.EntrySpeed,
                    a.MinimumSpeed,
                    a.ExitSpeed,
                    a.BrakingPercentage,
                    a.ThrottleResponseTime,
                    a.TimeDelta,
                    a.Recommendation,
                    a.RecommendationType
                })
                .ToListAsync();

            var condition = session.Condition != null ? new
            {
                airTemperature = session.Condition.AirTemperature,
                trackTemperature = session.Condition.TrackTemperature,
                weather = session.Condition.Weather,
                windSpeed = session.Condition.WindSpeed,
                trackCondition = session.Condition.TrackCondition
            } : null;

            return Ok(new
            {
                success = true,
                sessionId = session.SessionId,
                sessionName = session.Name,
                sessionType = session.SessionType ?? session.Name,
                date = session.Date,
                vehicle = new
                {
                    session.Vehicle?.VehicleId,
                    session.Vehicle?.Name,
                    session.Vehicle?.Manufacturer,
                    session.Vehicle?.Class,
                    session.Vehicle?.VehicleType,
                    session.Vehicle?.Power,
                    session.Vehicle?.TopSpeed,
                    session.Vehicle?.Weight,
                    session.Vehicle?.Engine,
                    session.Vehicle?.ImagePath
                },
                track = new
                {
                    session.Track?.TrackId,
                    session.Track?.Name,
                    session.Track?.Length,
                    session.Track?.Country,
                    session.Track?.NumberOfTurns,
                    session.Track?.ImagePath
                },
                condition,
                analysisResults,
                totalLaps = orderedLaps.Count,
                totalPoints = telemetryData.Count,
                laps = orderedLaps,
                data = telemetryData
            });
        }

        /// <summary>
        /// Lists sessions filtered strictly by vehicleId and trackId to guarantee isolation.
        /// </summary>
        [HttpGet("/api/sessions")]
        public async Task<IActionResult> GetSessions([FromQuery] int? vehicleId = null, [FromQuery] int? trackId = null)
        {
            var query = _context.Sessions
                .Include(s => s.Vehicle)
                .Include(s => s.Track)
                .Include(s => s.Laps)
                .AsNoTracking();

            if (vehicleId.HasValue && vehicleId.Value > 0)
            {
                query = query.Where(s => s.VehicleId == vehicleId.Value);
            }

            if (trackId.HasValue && trackId.Value > 0)
            {
                query = query.Where(s => s.TrackId == trackId.Value);
            }

            var sessions = await query
                .OrderByDescending(s => s.SessionId)
                .Select(s => new
                {
                    sessionId = s.SessionId,
                    name = s.Name,
                    sessionType = s.SessionType ?? s.Name,
                    date = s.Date,
                    vehicleId = s.VehicleId,
                    vehicleName = s.Vehicle != null ? s.Vehicle.Name : "",
                    trackId = s.TrackId,
                    trackName = s.Track != null ? s.Track.Name : "",
                    totalLaps = s.Laps.Count,
                    hasLaps = s.Laps.Any(),
                    bestLapTime = s.Laps.Where(l => l.IsValid).OrderBy(l => l.LapTime).Select(l => l.LapTime.ToString(@"mm\:ss\.fff")).FirstOrDefault() 
                                  ?? (s.Laps.Any() ? s.Laps.OrderBy(l => l.LapTime).Select(l => l.LapTime.ToString(@"mm\:ss\.fff")).FirstOrDefault() : "N/A"),
                    bestLapId = s.Laps.Where(l => l.IsValid).OrderBy(l => l.LapTime).Select(l => (int?)l.LapId).FirstOrDefault()
                                ?? (s.Laps.Any() ? s.Laps.OrderBy(l => l.LapTime).Select(l => (int?)l.LapId).FirstOrDefault() : null)
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                vehicleId,
                trackId,
                totalSessions = sessions.Count,
                sessions
            });
        }

        /// <summary>
        /// Explicit user-triggered synthetic telemetry generation for demo/testing workflows.
        /// Never called silently or automatically.
        /// </summary>
        [HttpPost("/api/telemetry/generate")]
        public async Task<IActionResult> GenerateDataApi(
            [FromBody] GenerateDataApiRequest? request,
            [FromServices] DataGeneratorService dataGenerator,
            [FromServices] ITelemetryAnalysisService analysisService)
        {
            if (request == null)
            {
                return BadRequest(new { success = false, message = "Request body is required." });
            }

            if (request.VehicleId <= 0 || request.TrackId <= 0)
            {
                return BadRequest(new { success = false, message = "Valid vehicleId and trackId are required." });
            }

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == request.VehicleId);
            if (vehicle == null)
            {
                return NotFound(new { success = false, message = $"Vehicle ID {request.VehicleId} not found." });
            }

            var track = await _context.Tracks.FirstOrDefaultAsync(t => t.TrackId == request.TrackId);
            if (track == null)
            {
                return NotFound(new { success = false, message = $"Track ID {request.TrackId} not found." });
            }

            var validatedSessionType = SessionTypeHelper.ValidateOrDefault(request.SessionType);

            var session = new Session
            {
                VehicleId = vehicle.VehicleId,
                TrackId = track.TrackId,
                Name = validatedSessionType,
                SessionType = validatedSessionType,
                Date = DateTime.UtcNow
            };
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            // Generate 3 laps for demo purposes
            for (int lapNum = 1; lapNum <= 3; lapNum++)
            {
                double baseTime = 102.0 - (lapNum * 0.5); // Laps get progressively faster
                var lap = new Lap
                {
                    SessionId = session.SessionId,
                    LapNumber = lapNum,
                    LapTime = TimeSpan.FromSeconds(baseTime),
                    Sector1Time = baseTime * 0.3,
                    Sector2Time = baseTime * 0.4,
                    Sector3Time = baseTime * 0.3,
                    IsValid = true
                };
                _context.Laps.Add(lap);
                await _context.SaveChangesAsync();

                var telemetry = dataGenerator.GenerateSyntheticData(lap.LapId, vehicle).ToList();

                _context.ChangeTracker.AutoDetectChangesEnabled = false;
                _context.TelemetryPoints.AddRange(telemetry);
                await _context.SaveChangesAsync();
                _context.ChangeTracker.AutoDetectChangesEnabled = true;

                var results = analysisService.AnalyzeLap(lap, telemetry).ToList();
                _context.AnalysisResults.AddRange(results);
                await _context.SaveChangesAsync();
            }

            return StatusCode(201, new
            {
                success = true,
                sessionId = session.SessionId,
                vehicleId = vehicle.VehicleId,
                trackId = track.TrackId,
                sessionType = validatedSessionType,
                totalLaps = 3,
                message = $"Successfully generated demo telemetry for {vehicle.Name} on {track.Name}."
            });
        }

        [HttpGet("/api/telemetry/import-progress")]
        public IActionResult ImportProgress([FromQuery] string token)
        {
            var progress = CsvImportService.GetProgress(token);
            return Ok(progress);
        }

        [HttpPost("/api/telemetry/import")]
        [RequestSizeLimit(250_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 250_000_000, ValueLengthLimit = 250_000_000)]
        public async Task<IActionResult> ImportCsvApi(
            IFormFile csvFile, 
            int vehicleId, 
            int trackId, 
            string? uploadToken,
            string? sessionType,
            [FromServices] CsvImportService csvService,
            [FromServices] ITelemetryAnalysisService analysisService)
        {
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == vehicleId);
            if (vehicle == null)
            {
                return BadRequest(new { success = false, message = "Vehicle not found." });
            }

            if (csvFile == null || csvFile.Length == 0)
            {
                return BadRequest(new { success = false, message = "No CSV file provided or file is empty." });
            }

            string token = string.IsNullOrWhiteSpace(uploadToken) ? Guid.NewGuid().ToString("N") : uploadToken;
            using var stream = csvFile.OpenReadStream();
            var progress = await csvService.StreamAndImportCsvAsync(
                stream, 
                vehicleId, 
                trackId, 
                _context, 
                analysisService, 
                token, 
                csvFile.Length,
                sessionType);

            if (!string.IsNullOrEmpty(progress.Error))
            {
                return StatusCode(500, new { success = false, message = progress.Error });
            }

            return Ok(new 
            { 
                success = true, 
                sessionId = progress.SessionId, 
                vehicleId = vehicleId, 
                trackId = trackId, 
                totalRows = progress.TotalRows, 
                totalLaps = progress.TotalLaps,
                redirectUrl = $"/Telemetry?vehicleId={vehicleId}&trackId={trackId}&sessionId={progress.SessionId}"
            });
        }
    }

    public class GenerateDataApiRequest
    {
        public int VehicleId { get; set; }
        public int TrackId { get; set; }
        public string? SessionType { get; set; }
    }
}
