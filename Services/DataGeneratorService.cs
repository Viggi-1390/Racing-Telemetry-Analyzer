using System;
using System.Collections.Generic;
using System.Linq;
using RacingTelemetryAnalyzer.Models;

namespace RacingTelemetryAnalyzer.Services;

public class DataGeneratorService
{
    // ──────────────────────────────────────────────────────
    // Vehicle profile: configures all physics limits per car/bike
    // ──────────────────────────────────────────────────────
    private class VehicleProfile
    {
        public double MaxSpeed;       // km/h
        public double MinCornerSpeed; // km/h at tightest hairpin
        public int    MaxRPM;
        public int    IdleRPM;
        public int    MaxGear;
        public double MaxSteeringAngle; // degrees (car) or handlebar (bike)
        public double MaxLeanAngle;     // bikes only
        public double BrakeGMax;        // max decel G
        public double AccelGMax;        // max accel G
        public double LatGMax;          // max lateral G
        public double SuspNominal;      // mm nominal travel
        public double SuspRange;        // mm +/- from nominal
        public bool   IsBike;
    }

    // ──────────────────────────────────────────────────────
    // Track section descriptor
    // ──────────────────────────────────────────────────────
    private enum SectionType { Straight, Braking, Corner, CornerExit, Chicane }

    private class TrackSegment
    {
        public SectionType Type;
        public double Length;        // meters
        public double TargetSpeed;   // km/h at end/through this section
        public double SteeringSign;  // -1 left, +1 right, 0 straight
        public double CornerSeverity;// 0-1, how tight (1 = hairpin)
    }

    // ──────────────────────────────────────────────────────
    // PUBLIC ENTRY POINT
    // ──────────────────────────────────────────────────────
    public IEnumerable<TelemetryPoint> GenerateSyntheticData(int lapId, Vehicle vehicle)
    {
        var profile = BuildProfile(vehicle);
        var track = BuildTrackLayout(profile);
        var points = SimulateLap(lapId, vehicle, profile, track);
        return points;
    }

    // ──────────────────────────────────────────────────────
    // BUILD VEHICLE PROFILE from DB model
    // ──────────────────────────────────────────────────────
    private VehicleProfile BuildProfile(Vehicle vehicle)
    {
        bool isBike = vehicle.VehicleType == VehicleType.Bike;
        string name = (vehicle.Name ?? "").ToUpper();
        double topSpeed = vehicle.TopSpeed > 0 ? vehicle.TopSpeed : (isBike ? 300 : 330);
        int power = vehicle.Power > 0 ? vehicle.Power : (isBike ? 200 : 700);
        int weight = vehicle.Weight > 0 ? vehicle.Weight : (isBike ? 200 : 1100);

        // Determine character from name/class
        bool isF1 = name.Contains("F1") || (vehicle.Class ?? "").ToUpper().Contains("F1");
        bool isLMP = name.Contains("963") || name.Contains("499P") || name.Contains("HYBRID") ||
                     name.Contains("LMH") || name.Contains("HYPERCAR");
        bool isGT = name.Contains("GT3") || name.Contains("GT4") || name.Contains("STO") ||
                    name.Contains("750S") || name.Contains("HURACAN");
        bool isSuperBike = isBike && (name.Contains("1000") || name.Contains("1390") ||
                           name.Contains("DUKE") || name.Contains("8C"));

        var p = new VehicleProfile
        {
            IsBike = isBike,
            MaxSpeed = Math.Min(topSpeed, isBike ? 340 : 370),
            MinCornerSpeed = isF1 ? 80 : (isLMP ? 70 : (isGT ? 55 : (isBike ? 45 : 40))),
            IdleRPM = isF1 ? 4000 : (isBike ? 2500 : 1500),
            MaxRPM = isF1 ? 15000 : (isLMP ? 10000 : (isGT ? 9000 : (isSuperBike ? 14000 : (isBike ? 12000 : 8000)))),
            MaxGear = isF1 ? 8 : (isBike ? 6 : (isLMP ? 7 : 6)),
            MaxSteeringAngle = isF1 ? 18 : (isLMP ? 22 : (isGT ? 28 : (isBike ? 5 : 35))),
            MaxLeanAngle = isBike ? 55 : 0,
            BrakeGMax = isF1 ? 5.5 : (isLMP ? 3.8 : (isGT ? 2.5 : (isBike ? 1.8 : 1.5))),
            AccelGMax = isF1 ? 2.0 : (isLMP ? 1.5 : (isGT ? 1.0 : (isBike ? 1.2 : 0.7))),
            LatGMax = isF1 ? 5.0 : (isLMP ? 3.5 : (isGT ? 2.2 : (isBike ? 1.6 : 1.3))),
            SuspNominal = isBike ? 110 : 50,
            SuspRange = isBike ? 20 : 8,
        };
        return p;
    }

    // ──────────────────────────────────────────────────────
    // BUILD A SYNTHETIC TRACK LAYOUT (~4.3 km circuit)
    // ──────────────────────────────────────────────────────
    private List<TrackSegment> BuildTrackLayout(VehicleProfile profile)
    {
        var segments = new List<TrackSegment>();
        double speedHi = profile.MaxSpeed * 0.92;
        double speedMid = profile.MaxSpeed * 0.65;
        double speedLo = profile.MinCornerSpeed * 1.3;
        double speedHairpin = profile.MinCornerSpeed;

        // Section 1: Start/finish straight — full speed
        segments.Add(new TrackSegment { Type = SectionType.Straight,    Length = 550, TargetSpeed = speedHi,         SteeringSign = 0,  CornerSeverity = 0 });
        // Section 2: Heavy braking into Turn 1
        segments.Add(new TrackSegment { Type = SectionType.Braking,     Length = 180, TargetSpeed = speedLo,         SteeringSign = 0,  CornerSeverity = 0 });
        // Section 3: Turn 1 — medium right
        segments.Add(new TrackSegment { Type = SectionType.Corner,      Length = 140, TargetSpeed = speedLo,         SteeringSign = 1,  CornerSeverity = 0.65 });
        // Section 4: Short accel out of T1
        segments.Add(new TrackSegment { Type = SectionType.CornerExit,  Length = 200, TargetSpeed = speedMid,        SteeringSign = 0,  CornerSeverity = 0 });
        // Section 5: Braking into hairpin
        segments.Add(new TrackSegment { Type = SectionType.Braking,     Length = 150, TargetSpeed = speedHairpin,    SteeringSign = 0,  CornerSeverity = 0 });
        // Section 6: Hairpin — tight left
        segments.Add(new TrackSegment { Type = SectionType.Corner,      Length = 100, TargetSpeed = speedHairpin,    SteeringSign = -1, CornerSeverity = 0.95 });
        // Section 7: Acceleration out of hairpin
        segments.Add(new TrackSegment { Type = SectionType.CornerExit,  Length = 350, TargetSpeed = speedHi * 0.85,  SteeringSign = 0,  CornerSeverity = 0 });
        // Section 8: Fast sweeping right
        segments.Add(new TrackSegment { Type = SectionType.Corner,      Length = 220, TargetSpeed = speedMid * 1.1,  SteeringSign = 1,  CornerSeverity = 0.3 });
        // Section 9: Chicane brake
        segments.Add(new TrackSegment { Type = SectionType.Braking,     Length = 120, TargetSpeed = speedLo * 1.1,   SteeringSign = 0,  CornerSeverity = 0 });
        // Section 10: Chicane left-right
        segments.Add(new TrackSegment { Type = SectionType.Chicane,     Length = 160, TargetSpeed = speedLo,         SteeringSign = -1, CornerSeverity = 0.55 });
        // Section 11: Medium straight
        segments.Add(new TrackSegment { Type = SectionType.Straight,    Length = 400, TargetSpeed = speedHi * 0.88,  SteeringSign = 0,  CornerSeverity = 0 });
        // Section 12: Braking into technical section
        segments.Add(new TrackSegment { Type = SectionType.Braking,     Length = 130, TargetSpeed = speedLo * 0.95,  SteeringSign = 0,  CornerSeverity = 0 });
        // Section 13: Technical right
        segments.Add(new TrackSegment { Type = SectionType.Corner,      Length = 110, TargetSpeed = speedLo * 1.05,  SteeringSign = 1,  CornerSeverity = 0.55 });
        // Section 14: Short link
        segments.Add(new TrackSegment { Type = SectionType.CornerExit,  Length = 160, TargetSpeed = speedMid * 0.9,  SteeringSign = 0,  CornerSeverity = 0 });
        // Section 15: Braking for final corner
        segments.Add(new TrackSegment { Type = SectionType.Braking,     Length = 140, TargetSpeed = speedLo * 1.15,  SteeringSign = 0,  CornerSeverity = 0 });
        // Section 16: Final corner — medium left
        segments.Add(new TrackSegment { Type = SectionType.Corner,      Length = 130, TargetSpeed = speedLo * 1.1,   SteeringSign = -1, CornerSeverity = 0.5 });
        // Section 17: Final acceleration to start/finish
        segments.Add(new TrackSegment { Type = SectionType.CornerExit,  Length = 480, TargetSpeed = speedHi,         SteeringSign = 0,  CornerSeverity = 0 });

        return segments;
    }

    // ──────────────────────────────────────────────────────
    // SIMULATE A FULL LAP (physics-based state machine)
    // ──────────────────────────────────────────────────────
    private List<TelemetryPoint> SimulateLap(int lapId, Vehicle vehicle, VehicleProfile vp, List<TrackSegment> track)
    {
        var points = new List<TelemetryPoint>();
        var rng = new Random(lapId * 31 + vehicle.VehicleId * 7);

        double totalTrackLen = track.Sum(s => s.Length);
        double dt = 0.1; // 10 Hz sampling
        double time = 0;
        double distance = 0;
        double speed = vp.MinCornerSpeed * 1.4; // start from pit exit speed
        double throttle = 60;
        double brake = 0;
        double steering = 0;
        double leanAngle = 0;
        int gear = 2;

        // Suspension state
        double suspFL = vp.SuspNominal, suspFR = vp.SuspNominal;
        double suspRL = vp.SuspNominal, suspRR = vp.SuspNominal;

        int segIdx = 0;
        double segDistConsumed = 0;

        while (distance < totalTrackLen && segIdx < track.Count)
        {
            var seg = track[segIdx];
            double segProgress = Math.Clamp(segDistConsumed / Math.Max(1, seg.Length), 0, 1);

            // ──── Determine target state based on section type ────
            double targetThrottle, targetBrake, targetSteering, targetSpeed;

            switch (seg.Type)
            {
                case SectionType.Straight:
                    targetSpeed = seg.TargetSpeed;
                    targetThrottle = speed < targetSpeed * 0.97 ? 95 + rng.NextDouble() * 5 : 70 + rng.NextDouble() * 10;
                    targetBrake = 0;
                    targetSteering = rng.NextDouble() * 1.5 - 0.75;
                    break;

                case SectionType.Braking:
                    targetSpeed = seg.TargetSpeed;
                    double brakingIntensity = Math.Clamp((speed - targetSpeed) / Math.Max(1, speed) * 2.5, 0, 1);
                    targetBrake = brakingIntensity * (75 + rng.NextDouble() * 25);
                    targetThrottle = brakingIntensity > 0.3 ? 0 : Math.Max(0, 15 - brakingIntensity * 30);
                    targetSteering = rng.NextDouble() * 2 - 1;
                    break;

                case SectionType.Corner:
                {
                    targetSpeed = seg.TargetSpeed;
                    double steerMag = seg.CornerSeverity * vp.MaxSteeringAngle * (0.7 + 0.3 * Math.Sin(segProgress * Math.PI));
                    targetSteering = steerMag * seg.SteeringSign;
                    if (segProgress < 0.25)
                    {
                        targetBrake = (1 - segProgress * 4) * seg.CornerSeverity * 60;
                        targetThrottle = 5;
                    }
                    else if (segProgress < 0.6)
                    {
                        targetBrake = 0;
                        targetThrottle = 20 + segProgress * 30;
                    }
                    else
                    {
                        targetBrake = 0;
                        targetThrottle = 40 + (segProgress - 0.6) * 150;
                    }
                    break;
                }

                case SectionType.CornerExit:
                    targetSpeed = seg.TargetSpeed;
                    targetSteering = steering * (1 - segProgress);
                    targetThrottle = 50 + segProgress * 50;
                    targetBrake = 0;
                    break;

                case SectionType.Chicane:
                {
                    targetSpeed = seg.TargetSpeed;
                    double chicanePhase = segProgress * 2 * Math.PI;
                    double chicaneSteerMag = seg.CornerSeverity * vp.MaxSteeringAngle * 0.65;
                    targetSteering = Math.Sin(chicanePhase) * chicaneSteerMag * seg.SteeringSign;
                    targetBrake = segProgress < 0.15 ? 40 : 0;
                    targetThrottle = segProgress < 0.3 ? 15 : (30 + segProgress * 60);
                    break;
                }

                default:
                    targetSpeed = seg.TargetSpeed;
                    targetThrottle = 50;
                    targetBrake = 0;
                    targetSteering = 0;
                    break;
            }

            // ──── Smooth transitions (exponential filter) ────
            double smoothRate = 0.12;
            double speedSmooth = 0.08;
            throttle = Lerp(throttle, targetThrottle, smoothRate);
            brake = Lerp(brake, targetBrake, smoothRate);
            steering = Lerp(steering, targetSteering, smoothRate * 0.8);

            // ──── Physics-based speed update ────
            double speedMs = speed / 3.6;
            double accelForce = (throttle / 100.0) * vp.AccelGMax * 9.81;
            double brakeForce = (brake / 100.0) * vp.BrakeGMax * 9.81;
            double dragForce = 0.0004 * speedMs * speedMs;
            double corneringDrag = Math.Abs(steering) / Math.Max(1, vp.MaxSteeringAngle) * 0.3 * speedMs;

            double netAccel = accelForce - brakeForce - dragForce - corneringDrag;
            speedMs += netAccel * dt;
            speedMs = Math.Max(0, speedMs);
            speed = speedMs * 3.6;

            // Gentle pull toward target speed to keep bounded
            speed = Lerp(speed, targetSpeed, speedSmooth * 0.3);

            // ──── Gear from speed ────
            double[] gearSpeeds = ComputeGearSpeeds(vp);
            gear = 1;
            for (int g = vp.MaxGear; g >= 1; g--)
            {
                if (speed >= gearSpeeds[g - 1] * 0.85)
                {
                    gear = g;
                    break;
                }
            }

            // ──── RPM from speed and gear ────
            double gearRatio = (double)gear / vp.MaxGear;
            double speedFraction = Math.Clamp(speed / Math.Max(1, vp.MaxSpeed), 0, 1);
            double rpmBase = vp.IdleRPM + (vp.MaxRPM - vp.IdleRPM) * (speedFraction / Math.Max(0.1, gearRatio));
            int rpm = (int)Math.Clamp(rpmBase + rng.NextDouble() * 200 - 100, vp.IdleRPM, vp.MaxRPM);

            // ──── Lean angle for bikes ────
            if (vp.IsBike)
            {
                double targetLean = Math.Abs(steering) / Math.Max(1, vp.MaxSteeringAngle) * vp.MaxLeanAngle * Math.Sign(steering);
                double speedLeanFactor = Math.Clamp(speed / 80.0, 0, 1);
                targetLean *= speedLeanFactor;
                leanAngle = Lerp(leanAngle, targetLean, 0.15);
            }

            // ──── G-force computation ────
            double longG = netAccel / 9.81;
            double latG = 0;
            if (speed > 10)
            {
                double steerFraction = Math.Abs(steering) / Math.Max(1, vp.MaxSteeringAngle);
                latG = steerFraction * vp.LatGMax * Math.Clamp(speed / 150.0, 0.2, 1.0) * Math.Sign(steering);
            }

            // ──── Suspension dynamics ────
            double brakePitch = Math.Clamp(-longG * 2.5, -vp.SuspRange, vp.SuspRange);
            double cornerRoll = Math.Clamp(latG * 1.8, -vp.SuspRange, vp.SuspRange);
            double bumpNoise = (rng.NextDouble() - 0.5) * 1.5;

            suspFL = vp.SuspNominal + brakePitch + cornerRoll + bumpNoise;
            suspFR = vp.SuspNominal + brakePitch - cornerRoll + bumpNoise;
            suspRL = vp.SuspNominal - brakePitch + cornerRoll * 0.6 + bumpNoise;
            suspRR = vp.SuspNominal - brakePitch - cornerRoll * 0.6 + bumpNoise;

            // ──── Determine sector (1-3) ────
            int sector = distance < totalTrackLen * 0.33 ? 1 : (distance < totalTrackLen * 0.66 ? 2 : 3);

            // ──── VALIDATION & CLAMPING ────
            speed = Math.Clamp(speed, 0, vp.MaxSpeed);
            throttle = Math.Clamp(throttle, 0, 100);
            brake = Math.Clamp(brake, 0, 100);
            rpm = (int)Math.Clamp(rpm, vp.IdleRPM, vp.MaxRPM);
            gear = Math.Clamp(gear, 1, vp.MaxGear);
            steering = Math.Clamp(steering, -vp.MaxSteeringAngle, vp.MaxSteeringAngle);
            leanAngle = Math.Clamp(leanAngle, -vp.MaxLeanAngle, vp.MaxLeanAngle);
            longG = Math.Clamp(longG, -vp.BrakeGMax, vp.AccelGMax);
            latG = Math.Clamp(latG, -vp.LatGMax, vp.LatGMax);

            // Cannot have simultaneous full throttle + full brake
            if (throttle > 50 && brake > 30) { brake = Math.Max(0, brake - throttle * 0.5); }

            // ──── Create telemetry point ────
            var pt = new TelemetryPoint
            {
                LapId = lapId,
                Timestamp = TimeSpan.FromSeconds(time),
                Distance = Math.Round(distance, 2),
                Speed = Math.Round(speed, 1),
                RPM = rpm,
                Gear = gear,
                Throttle = Math.Round(throttle, 1),
                Brake = Math.Round(brake, 1),
                Sector = sector
            };

            if (vp.IsBike)
            {
                pt.LeanAngle = Math.Round(leanAngle, 1);
                pt.FrontBrake = Math.Round(Math.Clamp(brake * 0.7, 0, 100), 1);
                pt.RearBrake = Math.Round(Math.Clamp(brake * 0.3, 0, 100), 1);
                pt.FrontSuspension = Math.Round(Math.Clamp(suspFL, vp.SuspNominal - vp.SuspRange * 1.5, vp.SuspNominal + vp.SuspRange * 1.5), 1);
                pt.RearSuspension = Math.Round(Math.Clamp(suspRL, vp.SuspNominal - vp.SuspRange * 1.5, vp.SuspNominal + vp.SuspRange * 1.5), 1);
            }
            else
            {
                pt.Steering = Math.Round(steering, 1);
                pt.SuspensionFL = Math.Round(Math.Clamp(suspFL, 20, 90), 1);
                pt.SuspensionFR = Math.Round(Math.Clamp(suspFR, 20, 90), 1);
                pt.SuspensionRL = Math.Round(Math.Clamp(suspRL, 20, 90), 1);
                pt.SuspensionRR = Math.Round(Math.Clamp(suspRR, 20, 90), 1);
            }

            points.Add(pt);

            // ──── Advance simulation ────
            double advanceDist = Math.Max(0, speed / 3.6 * dt);
            distance += advanceDist;
            segDistConsumed += advanceDist;
            time += dt;

            if (segDistConsumed >= seg.Length)
            {
                segDistConsumed -= seg.Length;
                segIdx++;
            }
        }

        // Ensure we have at least some points
        if (points.Count == 0)
        {
            points.Add(new TelemetryPoint
            {
                LapId = lapId, Timestamp = TimeSpan.Zero, Distance = 0,
                Speed = 0, RPM = vp.IdleRPM, Gear = 1, Throttle = 0, Brake = 0, Sector = 1
            });
        }

        return points;
    }

    // ──────────────────────────────────────────────────────
    // HELPER: Gear speed boundaries
    // ──────────────────────────────────────────────────────
    private double[] ComputeGearSpeeds(VehicleProfile vp)
    {
        var speeds = new double[vp.MaxGear];
        for (int g = 0; g < vp.MaxGear; g++)
        {
            speeds[g] = vp.MaxSpeed * ((double)(g + 1) / vp.MaxGear) * 0.92;
        }
        return speeds;
    }

    // ──────────────────────────────────────────────────────
    // HELPER: Linear interpolation
    // ──────────────────────────────────────────────────────
    private static double Lerp(double current, double target, double rate)
    {
        return current + (target - current) * Math.Clamp(rate, 0, 1);
    }
}

