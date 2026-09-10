using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace RacingTelemetryAnalyzer.Models
{
    public enum VehicleType { Car, Bike }
    public enum VehicleCategory { Race, Production }

    public enum MotorsportSessionType
    {
        Practice1,
        Practice2,
        Practice3,
        Qualifying,
        SprintQualifying,
        SprintRace,
        Race
    }

    public static class SessionTypeHelper
    {
        private static readonly Dictionary<MotorsportSessionType, string> DisplayNames = new()
        {
            { MotorsportSessionType.Practice1, "PRACTICE 1" },
            { MotorsportSessionType.Practice2, "PRACTICE 2" },
            { MotorsportSessionType.Practice3, "PRACTICE 3" },
            { MotorsportSessionType.Qualifying, "QUALIFYING" },
            { MotorsportSessionType.SprintQualifying, "SPRINT QUALIFYING" },
            { MotorsportSessionType.SprintRace, "SPRINT RACE" },
            { MotorsportSessionType.Race, "RACE" }
        };

        public static string ToDisplayName(this MotorsportSessionType type) => DisplayNames[type];

        public static string[] AllDisplayNames() => DisplayNames.Values.ToArray();

        public static string ValidateOrDefault(string? sessionType)
        {
            if (string.IsNullOrWhiteSpace(sessionType)) return "PRACTICE 1";
            var upper = sessionType.Trim().ToUpperInvariant();
            return DisplayNames.Values.Contains(upper) ? upper : "PRACTICE 1";
        }
    }
    
    public class Vehicle
    {
        public int VehicleId { get; set; }
        [Required] public string Name { get; set; }
        [Required] public string Manufacturer { get; set; }
        public VehicleType VehicleType { get; set; }
        public VehicleCategory VehicleCategory { get; set; }
        public string Class { get; set; }
        public string Engine { get; set; }
        public int Power { get; set; }
        public int Weight { get; set; }
        public int TopSpeed { get; set; }
        public string ImagePath { get; set; }

        public ICollection<Session> Sessions { get; set; }
    }

    public class Track
    {
        public int TrackId { get; set; }
        [Required] public string Name { get; set; }
        [Required] public string Country { get; set; }
        public double Length { get; set; }
        public int NumberOfTurns { get; set; }
        public string ImagePath { get; set; }

        public ICollection<TrackSection> Sections { get; set; }
        public ICollection<Session> Sessions { get; set; }
    }

    public class TrackSection
    {
        public int TrackSectionId { get; set; }
        public int TrackId { get; set; }
        public string Name { get; set; }
        public double StartDistance { get; set; }
        public double EndDistance { get; set; }
        public Track Track { get; set; }
    }

    public class Session
    {
        public int SessionId { get; set; }
        public int VehicleId { get; set; }
        public int TrackId { get; set; }
        public string Name { get; set; }
        public string SessionType { get; set; }
        public DateTime Date { get; set; }

        public Vehicle Vehicle { get; set; }
        public Track Track { get; set; }
        public SessionCondition Condition { get; set; }
        public VehicleSetup Setup { get; set; }
        public ICollection<Lap> Laps { get; set; }
        public ICollection<SessionNote> Notes { get; set; }
    }

    public class SessionCondition
    {
        public int SessionConditionId { get; set; }
        public int SessionId { get; set; }
        public double AirTemperature { get; set; }
        public double TrackTemperature { get; set; }
        public string Weather { get; set; }
        public double WindSpeed { get; set; }
        public string TrackCondition { get; set; }
        public Session Session { get; set; }
    }

    public class VehicleSetup
    {
        public int VehicleSetupId { get; set; }
        public int SessionId { get; set; }
        public double FrontWing { get; set; }
        public double RearWing { get; set; }
        public double RideHeight { get; set; }
        public double BrakeBias { get; set; }
        public Session Session { get; set; }
    }

    public class Lap
    {
        public int LapId { get; set; }
        public int SessionId { get; set; }
        public int LapNumber { get; set; }
        public TimeSpan LapTime { get; set; }
        public double Sector1Time { get; set; }
        public double Sector2Time { get; set; }
        public double Sector3Time { get; set; }
        public bool IsValid { get; set; }

        public Session Session { get; set; }
        public ICollection<TelemetryPoint> TelemetryPoints { get; set; }
        public ICollection<AnalysisResult> AnalysisResults { get; set; }
    }

    public class TelemetryPoint
    {
        public int TelemetryPointId { get; set; }
        public int LapId { get; set; }
        public TimeSpan Timestamp { get; set; }
        public double Distance { get; set; }
        public double Speed { get; set; }
        public int RPM { get; set; }
        public int Gear { get; set; }
        public double Throttle { get; set; }
        public double Brake { get; set; }
        public int Sector { get; set; }

        // Car specifics
        public double? Steering { get; set; }
        public double? SuspensionFL { get; set; }
        public double? SuspensionFR { get; set; }
        public double? SuspensionRL { get; set; }
        public double? SuspensionRR { get; set; }

        // G-Forces & Spatial
        public double? GLat { get; set; }
        public double? GLon { get; set; }
        public double? GVert { get; set; }
        public double? PosX { get; set; }
        public double? PosZ { get; set; }

        // Bike specifics
        public double? LeanAngle { get; set; }
        public double? FrontBrake { get; set; }
        public double? RearBrake { get; set; }
        public double? FrontSuspension { get; set; }
        public double? RearSuspension { get; set; }

        public Lap Lap { get; set; }
    }

    public class AnalysisResult
    {
        public int AnalysisResultId { get; set; }
        public int LapId { get; set; }
        public int? TrackSectionId { get; set; }
        public double EntrySpeed { get; set; }
        public double MinimumSpeed { get; set; }
        public double ExitSpeed { get; set; }
        public double BrakingPercentage { get; set; }
        public double ThrottleResponseTime { get; set; }
        public double TimeDelta { get; set; }
        public string Recommendation { get; set; }
        public string RecommendationType { get; set; }

        public Lap Lap { get; set; }
        public TrackSection TrackSection { get; set; }
    }

    public class SessionNote
    {
        public int SessionNoteId { get; set; }
        public int SessionId { get; set; }
        public int? LapId { get; set; }
        public int? TrackSectionId { get; set; }
        public string Note { get; set; }
        public Session Session { get; set; }
    }

    public class TyreInfo
    {
        public string Position { get; set; } = "";
        public string Label { get; set; } = "";
        public double Temperature { get; set; }
        public double Pressure { get; set; }
        public double WearPercentage { get; set; }
        public string Status { get; set; } = "OPTIMAL";
    }

    public class VehicleTyreStatus
    {
        public bool IsBike { get; set; }
        public TyreInfo? FrontTyre { get; set; }
        public TyreInfo? RearTyre { get; set; }
        public TyreInfo? FL { get; set; }
        public TyreInfo? FR { get; set; }
        public TyreInfo? RL { get; set; }
        public TyreInfo? RR { get; set; }
        public double? MaxLeanAngle { get; set; }
    }
}

