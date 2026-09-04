using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using RacingTelemetryAnalyzer.Models;

namespace RacingTelemetryAnalyzer.Services;

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

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;
            var vals = line.Split(",");
            
            double ParseDoubleAlias(params string[] names) {
                foreach (var n in names) if (colMap.ContainsKey(n) && vals.Length > colMap[n] && double.TryParse(vals[colMap[n]], NumberStyles.Any, CultureInfo.InvariantCulture, out double v)) return v;
                return 0;
            }
            double? ParseNullableDoubleAlias(params string[] names) {
                foreach (var n in names) if (colMap.ContainsKey(n) && vals.Length > colMap[n] && double.TryParse(vals[colMap[n]], NumberStyles.Any, CultureInfo.InvariantCulture, out double v)) return v;
                return null;
            }
            int ParseIntAlias(params string[] names) {
                foreach (var n in names) if (colMap.ContainsKey(n) && vals.Length > colMap[n] && int.TryParse(vals[colMap[n]], out int v)) return v;
                return 0;
            }

            int lapNum = ParseIntAlias("LapNumber", "Lap", "Lap Num", "Lap_Number", "LapNo");

            var tp = new TelemetryPoint
            {
                Timestamp = TimeSpan.FromSeconds(ParseDoubleAlias("Timestamp", "Time")),
                Distance = ParseDoubleAlias("Distance", "Dist"),
                Speed = ParseDoubleAlias("Speed", "Vel"),
                RPM = ParseIntAlias("RPM", "EngineRPM", "EngineSpeed"),
                Gear = ParseIntAlias("Gear", "nGear", "GearPosition"),
                Throttle = ParseDoubleAlias("Throttle", "ThrottlePos", "Gas"),
                Brake = ParseDoubleAlias("Brake", "BrakePos", "BrakePressure", "Brk"),
                Sector = ParseIntAlias("Sector", "Sec", "SectorNum") == 0 ? 1 : ParseIntAlias("Sector", "Sec", "SectorNum"),
                Steering = ParseNullableDoubleAlias("Steering", "Steer", "SteerAngle"),
                SuspensionFL = ParseNullableDoubleAlias("SuspensionFL", "SuspFL", "Suspension"),
                SuspensionFR = ParseNullableDoubleAlias("SuspensionFR", "SuspFR"),
                SuspensionRL = ParseNullableDoubleAlias("SuspensionRL", "SuspRL"),
                SuspensionRR = ParseNullableDoubleAlias("SuspensionRR", "SuspRR"),
                LeanAngle = ParseNullableDoubleAlias("LeanAngle", "Lean")
            };
            
            rawPoints.Add((lapNum, tp));
        }

        if (!rawPoints.Any()) return result;

        // Phase 2: Fix missing Timestamps
        if (rawPoints.All(r => r.Point.Timestamp.TotalSeconds == 0))
        {
            for (int i = 0; i < rawPoints.Count; i++)
            {
                rawPoints[i].Point.Timestamp = TimeSpan.FromSeconds(i * 0.1); // assume 10Hz
            }
        }

        // Phase 3: Fix missing Distances
        if (rawPoints.All(r => r.Point.Distance == 0))
        {
            double currentDist = 0;
            for (int i = 1; i < rawPoints.Count; i++)
            {
                var dt = (rawPoints[i].Point.Timestamp - rawPoints[i - 1].Point.Timestamp).TotalSeconds;
                var speedMs = rawPoints[i - 1].Point.Speed * (1000.0 / 3600.0);
                currentDist += speedMs * dt;
                rawPoints[i].Point.Distance = currentDist;
            }
        }

        // Phase 4: Fix Lap Boundaries
        if (rawPoints.All(r => r.LapNum <= 1))
        {
            int currentLap = 1;
            double distanceAccumulator = 0;
            double lastDist = 0;
            int lastSector = 1;

            for (int i = 0; i < rawPoints.Count; i++)
            {
                var pt = rawPoints[i].Point;
                
                // Distance reset (real distance column dropping)
                if (lastDist > 1000 && pt.Distance < 500) {
                    currentLap++;
                    distanceAccumulator = 0;
                }
                else if (lastSector == 3 && pt.Sector == 1) {
                    currentLap++;
                }
                else {
                    double delta = pt.Distance - lastDist;
                    if (delta > 0) distanceAccumulator += delta;
                    
                    if (distanceAccumulator >= trackLengthMeters) {
                        currentLap++;
                        distanceAccumulator = distanceAccumulator % trackLengthMeters;
                    }
                }

                lastDist = pt.Distance;
                lastSector = pt.Sector;
                rawPoints[i] = (currentLap, pt);
            }
        }

        // Phase 5: Group and Create Result
        var grouped = rawPoints.GroupBy(x => x.LapNum).OrderBy(g => g.Key);
        foreach (var group in grouped)
        {
            var allPts = group.Select(g => g.Point).OrderBy(p => p.Timestamp).ToList();

            result.Laps.Add(new ParsedLap
            {
                LapNumber = group.Key,
                Points = allPts
            });
        }

        return result;
    }
}

