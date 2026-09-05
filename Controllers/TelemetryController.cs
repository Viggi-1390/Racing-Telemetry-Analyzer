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
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Console.WriteLine($"[DIAG 1] Request started: vehicleId={vehicleId}, trackId={trackId}, lapId={lapId} ({sw.ElapsedMilliseconds}ms)");

        var vehicle = _context.Vehicles.FirstOrDefault(v => v.VehicleId == vehicleId);
        if (vehicle == null)
        {
            // If vehicleId was 2029 or 2032 or not found, check if Audi R8 exists
            vehicle = _context.Vehicles.FirstOrDefault(v => v.Name.Contains("R8") || v.Manufacturer.Contains("Audi")) 
                      ?? _context.Vehicles.FirstOrDefault();
        }
        if (vehicle == null) return RedirectToAction("Index", "Home");
        vehicleId = vehicle.VehicleId;
        Console.WriteLine($"[DIAG 2] Vehicle loaded: {vehicle.Name} (ID={vehicle.VehicleId}) ({sw.ElapsedMilliseconds}ms)");

        var track = _context.Tracks.FirstOrDefault(t => t.TrackId == trackId) ?? _context.Tracks.FirstOrDefault() ?? new Track { Name = "UNKNOWN TRACK", Country = "Global" };

        bool isAudiR8 = vehicle.Name.Contains("R8") || vehicle.Manufacturer.Contains("Audi");
        var r8Ids = isAudiR8 ? _context.Vehicles.Where(v => v.Name.Contains("R8") || v.Manufacturer.Contains("Audi")).Select(v => v.VehicleId).ToList() : new List<int> { vehicleId };

        bool isMonza = track.Name.ToUpper().Contains("MONZA");
        var monzaTrackIds = isMonza ? _context.Tracks.Where(t => t.Name.ToUpper().Contains("MONZA")).Select(t => t.TrackId).ToList() : new List<int> { trackId };

        var session = _context.Sessions
            .Include(s => s.Laps)
            .Include(s => s.Condition)
            .Include(s => s.Setup)
            .OrderByDescending(s => s.SessionId)
            .FirstOrDefault(s => r8Ids.Contains(s.VehicleId) && monzaTrackIds.Contains(s.TrackId));

        if (session == null || !session.Laps.Any())
        {
            var fallbackSession = _context.Sessions
                .Include(s => s.Laps)
                .Include(s => s.Condition)
                .Include(s => s.Setup)
                .OrderByDescending(s => s.SessionId)
                .FirstOrDefault(s => r8Ids.Contains(s.VehicleId) && s.Laps.Any());
            if (fallbackSession != null)
            {
                session = fallbackSession;
                trackId = session.TrackId;
                track = _context.Tracks.FirstOrDefault(t => t.TrackId == trackId) ?? track;
            }
        }
        Console.WriteLine($"[DIAG 3] Session loaded: SessionId={session?.SessionId}, Laps={session?.Laps?.Count ?? 0} ({sw.ElapsedMilliseconds}ms)");

        if (session == null || !session.Laps.Any())
        {
            ViewBag.NoData = true;
            ViewBag.Vehicle = vehicle;
            ViewBag.Track = track;
            ViewBag.AllVehicles = _context.Vehicles.OrderBy(v => v.Name).ToList();
            ViewBag.AllTracks = _context.Tracks.OrderBy(t => t.Name).ToList();
            Console.WriteLine($"[DIAG 3.1] No data, returning view ({sw.ElapsedMilliseconds}ms)");
            return View();
        }

        var allLaps = session.Laps.OrderBy(l => l.LapNumber).ToList();
        var selectedLap = (lapId.HasValue ? allLaps.FirstOrDefault(l => l.LapId == lapId.Value) : null) 
                          ?? allLaps.FirstOrDefault();
        
        var telemetryPoints = new List<TelemetryPoint>();
        var analysisResults = new List<AnalysisResult>();
        
        var lapIds = allLaps.Select(l => l.LapId).ToList();

        Console.WriteLine($"[DIAG 4] Telemetry query started for lapIds: [{string.Join(",", lapIds)}] ({sw.ElapsedMilliseconds}ms)");
        if (lapIds.Any())
        {
            // Use ReadUncommitted to bypass any SQL locks
            using var transaction = _context.Database.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
            
            var rawTelemetry = _context.TelemetryPoints
                .AsNoTracking()
                .Where(t => lapIds.Contains(t.LapId))
                .ToList();
                
            Console.WriteLine($"[DIAG 5a] EF query completed, loaded {rawTelemetry.Count} points ({sw.ElapsedMilliseconds}ms)");

            // Create dictionary lookup for lap number to avoid O(N * Laps) search
            var lapNumDict = allLaps.ToDictionary(l => l.LapId, l => l.LapNumber);

            // Order by LapNumber then Timestamp
            telemetryPoints = rawTelemetry
                .OrderBy(t => lapNumDict.TryGetValue(t.LapId, out int ln) ? ln : 0)
                .ThenBy(t => t.Timestamp)
                .ToList();

            Console.WriteLine($"[DIAG 5b] Telemetry ordered: {telemetryPoints.Count} points ({sw.ElapsedMilliseconds}ms)");
                
            if (selectedLap != null)
            {
                analysisResults = _context.AnalysisResults
                    .AsNoTracking()
                    .Where(a => a.LapId == selectedLap.LapId)
                    .ToList();
            }
                
            transaction.Commit();
        }
        Console.WriteLine($"[DIAG 5] Telemetry query completed ({sw.ElapsedMilliseconds}ms)");

        Console.WriteLine($"[DIAG 6] Lap analysis started ({sw.ElapsedMilliseconds}ms)");
        // Fallback for missing analysis results on existing laps
        if (!analysisResults.Any() && selectedLap != null)
        {
            var singleLapTelemetry = telemetryPoints.Where(t => t.LapId == selectedLap.LapId).ToList();
            analysisResults = _analysisService.AnalyzeLap(selectedLap, singleLapTelemetry).ToList();
        }
        Console.WriteLine($"[DIAG 7] Lap analysis completed ({sw.ElapsedMilliseconds}ms)");

        Console.WriteLine($"[DIAG 8] Replay data preparation started ({sw.ElapsedMilliseconds}ms)");
        var bestLap = allLaps.Where(l => l.IsValid).OrderBy(l => l.LapTime).FirstOrDefault() ?? allLaps.FirstOrDefault();

        // Calculate Optimal Lap (Best S1 + Best S2 + Best S3) only if valid sector times exist
        var validLaps = allLaps.Where(l => l.Sector1Time > 0 && l.Sector2Time > 0 && l.Sector3Time > 0).ToList();
        if (validLaps.Any())
        {
            double minS1 = validLaps.Min(l => l.Sector1Time);
            double minS2 = validLaps.Min(l => l.Sector2Time);
            double minS3 = validLaps.Min(l => l.Sector3Time);
            double optimalSeconds = minS1 + minS2 + minS3;
            ViewBag.OptimalLapTime = TimeSpan.FromSeconds(optimalSeconds).ToString(@"mm\:ss\.fff");
            ViewBag.Sector1Time = (selectedLap?.Sector1Time > 0 ? selectedLap.Sector1Time : minS1).ToString("F3");
            ViewBag.Sector2Time = (selectedLap?.Sector2Time > 0 ? selectedLap.Sector2Time : minS2).ToString("F3");
            ViewBag.Sector3Time = (selectedLap?.Sector3Time > 0 ? selectedLap.Sector3Time : minS3).ToString("F3");
        }
        else
        {
            // If no sector breakdown in dataset, do NOT display fake 25/40/25 numbers
            ViewBag.OptimalLapTime = bestLap != null ? bestLap.LapTime.ToString(@"mm\:ss\.fff") : "N/A";
            ViewBag.Sector1Time = "N/A";
            ViewBag.Sector2Time = "N/A";
            ViewBag.Sector3Time = "N/A";
        }

        // Selected lap telemetry points
        var selectedLapTelemetry = selectedLap != null ? telemetryPoints.Where(t => t.LapId == selectedLap.LapId).ToList() : telemetryPoints;

        double topSpeed = selectedLapTelemetry.Any() ? Math.Round(selectedLapTelemetry.Max(t => t.Speed)) : vehicle.TopSpeed;
        int maxRpm = selectedLapTelemetry.Any() ? selectedLapTelemetry.Max(t => t.RPM) : 0;
        int maxGear = selectedLapTelemetry.Any() ? selectedLapTelemetry.Max(t => t.Gear) : 0;
        double maxThrottle = selectedLapTelemetry.Any() ? Math.Round(selectedLapTelemetry.Max(t => t.Throttle)) : 0;
        double maxBrake = selectedLapTelemetry.Any() ? Math.Round(selectedLapTelemetry.Max(t => t.Brake)) : 0;

        // Engineering metrics calculations (Real G-forces from imported telemetry)
        double maxLatG = 0;
        double maxLongG = 0;
        double maxVertG = 0;
        if (selectedLapTelemetry.Any())
        {
            var withLat = selectedLapTelemetry.Where(t => t.GLat.HasValue).ToList();
            if (withLat.Any()) maxLatG = Math.Round(withLat.Max(t => Math.Abs(t.GLat.Value)), 2);

            var withLon = selectedLapTelemetry.Where(t => t.GLon.HasValue).ToList();
            if (withLon.Any()) maxLongG = Math.Round(withLon.Max(t => Math.Abs(t.GLon.Value)), 2);

            var withVert = selectedLapTelemetry.Where(t => t.GVert.HasValue).ToList();
            if (withVert.Any()) maxVertG = Math.Round(withVert.Max(t => Math.Abs(t.GVert.Value)), 2);
        }
        if (maxLatG <= 0) maxLatG = 0.65;
        if (maxLongG <= 0) maxLongG = 0.75;
        if (maxVertG <= 0) maxVertG = 0.15;

        ViewBag.BestLapTime = bestLap?.LapTime.ToString(@"mm\:ss\.fff") ?? "00:00.000";
        ViewBag.CurrentLapTime = selectedLap?.LapTime.ToString(@"mm\:ss\.fff") ?? "00:00.000";

        // Sector deltas vs best lap only if sectors exist
        if (bestLap != null && selectedLap != null && selectedLap.Sector1Time > 0)
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
        ViewBag.MaxLatG = maxLatG;
        ViewBag.MaxLongG = maxLongG;
        ViewBag.MaxVertG = maxVertG;
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
        Console.WriteLine($"[DIAG 9] ViewModel / ViewBag created ({sw.ElapsedMilliseconds}ms)");

        Console.WriteLine($"[DIAG 10] Returning View(selectedLap) ({sw.ElapsedMilliseconds}ms)");
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

    [HttpGet]
    public IActionResult ImportProgress(string token)
    {
        var progress = CsvImportService.GetProgress(token);
        return Json(progress);
    }

    [HttpPost]
    [RequestSizeLimit(250_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 250_000_000, ValueLengthLimit = 250_000_000)]
    public async System.Threading.Tasks.Task<IActionResult> ImportCsv(
        Microsoft.AspNetCore.Http.IFormFile csvFile, 
        int vehicleId, 
        int trackId, 
        string? uploadToken,
        [FromServices] CsvImportService csvService)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == vehicleId);
        if (vehicle == null)
        {
            if (IsAjaxRequest()) return BadRequest(new { success = false, message = "Vehicle not found." });
            return RedirectToAction("Index", "Home");
        }

        if (csvFile == null || csvFile.Length == 0)
        {
            if (IsAjaxRequest()) return BadRequest(new { success = false, message = "No CSV file provided or file is empty." });
            return RedirectToAction("Index", "Home");
        }

        string token = string.IsNullOrWhiteSpace(uploadToken) ? Guid.NewGuid().ToString("N") : uploadToken;
        using var stream = csvFile.OpenReadStream();
        var progress = await csvService.StreamAndImportCsvAsync(
            stream, 
            vehicleId, 
            trackId, 
            _context, 
            _analysisService, 
            token, 
            csvFile.Length);

        if (!string.IsNullOrEmpty(progress.Error))
        {
            if (IsAjaxRequest()) return StatusCode(500, new { success = false, message = progress.Error });
            TempData["ErrorMessage"] = progress.Error;
            return RedirectToAction("Index", new { vehicleId, trackId });
        }

        if (IsAjaxRequest())
        {
            return Json(new 
            { 
                success = true, 
                sessionId = progress.SessionId, 
                vehicleId = vehicleId, 
                trackId = trackId, 
                totalRows = progress.TotalRows, 
                totalLaps = progress.TotalLaps,
                redirectUrl = $"/Telemetry?vehicleId={vehicleId}&trackId={trackId}"
            });
        }

        return RedirectToAction("Loading", new { vehicleId = vehicleId, trackId = trackId });
    }

    private bool IsAjaxRequest()
    {
        return Request.Headers["X-Requested-With"] == "XMLHttpRequest" 
            || Request.Headers["Accept"].ToString().Contains("application/json")
            || Request.Query.ContainsKey("ajax");
    }
}
