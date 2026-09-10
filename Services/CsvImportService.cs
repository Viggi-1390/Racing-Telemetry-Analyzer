using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RacingTelemetryAnalyzer.Data;
using RacingTelemetryAnalyzer.Models;

namespace RacingTelemetryAnalyzer.Services;

public class ImportProgress
{
    public string Token { get; set; } = "";
    public int RowsProcessed { get; set; }
    public int TotalRows { get; set; }
    public int Percent => TotalRows > 0 ? (int)Math.Min(100, Math.Round((double)RowsProcessed / TotalRows * 100)) : 0;
    public string Status { get; set; } = "Initializing...";
    public bool IsComplete { get; set; }
    public int? SessionId { get; set; }
    public int? VehicleId { get; set; }
    public int TotalLaps { get; set; }
    public string? Error { get; set; }
}

public class CsvImportResult
{
    public List<ParsedLap> Laps { get; set; } = new List<ParsedLap>();
}

public class ParsedLap
{
    public int LapNumber { get; set; }
    public List<TelemetryPoint> Points { get; set; } = new List<TelemetryPoint>();
}

public class CsvImportService
{
    public static readonly ConcurrentDictionary<string, ImportProgress> ActiveProgress = new();

    public static ImportProgress GetProgress(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return new ImportProgress { Status = "Invalid token" };
        return ActiveProgress.TryGetValue(token, out var p) ? p : new ImportProgress { Token = token, Status = "Not found" };
    }

    /// <summary>
    /// High-performance streaming importer for large CSV files (80MB+, 63,000+ rows).
    /// Streams line-by-line, flushes in 1,000-row batches, and clears EF Core ChangeTracker
    /// to avoid memory exhaustion and SQL command timeouts.
    /// </summary>
    public async Task<ImportProgress> StreamAndImportCsvAsync(
        Stream csvStream,
        int vehicleId,
        int trackId,
        ApplicationDbContext context,
        ITelemetryAnalysisService analysisService,
        string token,
        long fileSizeBytes = 0,
        string? sessionType = null)
    {
        var progress = new ImportProgress
        {
            Token = token,
            VehicleId = vehicleId,
            Status = "Parsing headers and initializing session...",
            TotalRows = fileSizeBytes > 0 ? (int)Math.Max(500, fileSizeBytes / 880) : 63274
        };
        ActiveProgress[token] = progress;

        try
        {
            var vehicle = await context.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == vehicleId);
            if (vehicle == null)
            {
                progress.Error = $"Vehicle ID {vehicleId} not found.";
                progress.Status = "Failed: Vehicle not found.";
                return progress;
            }

            var track = await context.Tracks.FirstOrDefaultAsync(t => t.TrackId == trackId)
                        ?? await context.Tracks.FirstOrDefaultAsync()
                        ?? new Track { TrackId = 1, Length = 5.793 };

            double trackLengthMeters = (track.Length > 0 ? track.Length : 5.0) * 1000.0;

            var validatedSessionType = RacingTelemetryAnalyzer.Models.SessionTypeHelper.ValidateOrDefault(sessionType);

            // 1. Create Telemetry Session
            var session = new Session
            {
                VehicleId = vehicleId,
                TrackId = track.TrackId == 0 ? 1 : track.TrackId,
                Name = validatedSessionType,
                SessionType = validatedSessionType,
                Date = DateTime.UtcNow
            };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();
            progress.SessionId = session.SessionId;

            // 2. Read Header Line and Build Column Map
            using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 65536);
            var headerLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                progress.Error = "Uploaded CSV file is empty.";
                progress.Status = "Failed: Empty file.";
                return progress;
            }

            var headers = headerLine.Split(',');
            var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                colMap[headers[i].Trim()] = i;
            }

            // Helper to find column index from multiple possible aliases
            int FindCol(params string[] aliases)
            {
                foreach (var a in aliases)
                {
                    if (colMap.TryGetValue(a, out int idx)) return idx;
                }
                return -1;
            }

            int idxLapCompleted = FindCol("completed_laps", "completedlaps");
            int idxLap = FindCol("lap", "lapnumber", "lap_number", "lapnum", "lapno", "lap_no", "LapNumber");
            int idxTime = FindCol("timestamp", "time", "wall_time", "session_time", "Time", "Timestamp");
            int idxDist = FindCol("distance", "dist", "Distance", "Dist");
            int idxLapProg = FindCol("lap_progress", "lapprogress", "lap_ratio");
            int idxSpeed = FindCol("speed_kmh", "speed", "vel", "velocity", "Speed", "SpeedKmh");
            int idxRpm = FindCol("rpms", "rpm", "engine_rpm", "engine_speed", "RPM", "EngineRPM");
            int idxGear = FindCol("gear", "gears", "ngear", "Gear", "GearPosition");
            int idxThrottle = FindCol("throttle", "throttle_pos", "gas", "Throttle", "ThrottlePos");
            int idxBrake = FindCol("brake", "brake_pos", "brakepressure", "brk", "Brake", "BrakePos");
            int idxSteer = FindCol("steer_angle", "steer", "steering", "Steering", "SteerAngle");
            int idxSector = FindCol("sector", "sec", "sectornum", "Sector", "SectorNum");
            int idxLean = FindCol("leanangle", "lean_angle", "lean", "LeanAngle");
            int idxSuspFL = FindCol("susp_fl", "suspensionfl", "SuspensionFL");
            int idxSuspFR = FindCol("susp_fr", "suspensionfr", "SuspensionFR");
            int idxSuspRL = FindCol("susp_rl", "suspensionrl", "SuspensionRL");
            int idxSuspRR = FindCol("susp_rr", "suspensionrr", "SuspensionRR");
            int idxGLat = FindCol("g_lat", "lat_g", "GLat", "LatG");
            int idxGLon = FindCol("g_lon", "long_g", "lon_g", "GLon", "LongG");
            int idxGVert = FindCol("g_vert", "vert_g", "GVert", "VertG");
            int idxPosX = FindCol("pos_x", "PosX", "x");
            int idxPosZ = FindCol("pos_z", "PosZ", "z");

            // 3. Streaming Variables
            var lapCache = new Dictionary<int, Lap>();
            var lapTimeBounds = new Dictionary<int, (double firstTime, double lastTime, double minSpeed, double maxSpeed, double maxBrake, double maxThrottle, double maxLean)>();
            
            var batch = new List<TelemetryPoint>(1000);
            const int BatchSize = 1000;
            int rowCount = 0;
            double prevTimestamp = 0;
            double sessionStartTimestamp = 0;
            double totalCumulativeDistance = 0;
            double lapStartCumulativeDist = 0;
            double prevLapDistance = 0;
            bool isFirstRow = true;
            bool autoScaleThrottleBrake = true;
            bool autoScaleSuspension = false;
            bool checkedSuspensionScale = false;

            // Spatial lap detection variables
            int currentLapNum = 1;
            double refGateX = 0, refGateZ = 0;
            double lastLapGateDist = 0;
            bool refGateInitialized = false;
            double prevX = 0, prevZ = 0;
            bool hasPrevPos = false;

            progress.Status = "Importing & processing telemetry rows...";

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;
                var vals = line.Split(',');
                rowCount++;

                double GetDouble(int idx, double def = 0.0)
                {
                    if (idx >= 0 && idx < vals.Length && double.TryParse(vals[idx], NumberStyles.Any, CultureInfo.InvariantCulture, out double v))
                        return v;
                    return def;
                }

                double? GetNullableDouble(int idx)
                {
                    if (idx >= 0 && idx < vals.Length && double.TryParse(vals[idx], NumberStyles.Any, CultureInfo.InvariantCulture, out double v))
                        return v;
                    return null;
                }

                int GetInt(int idx, int def = 0)
                {
                    if (idx >= 0 && idx < vals.Length)
                    {
                        if (int.TryParse(vals[idx], NumberStyles.Any, CultureInfo.InvariantCulture, out int iv)) return iv;
                        if (double.TryParse(vals[idx], NumberStyles.Any, CultureInfo.InvariantCulture, out double dv)) return (int)Math.Round(dv);
                    }
                    return def;
                }

                // Timestamps & Delta
                double rawTime = GetDouble(idxTime, isFirstRow ? 0.0 : prevTimestamp + 0.016);
                if (isFirstRow)
                {
                    sessionStartTimestamp = rawTime;
                    prevTimestamp = rawTime;
                    isFirstRow = false;
                }
                double dt = Math.Max(0.001, Math.Min(1.0, rawTime - prevTimestamp));
                prevTimestamp = rawTime;
                double normalizedSessionTime = rawTime - sessionStartTimestamp;

                // Speed
                double speed = Math.Max(0.0, GetDouble(idxSpeed, 0.0));

                // Positions & Step Distance
                double posX = GetDouble(idxPosX, 0.0);
                double posZ = GetDouble(idxPosZ, 0.0);
                double stepDist = 0.0;

                if (idxPosX >= 0 && idxPosZ >= 0)
                {
                    if (hasPrevPos)
                    {
                        double dx = posX - prevX;
                        double dz = posZ - prevZ;
                        stepDist = Math.Sqrt(dx * dx + dz * dz);
                    }
                    else
                    {
                        hasPrevPos = true;
                    }
                    prevX = posX;
                    prevZ = posZ;
                }
                else
                {
                    stepDist = (speed / 3.6) * dt;
                }
                totalCumulativeDistance += stepDist;

                // Multi-lap Detection:
                // 1) First check explicit lap columns (lap > 1 or completed_laps > 0)
                int explicitLapNum = -1;
                if (idxLapCompleted >= 0)
                {
                    int comp = GetInt(idxLapCompleted, -1);
                    if (comp > 0) explicitLapNum = comp + 1;
                }
                if (explicitLapNum <= 0 && idxLap >= 0)
                {
                    int l = GetInt(idxLap, -1);
                    if (l > 1) explicitLapNum = l;
                }

                if (explicitLapNum > 0)
                {
                    if (explicitLapNum != currentLapNum)
                    {
                        currentLapNum = explicitLapNum;
                        lapStartCumulativeDist = totalCumulativeDistance;
                    }
                }
                else
                {
                    // 2) Spatial gate detection if pos_x & pos_z exist
                    if (idxPosX >= 0 && idxPosZ >= 0)
                    {
                        if (!refGateInitialized && speed > 80.0 && totalCumulativeDistance > 50.0)
                        {
                            refGateX = posX;
                            refGateZ = posZ;
                            refGateInitialized = true;
                            lastLapGateDist = totalCumulativeDistance;
                        }
                        else if (refGateInitialized)
                        {
                            double distSinceLastGate = totalCumulativeDistance - lastLapGateDist;
                            if (distSinceLastGate > 2500.0)
                            {
                                double distToGate = Math.Sqrt(Math.Pow(posX - refGateX, 2) + Math.Pow(posZ - refGateZ, 2));
                                if (distToGate < 25.0)
                                {
                                    currentLapNum++;
                                    lastLapGateDist = totalCumulativeDistance;
                                    lapStartCumulativeDist = totalCumulativeDistance;
                                }
                            }
                        }
                    }
                    else
                    {
                        // 3) Cumulative distance fallback (wrap around trackLengthMeters)
                        if (totalCumulativeDistance - lapStartCumulativeDist >= trackLengthMeters && trackLengthMeters > 500)
                        {
                            currentLapNum++;
                            lapStartCumulativeDist = totalCumulativeDistance;
                        }
                    }
                }

                // Resolve or create Lap entity
                if (!lapCache.TryGetValue(currentLapNum, out var currentLap))
                {
                    currentLap = new Lap
                    {
                        SessionId = session.SessionId,
                        LapNumber = currentLapNum,
                        LapTime = TimeSpan.FromSeconds(90.0), // Updated at end
                        Sector1Time = 0,
                        Sector2Time = 0,
                        Sector3Time = 0,
                        IsValid = true
                    };
                    context.Laps.Add(currentLap);
                    await context.SaveChangesAsync();
                    lapCache[currentLapNum] = currentLap;
                    lapTimeBounds[currentLapNum] = (double.MaxValue, double.MinValue, double.MaxValue, double.MinValue, 0, 0, 0);
                }

                // Throttle & Brake with intelligent scaling (0..1 -> 0..100%)
                double rawThrottle = GetDouble(idxThrottle, 0.0);
                double rawBrake = GetDouble(idxBrake, 0.0);

                if (autoScaleThrottleBrake && (rawThrottle > 1.05 || rawBrake > 1.05))
                {
                    autoScaleThrottleBrake = false; // Source data is already 0..100%
                }

                double throttle = autoScaleThrottleBrake ? Math.Min(100.0, Math.Max(0.0, rawThrottle * 100.0)) : Math.Min(100.0, Math.Max(0.0, rawThrottle));
                double brake = autoScaleThrottleBrake ? Math.Min(100.0, Math.Max(0.0, rawBrake * 100.0)) : Math.Min(100.0, Math.Max(0.0, rawBrake));

                // Per-lap Distance calculation (resetting to 0 at the start of every lap)
                double lapDist = 0.0;
                if (idxDist >= 0 && explicitLapNum > 0)
                {
                    lapDist = GetDouble(idxDist, 0.0);
                }
                else
                {
                    lapDist = Math.Max(0.0, totalCumulativeDistance - lapStartCumulativeDist);
                }

                // Suspension check: meters vs millimeters
                // If values are e.g. 0.095, scale * 1000 to 95 mm
                if (!checkedSuspensionScale && idxSuspFL >= 0)
                {
                    double? flCheck = GetNullableDouble(idxSuspFL);
                    if (flCheck.HasValue && Math.Abs(flCheck.Value) > 0.0001 && Math.Abs(flCheck.Value) < 2.0)
                    {
                        autoScaleSuspension = true;
                    }
                    checkedSuspensionScale = true;
                }

                double? ScaleSusp(int idx)
                {
                    double? v = GetNullableDouble(idx);
                    if (v.HasValue && autoScaleSuspension) return Math.Round(v.Value * 1000.0, 1);
                    return v.HasValue ? Math.Round(v.Value, 1) : null;
                }

                // Sector
                int sector = GetInt(idxSector, 0);
                if (sector < 1 || sector > 3)
                {
                    // Derive approximate sector based on lap progress if not explicitly provided
                    double lapProgress = trackLengthMeters > 0 ? (lapDist / trackLengthMeters) : 0;
                    sector = lapProgress < 0.33 ? 1 : (lapProgress < 0.66 ? 2 : 3);
                }

                var pt = new TelemetryPoint
                {
                    LapId = currentLap.LapId,
                    Timestamp = TimeSpan.FromSeconds(normalizedSessionTime),
                    Distance = Math.Round(lapDist, 2),
                    Speed = Math.Round(speed, 2),
                    RPM = GetInt(idxRpm, 3000),
                    Gear = GetInt(idxGear, 1),
                    Throttle = Math.Round(throttle, 1),
                    Brake = Math.Round(brake, 1),
                    Sector = sector,
                    Steering = GetNullableDouble(idxSteer),
                    LeanAngle = GetNullableDouble(idxLean),
                    SuspensionFL = ScaleSusp(idxSuspFL),
                    SuspensionFR = ScaleSusp(idxSuspFR),
                    SuspensionRL = ScaleSusp(idxSuspRL),
                    SuspensionRR = ScaleSusp(idxSuspRR),
                    GLat = GetNullableDouble(idxGLat),
                    GLon = GetNullableDouble(idxGLon),
                    GVert = GetNullableDouble(idxGVert),
                    PosX = idxPosX >= 0 ? posX : null,
                    PosZ = idxPosZ >= 0 ? posZ : null
                };

                // Track lap statistics for metrics
                var bounds = lapTimeBounds[currentLapNum];
                lapTimeBounds[currentLapNum] = (
                    Math.Min(bounds.firstTime, normalizedSessionTime),
                    Math.Max(bounds.lastTime, normalizedSessionTime),
                    Math.Min(bounds.minSpeed, speed),
                    Math.Max(bounds.maxSpeed, speed),
                    Math.Max(bounds.maxBrake, brake),
                    Math.Max(bounds.maxThrottle, throttle),
                    Math.Max(bounds.maxLean, pt.LeanAngle ?? 0.0)
                );

                batch.Add(pt);

                // Flush batch to database when limit reached
                if (batch.Count >= BatchSize)
                {
                    context.ChangeTracker.AutoDetectChangesEnabled = false;
                    context.TelemetryPoints.AddRange(batch);
                    await context.SaveChangesAsync();
                    context.ChangeTracker.Clear();
                    batch.Clear();

                    progress.RowsProcessed = rowCount;
                    if (progress.TotalRows < rowCount) progress.TotalRows = rowCount + 1000;
                }
            }

            // Flush final batch
            if (batch.Count > 0)
            {
                context.ChangeTracker.AutoDetectChangesEnabled = false;
                context.TelemetryPoints.AddRange(batch);
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();
                batch.Clear();
            }

            progress.TotalRows = rowCount;
            progress.RowsProcessed = rowCount;
            progress.TotalLaps = lapCache.Count;
            progress.Status = "Finalizing lap times and telemetry analysis...";

            // 4. Handle trailing incomplete in-lap (e.g. car entered pit lane with only a few meters/seconds)
            if (lapCache.Count > 1)
            {
                int maxLapNum = lapCache.Keys.Max();
                if (lapTimeBounds.TryGetValue(maxLapNum, out var lastLapBounds))
                {
                    double lastLapDur = lastLapBounds.lastTime - lastLapBounds.firstTime;
                    if (lastLapDur < 15.0)
                    {
                        // Incomplete trailing in-lap: merge into previous lap
                        int prevLapNum = maxLapNum - 1;
                        var incompleteLap = lapCache[maxLapNum];
                        var prevLap = lapCache[prevLapNum];

                        // Reassign points directly in SQL (no entities loaded into ChangeTracker)
                        await context.TelemetryPoints
                            .Where(tp => tp.LapId == incompleteLap.LapId)
                            .ExecuteUpdateAsync(s => s.SetProperty(tp => tp.LapId, prevLap.LapId));

                        // Update previous lap bounds
                        if (lapTimeBounds.TryGetValue(prevLapNum, out var prevBounds))
                        {
                            lapTimeBounds[prevLapNum] = (
                                prevBounds.firstTime,
                                lastLapBounds.lastTime,
                                Math.Min(prevBounds.minSpeed, lastLapBounds.minSpeed),
                                Math.Max(prevBounds.maxSpeed, lastLapBounds.maxSpeed),
                                Math.Max(prevBounds.maxBrake, lastLapBounds.maxBrake),
                                Math.Max(prevBounds.maxThrottle, lastLapBounds.maxThrottle),
                                Math.Max(prevBounds.maxLean, lastLapBounds.maxLean)
                            );
                        }

                        // Delete incomplete lap directly in SQL (no ChangeTracker involvement)
                        await context.Laps
                            .Where(l => l.LapId == incompleteLap.LapId)
                            .ExecuteDeleteAsync();

                        lapCache.Remove(maxLapNum);
                        lapTimeBounds.Remove(maxLapNum);
                    }
                }
            }

            // 5. Finalize Lap Times & Recommendations (WITHOUT recursive Update(lap))
            context.ChangeTracker.Clear();
            context.ChangeTracker.AutoDetectChangesEnabled = true;
            var analysisResultsToAdd = new List<AnalysisResult>();

            foreach (var kvp in lapCache)
            {
                int lapNum = kvp.Key;
                var lap = kvp.Value;
                if (lapTimeBounds.TryGetValue(lapNum, out var bounds) && bounds.lastTime > bounds.firstTime)
                {
                    double duration = bounds.lastTime - bounds.firstTime;
                    var computedLapTime = TimeSpan.FromSeconds(Math.Max(1.0, duration));

                    // Re-query the Lap by primary key to track ONLY the Lap scalar entity (never attaches TelemetryPoints)
                    var trackedLap = await context.Laps.FindAsync(lap.LapId);
                    if (trackedLap != null)
                    {
                        trackedLap.LapTime = computedLapTime;
                        trackedLap.Sector1Time = 0;
                        trackedLap.Sector2Time = 0;
                        trackedLap.Sector3Time = 0;
                        trackedLap.IsValid = true;

                        context.Entry(trackedLap).Property(p => p.LapTime).IsModified = true;
                        context.Entry(trackedLap).Property(p => p.Sector1Time).IsModified = true;
                        context.Entry(trackedLap).Property(p => p.Sector2Time).IsModified = true;
                        context.Entry(trackedLap).Property(p => p.Sector3Time).IsModified = true;
                        context.Entry(trackedLap).Property(p => p.IsValid).IsModified = true;
                    }

                    // Add summary Analysis Result
                    analysisResultsToAdd.Add(new AnalysisResult
                    {
                        LapId = lap.LapId,
                        Recommendation = bounds.maxBrake > 85 ? "Optimal brake threshold achieved." : "Braking potential can be improved into heavy braking zones.",
                        RecommendationType = "Braking",
                        BrakingPercentage = bounds.maxBrake,
                        MinimumSpeed = bounds.minSpeed,
                        ExitSpeed = bounds.maxSpeed * 0.8
                    });
                }
            }

            if (analysisResultsToAdd.Count > 0)
            {
                context.AnalysisResults.AddRange(analysisResultsToAdd);
            }

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            progress.IsComplete = true;
            progress.Status = $"Import successful! {rowCount:N0} telemetry points across {lapCache.Count} laps processed.";
            return progress;
        }
        catch (Exception ex)
        {
            progress.Error = ex.Message;
            progress.Status = "Import failed: " + ex.Message;
            return progress;
        }
        finally
        {
            context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }

    /// <summary>
    /// Legacy in-memory parser preserved for backward compatibility with small files/tests.
    /// </summary>
    public CsvImportResult ParseCsv(Stream csvStream, double trackLengthMeters = 5000)
    {
        var result = new CsvImportResult();
        var rawPoints = new List<(int LapNum, TelemetryPoint Point)>();

        using var reader = new StreamReader(csvStream);
        var headerLine = reader.ReadLine();
        if (headerLine == null) return result;
        
        var headers = headerLine.Split(",");
        var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
        {
            colMap[headers[i].Trim()] = i;
        }

        int FindCol(params string[] aliases)
        {
            foreach (var a in aliases)
            {
                if (colMap.TryGetValue(a, out int idx)) return idx;
            }
            return -1;
        }

        int idxLapCompleted = FindCol("completed_laps", "completedlaps");
        int idxLap = FindCol("lap", "lapnumber", "lap_number", "lapnum", "lapno", "LapNumber");
        int idxTime = FindCol("timestamp", "time", "wall_time", "Time", "Timestamp");
        int idxDist = FindCol("distance", "dist", "Distance", "Dist");
        int idxLapProg = FindCol("lap_progress", "lapprogress");
        int idxSpeed = FindCol("speed_kmh", "speed", "vel", "velocity", "Speed");
        int idxRpm = FindCol("rpms", "rpm", "engine_rpm", "engine_speed", "RPM", "EngineRPM");
        int idxGear = FindCol("gear", "gears", "ngear", "Gear", "GearPosition");
        int idxThrottle = FindCol("throttle", "throttle_pos", "gas", "Throttle", "ThrottlePos");
        int idxBrake = FindCol("brake", "brake_pos", "brakepressure", "brk", "Brake", "BrakePos");
        int idxSteer = FindCol("steer_angle", "steer", "steering", "Steering", "SteerAngle");
        int idxSector = FindCol("sector", "sec", "sectornum", "Sector", "SectorNum");

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;
            var vals = line.Split(",");

            double GetDouble(int idx, double def = 0.0)
            {
                if (idx >= 0 && idx < vals.Length && double.TryParse(vals[idx], NumberStyles.Any, CultureInfo.InvariantCulture, out double v))
                    return v;
                return def;
            }

            int GetInt(int idx, int def = 0)
            {
                if (idx >= 0 && idx < vals.Length)
                {
                    if (int.TryParse(vals[idx], NumberStyles.Any, CultureInfo.InvariantCulture, out int iv)) return iv;
                    if (double.TryParse(vals[idx], NumberStyles.Any, CultureInfo.InvariantCulture, out double dv)) return (int)Math.Round(dv);
                }
                return def;
            }

            int lapNum = 1;
            if (idxLapCompleted >= 0) lapNum = GetInt(idxLapCompleted, 0) + 1;
            else if (idxLap >= 0) lapNum = Math.Max(1, GetInt(idxLap, 1));

            double rawThrottle = GetDouble(idxThrottle, 0.0);
            double rawBrake = GetDouble(idxBrake, 0.0);
            double throttle = rawThrottle <= 1.05 && rawThrottle > 0 ? rawThrottle * 100.0 : rawThrottle;
            double brake = rawBrake <= 1.05 && rawBrake > 0 ? rawBrake * 100.0 : rawBrake;

            var tp = new TelemetryPoint
            {
                Timestamp = TimeSpan.FromSeconds(GetDouble(idxTime, 0.0)),
                Distance = idxDist >= 0 ? GetDouble(idxDist, 0.0) : (idxLapProg >= 0 ? GetDouble(idxLapProg, 0.0) * trackLengthMeters : 0.0),
                Speed = Math.Max(0.0, GetDouble(idxSpeed, 0.0)),
                RPM = GetInt(idxRpm, 0),
                Gear = GetInt(idxGear, 1),
                Throttle = throttle,
                Brake = brake,
                Sector = Math.Max(1, Math.Min(3, GetInt(idxSector, 1))),
                Steering = GetDouble(idxSteer, 0.0)
            };

            rawPoints.Add((lapNum, tp));
        }

        var grouped = rawPoints.GroupBy(x => x.LapNum).OrderBy(g => g.Key);
        foreach (var group in grouped)
        {
            result.Laps.Add(new ParsedLap
            {
                LapNumber = group.Key,
                Points = group.Select(g => g.Point).OrderBy(p => p.Timestamp).ToList()
            });
        }

        return result;
    }
}


