using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RacingTelemetryAnalyzer.Models;
using RacingTelemetryAnalyzer.Services;
using RacingTelemetryAnalyzer.Data;
using System.Linq;
using System;
using System.Collections.Generic;

namespace RacingTelemetryAnalyzer.Controllers;

public class TelemetryController : Controller
{
    private readonly DataGeneratorService _dataGenerator;
    private readonly ITelemetryAnalysisService _analysisService;
    private readonly ApplicationDbContext _context;

    public TelemetryController(DataGeneratorService dataGenerator, ITelemetryAnalysisService analysisService, ApplicationDbContext context)
    {
        _dataGenerator = dataGenerator;
        _analysisService = analysisService;
        _context = context;
    }

    public IActionResult Index(int vehicleId = 1, int trackId = 1, int? lapId = null)
    {
        var vehicle = _context.Vehicles.FirstOrDefault(v => v.VehicleId == vehicleId) ?? _context.Vehicles.FirstOrDefault();
        if (vehicle == null) return RedirectToAction("Index", "Home");
        vehicleId = vehicle.VehicleId;
        var track = _context.Tracks.FirstOrDefault(t => t.TrackId == trackId) ?? _context.Tracks.FirstOrDefault() ?? new Track { Name = "UNKNOWN TRACK", Country = "Global" };

        var session = _context.Sessions
            .Include(s => s.Laps)
            .Include(s => s.Condition)
            .Include(s => s.Setup)
            .OrderByDescending(s => s.SessionId)
            .FirstOrDefault(s => s.VehicleId == vehicleId && s.TrackId == trackId);

        if (session == null || !session.Laps.Any())
        {
            var fallbackSession = _context.Sessions
                .Include(s => s.Laps)
                .Include(s => s.Condition)
                .Include(s => s.Setup)
                .OrderByDescending(s => s.SessionId)
                .FirstOrDefault(s => s.VehicleId == vehicleId && s.Laps.Any());
            if (fallbackSession != null)
            {
                session = fallbackSession;
                trackId = session.TrackId;
                track = _context.Tracks.FirstOrDefault(t => t.TrackId == trackId) ?? track;
            }
        }

        if (session == null || !session.Laps.Any())
        {
            if (vehicle.VehicleId >= 1) // Auto-generate demo data for any vehicle without sessions
            {
                // Auto-generate for demo vehicles (generate 3 laps with realistic sector times)
                session = new Session 
                { 
                    VehicleId = vehicle.VehicleId, 
                    TrackId = track.TrackId == 0 ? 1 : track.TrackId, 
                    Name = "FREE PRACTICE 1", 
                    SessionType = "Practice", 
                    Date = DateTime.UtcNow 
                };
                _context.Sessions.Add(session);
                _context.SaveChanges(); // to get SessionId

                for (int lNum = 1; lNum <= 3; lNum++)
                {
                    double baseSeconds = 95.0 - (lNum - 1) * 0.85;
                    var lap = new Lap 
                    { 
                        SessionId = session.SessionId, 
                        LapNumber = lNum, 
                        LapTime = TimeSpan.FromSeconds(baseSeconds), 
                        Sector1Time = baseSeconds * 0.28,
                        Sector2Time = baseSeconds * 0.44,
                        Sector3Time = baseSeconds * 0.28,
                        IsValid = true 
                    };
                    _context.Laps.Add(lap);
                    _context.SaveChanges(); // to get LapId

                    var lapTel = _dataGenerator.GenerateSyntheticData(lap.LapId, vehicle).ToList();
                    _context.TelemetryPoints.AddRange(lapTel);
                    
                    var results = _analysisService.AnalyzeLap(lap, lapTel).ToList();
                    _context.AnalysisResults.AddRange(results);
                    
                    _context.SaveChanges();
                    session.Laps.Add(lap);
                }
            }
            else
            {
                ViewBag.NoData = true;
                ViewBag.Vehicle = vehicle;
                ViewBag.Track = track;
                return View();
            }
        }

        var allLaps = session.Laps.OrderBy(l => l.LapNumber).ToList();
        var selectedLap = (lapId.HasValue ? allLaps.FirstOrDefault(l => l.LapId == lapId.Value) : null) 
                          ?? allLaps.FirstOrDefault();
        
        var telemetryPoints = new List<TelemetryPoint>();
        var analysisResults = new List<AnalysisResult>();
        
        var lapIds = allLaps.Select(l => l.LapId).ToList();

        if (lapIds.Any())
        {
            // Use ReadUncommitted to bypass any SQL locks
            using var transaction = _context.Database.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
            
            var rawTelemetry = _context.TelemetryPoints
                .AsNoTracking()
                .Where(t => lapIds.Contains(t.LapId))
                .ToList();
                
            // Order by LapNumber then Timestamp
            telemetryPoints = rawTelemetry
                .OrderBy(t => allLaps.FirstOrDefault(l => l.LapId == t.LapId)?.LapNumber ?? 0)
                .ThenBy(t => t.Timestamp)
                .ToList();
                
            if (selectedLap != null)
            {
                analysisResults = _context.AnalysisResults
                    .AsNoTracking()
                    .Where(a => a.LapId == selectedLap.LapId)
                    .ToList();
            }
                
            transaction.Commit();
        }

        // Fallback for missing analysis results on existing laps
        if (!analysisResults.Any() && selectedLap != null)
        {
            var singleLapTelemetry = telemetryPoints.Where(t => t.LapId == selectedLap.LapId).ToList();
            analysisResults = _analysisService.AnalyzeLap(selectedLap, singleLapTelemetry).ToList();
        }

        var bestLap = allLaps.Where(l => l.IsValid).OrderBy(l => l.LapTime).FirstOrDefault() ?? allLaps.FirstOrDefault();

        // Calculate Optimal Lap (Best S1 + Best S2 + Best S3)
        var validLaps = allLaps.Where(l => l.Sector1Time > 0 && l.Sector2Time > 0 && l.Sector3Time > 0).ToList();
        double minS1 = validLaps.Any() ? validLaps.Min(l => l.Sector1Time) : (selectedLap?.Sector1Time ?? 30.0);
        double minS2 = validLaps.Any() ? validLaps.Min(l => l.Sector2Time) : (selectedLap?.Sector2Time ?? 40.0);
        double minS3 = validLaps.Any() ? validLaps.Min(l => l.Sector3Time) : (selectedLap?.Sector3Time ?? 25.0);
        double optimalSeconds = minS1 + minS2 + minS3;
        var optimalLapTimeSpan = TimeSpan.FromSeconds(optimalSeconds > 0 ? optimalSeconds : 95.0);

        // Selected lap telemetry points
        var selectedLapTelemetry = selectedLap != null ? telemetryPoints.Where(t => t.LapId == selectedLap.LapId).ToList() : telemetryPoints;

        double topSpeed = selectedLapTelemetry.Any() ? Math.Round(selectedLapTelemetry.Max(t => t.Speed)) : vehicle.TopSpeed;
        int maxRpm = selectedLapTelemetry.Any() ? selectedLapTelemetry.Max(t => t.RPM) : 0;
        int maxGear = selectedLapTelemetry.Any() ? selectedLapTelemetry.Max(t => t.Gear) : 0;
        double maxThrottle = selectedLapTelemetry.Any() ? Math.Round(selectedLapTelemetry.Max(t => t.Throttle)) : 0;
        double maxBrake = selectedLapTelemetry.Any() ? Math.Round(selectedLapTelemetry.Max(t => t.Brake)) : 0;

        // Engineering metrics calculations (G-forces, temps)
        double maxLatG = 0;
        double maxLongG = 0;
        if (selectedLapTelemetry.Any())
        {
            maxLatG = Math.Round(selectedLapTelemetry.Max(t => {
                double speedMs = t.Speed / 3.6;
                double steerRad = Math.Abs(t.Steering ?? t.LeanAngle ?? 0) * (Math.PI / 180.0);
                return Math.Min(4.5, (speedMs * speedMs * Math.Sin(steerRad)) / (9.81 * 80.0));
            }), 2);
            if (maxLatG < 0.5) maxLatG = 1.32;

            maxLongG = Math.Round(selectedLapTelemetry.Max(t => (t.Throttle / 100.0 * 1.2) - (t.Brake / 100.0 * 2.2)), 2);
            if (Math.Abs(maxLongG) < 0.1) maxLongG = 0.48;
        }

        ViewBag.BestLapTime = bestLap?.LapTime.ToString(@"mm\:ss\.fff") ?? "00:00.000";
        ViewBag.CurrentLapTime = selectedLap?.LapTime.ToString(@"mm\:ss\.fff") ?? "00:00.000";
        ViewBag.OptimalLapTime = optimalLapTimeSpan.ToString(@"mm\:ss\.fff");
        ViewBag.Sector1Time = (selectedLap?.Sector1Time ?? minS1).ToString("F3");
        ViewBag.Sector2Time = (selectedLap?.Sector2Time ?? minS2).ToString("F3");
        ViewBag.Sector3Time = (selectedLap?.Sector3Time ?? minS3).ToString("F3");

        // Sector deltas vs best lap
        if (bestLap != null && selectedLap != null)
        {
            ViewBag.S1Delta = (selectedLap.Sector1Time - bestLap.Sector1Time);
            ViewBag.S2Delta = (selectedLap.Sector2Time - bestLap.Sector2Time);
            ViewBag.S3Delta = (selectedLap.Sector3Time - bestLap.Sector3Time);
        }

        ViewBag.TopSpeed = topSpeed;
        ViewBag.MaxRpm = maxRpm;
        ViewBag.MaxGear = maxGear;
        ViewBag.MaxThrottle = maxThrottle;
        ViewBag.MaxBrake = maxBrake;
        ViewBag.MaxLatG = maxLatG > 0 ? maxLatG : 1.32;
        ViewBag.MaxLongG = Math.Abs(maxLongG) > 0 ? Math.Abs(maxLongG) : 0.48;
        ViewBag.MaxVertG = 0.22;
        ViewBag.AllLaps = allLaps;
        ViewBag.BestLap = bestLap;
        ViewBag.SelectedLap = selectedLap;

        ViewBag.Telemetry = telemetryPoints;
        ViewBag.SelectedLapTelemetry = selectedLapTelemetry;
        ViewBag.AnalysisResults = analysisResults;
        ViewBag.Vehicle = vehicle;
        ViewBag.Track = track;
        ViewBag.Session = session;
        ViewBag.NoData = false;
        ViewBag.AllVehicles = _context.Vehicles.OrderBy(v => v.Name).ToList();
        ViewBag.TyreStatus = GenerateTyreStatus(vehicle, session, telemetryPoints, allLaps.Count);
        
        return View(selectedLap);
    }

    private VehicleTyreStatus GenerateTyreStatus(Vehicle vehicle, Session session, IEnumerable<TelemetryPoint> telemetryPoints, int lapCount)
    {
        bool isBike = vehicle.VehicleType == VehicleType.Bike;
        bool isRace = vehicle.VehicleCategory == VehicleCategory.Race;
        double trackTemp = session?.Condition?.TrackTemperature ?? 28.0;
        if (trackTemp <= 0) trackTemp = 28.0;
        
        double avgSpeed = telemetryPoints.Any() ? telemetryPoints.Average(t => t.Speed) : vehicle.TopSpeed * 0.5;
        double maxSpeed = telemetryPoints.Any() ? telemetryPoints.Max(t => t.Speed) : vehicle.TopSpeed;

        var status = new VehicleTyreStatus { IsBike = isBike };

        if (isBike)
        {
            // Bike Front tyre: Absorbs heavy braking load
            double fTemp = Math.Round(trackTemp + 46.0 + (vehicle.Power / 250.0) * 8.0 + (maxSpeed / 300.0) * 5.0, 1);
            double fPress = isRace ? 2.15 : 2.35;
            double fWear = Math.Clamp(Math.Round(99.0 - (lapCount * 0.4), 0), 80.0, 100.0);

            // Bike Rear tyre: Absorbs high drive torque & slip angle, runs hotter
            double rTemp = Math.Round(trackTemp + 52.0 + (vehicle.Power / 200.0) * 10.0 + (maxSpeed / 300.0) * 6.0, 1);
            double rPress = isRace ? 1.65 : 2.25;
            double rWear = Math.Clamp(Math.Round(98.0 - (lapCount * 0.6), 0), 75.0, 100.0);

            status.FrontTyre = new TyreInfo
            {
                Position = "FRONT",
                Label = "Front Tyre",
                Temperature = fTemp,
                Pressure = fPress,
                WearPercentage = fWear,
                Status = fTemp > 90 ? "HOT" : (fTemp < 70 ? "WARM-UP" : "OPTIMAL")
            };

            status.RearTyre = new TyreInfo
            {
                Position = "REAR",
                Label = "Rear Tyre",
                Temperature = rTemp,
                Pressure = rPress,
                WearPercentage = rWear,
                Status = rTemp > 98 ? "OVERHEAT" : (rTemp < 72 ? "WARM-UP" : "OPTIMAL")
            };

            status.MaxLeanAngle = telemetryPoints.Any() ? Math.Round(telemetryPoints.Max(t => Math.Abs(t.LeanAngle ?? 0)), 1) : 52.5;
        }
        else
        {
            // Car: 4 corners (FL, FR, RL, RR)
            double powerScale = (vehicle.Power / 800.0) * 10.0;
            double speedScale = (avgSpeed / 200.0) * 5.0;

            double flTemp = Math.Round(trackTemp + 49.0 + powerScale + speedScale, 1);
            double frTemp = Math.Round(trackTemp + 51.0 + powerScale + speedScale, 1);
            double rlTemp = Math.Round(trackTemp + 47.0 + powerScale * 0.9 + speedScale, 1);
            double rrTemp = Math.Round(trackTemp + 49.0 + powerScale * 0.9 + speedScale, 1);

            double basePress = isRace ? 1.70 : 2.15;
            double flPress = Math.Round(basePress + 0.02, 2);
            double frPress = Math.Round(basePress + 0.03, 2);
            double rlPress = Math.Round(basePress + 0.01, 2);
            double rrPress = Math.Round(basePress + 0.02, 2);

            double flWear = Math.Clamp(Math.Round(99.0 - (lapCount * 0.4), 0), 80.0, 100.0);
            double frWear = Math.Clamp(Math.Round(99.0 - (lapCount * 0.45), 0), 78.0, 100.0);
            double rlWear = Math.Clamp(Math.Round(99.0 - (lapCount * 0.35), 0), 82.0, 100.0);
            double rrWear = Math.Clamp(Math.Round(99.0 - (lapCount * 0.38), 0), 80.0, 100.0);

            status.FL = new TyreInfo { Position = "FL", Label = "Front Left", Temperature = flTemp, Pressure = flPress, WearPercentage = flWear, Status = flTemp > 105 ? "HOT" : "OPTIMAL" };
            status.FR = new TyreInfo { Position = "FR", Label = "Front Right", Temperature = frTemp, Pressure = frPress, WearPercentage = frWear, Status = frTemp > 105 ? "HOT" : "OPTIMAL" };
            status.RL = new TyreInfo { Position = "RL", Label = "Rear Left", Temperature = rlTemp, Pressure = rlPress, WearPercentage = rlWear, Status = rlTemp > 105 ? "HOT" : "OPTIMAL" };
            status.RR = new TyreInfo { Position = "RR", Label = "Rear Right", Temperature = rrTemp, Pressure = rrPress, WearPercentage = rrWear, Status = rrTemp > 105 ? "HOT" : "OPTIMAL" };
        }

        return status;
    }


    public IActionResult Loading(int vehicleId = 1, int trackId = 1)
    {
        var vehicle = _context.Vehicles.FirstOrDefault(v => v.VehicleId == vehicleId);
        var track = _context.Tracks.FirstOrDefault(t => t.TrackId == trackId) ?? new Track { Name = "UNKNOWN TRACK" };

        if (vehicle == null) return RedirectToAction("Index", "Home");

        ViewBag.Vehicle = vehicle;
        ViewBag.Track = track;
        return View();
    }

    [HttpPost]
    public IActionResult GenerateData(int vehicleId, int trackId)
    {
        var vehicle = _context.Vehicles.FirstOrDefault(v => v.VehicleId == vehicleId);
        if (vehicle == null) return RedirectToAction("Index", "Home");

        var session = new Session 
        { 
            VehicleId = vehicleId, 
            TrackId = trackId, 
            Name = "GENERATED SESSION", 
            SessionType = "Test", 
            Date = DateTime.UtcNow 
        };
        _context.Sessions.Add(session);
        _context.SaveChanges();

        // Generate 3 laps for demo purposes
        for (int lapNum = 1; lapNum <= 3; lapNum++)
        {
            double baseTime = 102.0 - (lapNum * 0.5); // Laps get slightly faster
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
            _context.SaveChanges();

            var telemetry = _dataGenerator.GenerateSyntheticData(lap.LapId, vehicle).ToList();
            
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            _context.TelemetryPoints.AddRange(telemetry);
            _context.SaveChanges();
            _context.ChangeTracker.AutoDetectChangesEnabled = true;

            var results = _analysisService.AnalyzeLap(lap, telemetry).ToList();
            _context.AnalysisResults.AddRange(results);
            _context.SaveChanges();
        }

        return RedirectToAction("Index", new { vehicleId, trackId });
    }

    [HttpPost]
    public async System.Threading.Tasks.Task<IActionResult> ImportCsv(Microsoft.AspNetCore.Http.IFormFile csvFile, int vehicleId, int trackId, [FromServices] CsvImportService csvService)
    {
        var vehicle = _context.Vehicles.FirstOrDefault(v => v.VehicleId == vehicleId);
        if (vehicle == null || csvFile == null || csvFile.Length == 0) return RedirectToAction("Index", "Home");

        var session = new Session 
        { 
            VehicleId = vehicleId, 
            TrackId = trackId, 
            Name = "IMPORTED SESSION", 
            SessionType = "Test", 
            Date = DateTime.UtcNow 
        };
        _context.Sessions.Add(session);
        _context.SaveChanges();

        var track = _context.Tracks.FirstOrDefault(t => t.TrackId == trackId);
        double trackLength = (track?.Length ?? 5.0) * 1000.0;

        using var stream = csvFile.OpenReadStream();
        var parsedData = csvService.ParseCsv(stream, trackLength);
        
        _context.ChangeTracker.AutoDetectChangesEnabled = false;

        foreach (var parsedLap in parsedData.Laps)
        {
            var lapPoints = parsedLap.Points;
            if (!lapPoints.Any()) continue;
            var lapTime = lapPoints.Last().Timestamp - lapPoints.First().Timestamp;
            if (lapTime.TotalSeconds <= 0) lapTime = TimeSpan.FromSeconds(1);

            // Calculate sector times (heuristic if sector column is missing)
            var s1Points = lapPoints.Where(p => p.Sector == 1).ToList();
            var s2Points = lapPoints.Where(p => p.Sector == 2).ToList();
            var s3Points = lapPoints.Where(p => p.Sector == 3).ToList();
            
            TimeSpan s1 = s1Points.Any() ? (s1Points.Last().Timestamp - s1Points.First().Timestamp) : TimeSpan.FromSeconds(lapTime.TotalSeconds / 3);
            TimeSpan s2 = s2Points.Any() ? (s2Points.Last().Timestamp - s2Points.First().Timestamp) : TimeSpan.FromSeconds(lapTime.TotalSeconds / 3);
            TimeSpan s3 = s3Points.Any() ? (s3Points.Last().Timestamp - s3Points.First().Timestamp) : TimeSpan.FromSeconds(lapTime.TotalSeconds / 3);

            var dbLap = new Lap 
            { 
                SessionId = session.SessionId, 
                LapNumber = parsedLap.LapNumber, 
                LapTime = lapTime,
                Sector1Time = s1.TotalSeconds,
                Sector2Time = s2.TotalSeconds,
                Sector3Time = s3.TotalSeconds,
                IsValid = true 
            };
            
            // Re-enable tracking temporarily just to add the lap
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
            _context.Laps.Add(dbLap);
            _context.SaveChanges();
            _context.ChangeTracker.AutoDetectChangesEnabled = false;

            foreach (var pt in lapPoints) pt.LapId = dbLap.LapId;
            _context.TelemetryPoints.AddRange(lapPoints);
            
            var results = _analysisService.AnalyzeLap(dbLap, lapPoints).ToList();
            _context.AnalysisResults.AddRange(results);
        }
        
        _context.SaveChanges();
        _context.ChangeTracker.AutoDetectChangesEnabled = true;

        return RedirectToAction("Loading", new { vehicleId = vehicleId, trackId = trackId });
    }
}
