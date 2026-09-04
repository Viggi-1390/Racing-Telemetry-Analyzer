using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RacingTelemetryAnalyzer.Data;
using RacingTelemetryAnalyzer.Models;

namespace RacingTelemetryAnalyzer.Controllers
{
    [ApiController]
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

            var lapIds = session.Laps.Select(l => l.LapId).ToList();

            var telemetryPoints = await _context.TelemetryPoints
                .AsNoTracking()
                .Where(t => lapIds.Contains(t.LapId))
                .OrderBy(t => t.LapId)
                .ThenBy(t => t.Timestamp)
                .Select(t => new
                {
                    t.TelemetryPointId,
                    t.LapId,
                    LapNumber = _context.Laps.Where(l => l.LapId == t.LapId).Select(l => l.LapNumber).FirstOrDefault(),
                    Distance = t.Distance,
                    Speed = t.Speed,
                    RPM = t.RPM,
                    Throttle = t.Throttle,
                    Brake = t.Brake,
                    Steering = t.Steering ?? 0.0,
                    LeanAngle = t.LeanAngle ?? 0.0,
                    TimestampSec = t.Timestamp.TotalSeconds
                })
                .ToListAsync();

            return Ok(new
            {
                SessionId = session.SessionId,
                SessionName = session.Name,
                Vehicle = new { session.Vehicle.VehicleId, session.Vehicle.Name, session.Vehicle.Class, session.Vehicle.VehicleType },
                Track = new { session.Track.TrackId, session.Track.Name, session.Track.Length },
                TotalLaps = session.Laps.Count,
                TotalPoints = telemetryPoints.Count,
                Data = telemetryPoints
            });
        }
    }
}
