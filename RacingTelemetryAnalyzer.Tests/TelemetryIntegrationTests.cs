using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RacingTelemetryAnalyzer.Controllers;
using RacingTelemetryAnalyzer.Data;
using RacingTelemetryAnalyzer.Models;
using RacingTelemetryAnalyzer.Services;
using Xunit;

namespace RacingTelemetryAnalyzer.Tests;

public class TelemetryIntegrationTests
{
    private ApplicationDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    private TelemetryApiController CreateTelemetryApiController(ApplicationDbContext context)
    {
        return new TelemetryApiController(context);
    }

    private static object? GetProp(object? obj, string propName)
    {
        return obj?.GetType().GetProperty(propName)?.GetValue(obj, null);
    }

    // =========================================================================
    // 1. SESSION ISOLATION & FILTERING
    // =========================================================================

    [Fact]
    public async Task GetSessions_FilteredByVehicleAndTrack_ReturnsOnlyMatchingSessions()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_GetSessions_Isolation_" + Guid.NewGuid());
        var v1 = new Vehicle { VehicleId = 1, Name = "Cadillac", Manufacturer = "Cadillac", Class = "GTP", Engine = "V8", ImagePath = "/img/cad.jpg" };
        var v2 = new Vehicle { VehicleId = 2, Name = "Ferrari", Manufacturer = "Ferrari", Class = "GT3", Engine = "V6", ImagePath = "/img/fer.jpg" };
        var t1 = new Track { TrackId = 1, Name = "Monza", Country = "Italy", Length = 5.793, ImagePath = "/img/monza.jpg" };
        var t2 = new Track { TrackId = 2, Name = "Spa", Country = "Belgium", Length = 7.004, ImagePath = "/img/spa.jpg" };

        context.Vehicles.AddRange(v1, v2);
        context.Tracks.AddRange(t1, t2);

        // Session 1: v1 @ t1
        var s1 = new Session { SessionId = 10, VehicleId = 1, TrackId = 1, Name = "Cadillac Monza Practice", SessionType = "PRACTICE 1", Date = DateTime.UtcNow };
        s1.Laps = new List<Lap> { new Lap { LapId = 101, SessionId = 10, LapNumber = 1, LapTime = TimeSpan.FromSeconds(105.2), IsValid = true } };

        // Session 2: v1 @ t2
        var s2 = new Session { SessionId = 20, VehicleId = 1, TrackId = 2, Name = "Cadillac Spa Practice", SessionType = "PRACTICE 1", Date = DateTime.UtcNow };
        s2.Laps = new List<Lap> { new Lap { LapId = 201, SessionId = 20, LapNumber = 1, LapTime = TimeSpan.FromSeconds(135.5), IsValid = true } };

        // Session 3: v2 @ t1
        var s3 = new Session { SessionId = 30, VehicleId = 2, TrackId = 1, Name = "Ferrari Monza Practice", SessionType = "PRACTICE 1", Date = DateTime.UtcNow };
        s3.Laps = new List<Lap> { new Lap { LapId = 301, SessionId = 30, LapNumber = 1, LapTime = TimeSpan.FromSeconds(108.0), IsValid = true } };

        context.Sessions.AddRange(s1, s2, s3);
        await context.SaveChangesAsync();

        var controller = CreateTelemetryApiController(context);

        // Act - Request sessions for v1 @ t1
        var result = await controller.GetSessions(vehicleId: 1, trackId: 1) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var val = result.Value!;
        Assert.True((bool)GetProp(val, "success")!);
        Assert.Equal(1, (int)GetProp(val, "totalSessions")!);

        var returnedSessions = (IEnumerable)GetProp(val, "sessions")!;
        var list = returnedSessions.Cast<object>().ToList();
        Assert.Single(list);

        var firstSession = list[0];
        Assert.Equal(10, (int)GetProp(firstSession, "sessionId")!);
        Assert.Equal(1, (int)GetProp(firstSession, "vehicleId")!);
        Assert.Equal(1, (int)GetProp(firstSession, "trackId")!);
    }

    // =========================================================================
    // 2. CROSS-VEHICLE / CROSS-TRACK LEAK PROTECTION
    // =========================================================================

    [Fact]
    public async Task GetSessionTelemetry_WithMismatchedVehicleId_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_LeakProtection_Vehicle_" + Guid.NewGuid());
        var v1 = new Vehicle { VehicleId = 1, Name = "Cadillac", Manufacturer = "Cadillac", Class = "GTP", Engine = "V8", ImagePath = "/img/cad.jpg" };
        var t1 = new Track { TrackId = 1, Name = "Monza", Country = "Italy", Length = 5.793, ImagePath = "/img/monza.jpg" };
        var s1 = new Session { SessionId = 10, VehicleId = 1, TrackId = 1, Name = "Practice", SessionType = "PRACTICE 1", Date = DateTime.UtcNow };
        s1.Laps = new List<Lap> { new Lap { LapId = 101, SessionId = 10, LapNumber = 1, LapTime = TimeSpan.FromSeconds(105), IsValid = true } };

        context.Vehicles.Add(v1);
        context.Tracks.Add(t1);
        context.Sessions.Add(s1);
        await context.SaveChangesAsync();

        var controller = CreateTelemetryApiController(context);

        // Act - Request session 10 with mismatched vehicleId = 99
        var result = await controller.GetSessionTelemetry(sessionId: 10, vehicleId: 99, trackId: 1) as NotFoundObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task GetSessionTelemetry_WithMismatchedTrackId_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_LeakProtection_Track_" + Guid.NewGuid());
        var v1 = new Vehicle { VehicleId = 1, Name = "Cadillac", Manufacturer = "Cadillac", Class = "GTP", Engine = "V8", ImagePath = "/img/cad.jpg" };
        var t1 = new Track { TrackId = 1, Name = "Monza", Country = "Italy", Length = 5.793, ImagePath = "/img/monza.jpg" };
        var s1 = new Session { SessionId = 10, VehicleId = 1, TrackId = 1, Name = "Practice", SessionType = "PRACTICE 1", Date = DateTime.UtcNow };
        s1.Laps = new List<Lap> { new Lap { LapId = 101, SessionId = 10, LapNumber = 1, LapTime = TimeSpan.FromSeconds(105), IsValid = true } };

        context.Vehicles.Add(v1);
        context.Tracks.Add(t1);
        context.Sessions.Add(s1);
        await context.SaveChangesAsync();

        var controller = CreateTelemetryApiController(context);

        // Act - Request session 10 with mismatched trackId = 99
        var result = await controller.GetSessionTelemetry(sessionId: 10, vehicleId: 1, trackId: 99) as NotFoundObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
    }

    // =========================================================================
    // 3. COMPLETE PAYLOAD RETRIEVAL WITH ENRICHED METADATA
    // =========================================================================

    [Fact]
    public async Task GetSessionTelemetry_WhenValid_ReturnsCompletePayloadWithTelemetryPoints()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_GetSessionTelemetry_Payload_" + Guid.NewGuid());
        var vehicle = new Vehicle
        {
            VehicleId = 1,
            Name = "CADILLAC V-SERIES.R",
            Manufacturer = "Cadillac",
            Class = "GTP / LMDh",
            Power = 670,
            TopSpeed = 340,
            Engine = "5.5L V8",
            ImagePath = "/images/cars/CadillacVSeriesR.jpg",
            VehicleType = VehicleType.Car
        };

        var track = new Track
        {
            TrackId = 1,
            Name = "AUTODROMO NAZIONALE MONZA",
            Country = "Italy",
            Length = 5.793,
            NumberOfTurns = 11,
            ImagePath = "/images/tracks/monza.jpg"
        };

        var session = new Session
        {
            SessionId = 5,
            VehicleId = 1,
            TrackId = 1,
            Name = "PRACTICE 1",
            SessionType = "PRACTICE 1",
            Date = DateTime.UtcNow,
            Condition = new SessionCondition
            {
                SessionConditionId = 50,
                SessionId = 5,
                TrackTemperature = 32.5,
                AirTemperature = 24.0,
                Weather = "DRY",
                TrackCondition = "Dry",
                WindSpeed = 10.0
            }
        };

        var lap = new Lap
        {
            LapId = 501,
            SessionId = 5,
            LapNumber = 1,
            LapTime = TimeSpan.FromSeconds(102.5),
            Sector1Time = 30.5,
            Sector2Time = 42.0,
            Sector3Time = 30.0,
            IsValid = true
        };

        var point = new TelemetryPoint
        {
            TelemetryPointId = 1001,
            LapId = 501,
            Distance = 250.0,
            Speed = 280.0,
            RPM = 7800,
            Gear = 5,
            Throttle = 100.0,
            Brake = 0.0,
            Steering = 2.5,
            Timestamp = TimeSpan.FromSeconds(3.5),
            SuspensionFL = 45.0,
            SuspensionFR = 46.0,
            SuspensionRL = 52.0,
            SuspensionRR = 52.0,
            GLat = 1.2,
            GLon = 0.5,
            GVert = 0.2
        };

        var analysis = new AnalysisResult
        {
            AnalysisResultId = 601,
            LapId = 501,
            EntrySpeed = 280.0,
            MinimumSpeed = 75.0,
            ExitSpeed = 250.0,
            BrakingPercentage = 88.0,
            ThrottleResponseTime = 0.22,
            Recommendation = "Brake 10m later into Turn 1",
            RecommendationType = "Braking"
        };

        context.Vehicles.Add(vehicle);
        context.Tracks.Add(track);
        context.Sessions.Add(session);
        context.Laps.Add(lap);
        context.TelemetryPoints.Add(point);
        context.AnalysisResults.Add(analysis);
        await context.SaveChangesAsync();

        var controller = CreateTelemetryApiController(context);

        // Act
        var result = await controller.GetSessionTelemetry(sessionId: 5, vehicleId: 1, trackId: 1) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var val = result.Value!;
        Assert.True((bool)GetProp(val, "success")!);
        Assert.Equal(5, (int)GetProp(val, "sessionId")!);
        Assert.Equal("PRACTICE 1", (string)GetProp(val, "sessionType")!);

        // Check vehicle metadata
        var vObj = GetProp(val, "vehicle");
        Assert.NotNull(vObj);
        Assert.Equal("CADILLAC V-SERIES.R", (string)GetProp(vObj, "Name")!);
        Assert.Equal(670, (int)GetProp(vObj, "Power")!);

        // Check track metadata
        var tObj = GetProp(val, "track");
        Assert.NotNull(tObj);
        Assert.Equal("AUTODROMO NAZIONALE MONZA", (string)GetProp(tObj, "Name")!);
        Assert.Equal(11, (int)GetProp(tObj, "NumberOfTurns")!);

        // Check condition
        var cObj = GetProp(val, "condition");
        Assert.NotNull(cObj);
        Assert.Equal(32.5, (double)GetProp(cObj, "trackTemperature")!);

        // Check laps and telemetry points
        Assert.Equal(1, (int)GetProp(val, "totalLaps")!);
        Assert.Equal(1, (int)GetProp(val, "totalPoints")!);

        var dataPoints = (IEnumerable)GetProp(val, "data")!;
        var firstPt = dataPoints.Cast<object>().First();
        Assert.Equal(280.0, (double)GetProp(firstPt, "speed")!);
        Assert.Equal(7800, (int)GetProp(firstPt, "rpm")!);
        Assert.Equal(100.0, (double)GetProp(firstPt, "thr")!);

        // Check analysis results
        var analysisList = (IEnumerable)GetProp(val, "analysisResults")!;
        Assert.Single(analysisList.Cast<object>());
    }

    // =========================================================================
    // 4. LATEST TELEMETRY RETRIEVAL
    // =========================================================================

    [Fact]
    public async Task GetLatestTelemetry_ReturnsMostRecentMatchingSession()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_GetLatestTelemetry_" + Guid.NewGuid());
        var v1 = new Vehicle { VehicleId = 1, Name = "Cadillac", Manufacturer = "Cadillac", Class = "GTP", Engine = "V8", ImagePath = "/img/cad.jpg" };
        var t1 = new Track { TrackId = 1, Name = "Monza", Country = "Italy", Length = 5.793, ImagePath = "/img/monza.jpg" };

        var s1 = new Session { SessionId = 10, VehicleId = 1, TrackId = 1, Name = "Session 1", SessionType = "PRACTICE 1", Date = DateTime.UtcNow.AddHours(-2) };
        s1.Laps = new List<Lap> { new Lap { LapId = 101, SessionId = 10, LapNumber = 1, LapTime = TimeSpan.FromSeconds(105), IsValid = true } };

        var s2 = new Session { SessionId = 20, VehicleId = 1, TrackId = 1, Name = "Session 2", SessionType = "QUALIFYING", Date = DateTime.UtcNow };
        s2.Laps = new List<Lap> { new Lap { LapId = 201, SessionId = 20, LapNumber = 1, LapTime = TimeSpan.FromSeconds(101), IsValid = true } };

        context.Vehicles.Add(v1);
        context.Tracks.Add(t1);
        context.Sessions.AddRange(s1, s2);
        await context.SaveChangesAsync();

        var controller = CreateTelemetryApiController(context);

        // Act
        var result = await controller.GetLatestTelemetry(vehicleId: 1, trackId: 1) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var val = result.Value!;
        Assert.True((bool)GetProp(val, "success")!);
        Assert.Equal(20, (int)GetProp(val, "sessionId")!);
        Assert.Equal("QUALIFYING", (string)GetProp(val, "sessionType")!);
    }

    // =========================================================================
    // 5. SYNTHETIC DEMO GENERATION API
    // =========================================================================

    [Fact]
    public async Task GenerateDataApi_WithValidRequest_Generates3LapsAndAnalysisResults()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_GenerateDataApi_" + Guid.NewGuid());
        var vehicle = new Vehicle { VehicleId = 1, Name = "Cadillac", Manufacturer = "Cadillac", Class = "GTP", Engine = "V8", ImagePath = "/img/cad.jpg" };
        var track = new Track { TrackId = 1, Name = "Monza", Country = "Italy", Length = 5.793, ImagePath = "/img/monza.jpg" };

        context.Vehicles.Add(vehicle);
        context.Tracks.Add(track);
        await context.SaveChangesAsync();

        var dataGenerator = new DataGeneratorService();
        var analysisService = new TelemetryAnalysisService();
        var controller = CreateTelemetryApiController(context);

        var request = new GenerateDataApiRequest
        {
            VehicleId = 1,
            TrackId = 1,
            SessionType = "PRACTICE 2"
        };

        // Act
        var result = await controller.GenerateDataApi(request, dataGenerator, analysisService) as ObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(201, result.StatusCode);
        var val = result.Value!;
        Assert.True((bool)GetProp(val, "success")!);
        Assert.Equal(3, (int)GetProp(val, "totalLaps")!);
        Assert.Equal("PRACTICE 2", (string)GetProp(val, "sessionType")!);

        // Verify database records
        int newSessionId = (int)GetProp(val, "sessionId")!;
        var savedSession = await context.Sessions
            .Include(s => s.Laps)
            .FirstOrDefaultAsync(s => s.SessionId == newSessionId);

        Assert.NotNull(savedSession);
        Assert.Equal(3, savedSession.Laps.Count);

        var lapIds = savedSession.Laps.Select(l => l.LapId).ToList();
        var totalPoints = await context.TelemetryPoints.CountAsync(t => lapIds.Contains(t.LapId));
        Assert.True(totalPoints > 0, "Telemetry points should be generated for each lap");

        var totalAnalysis = await context.AnalysisResults.CountAsync(a => lapIds.Contains(a.LapId));
        Assert.True(totalAnalysis > 0, "Analysis results should be created for generated laps");
    }

    [Fact]
    public async Task GenerateDataApi_WithMissingVehicleOrTrack_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_GenerateDataApi_Invalid_" + Guid.NewGuid());
        var dataGenerator = new DataGeneratorService();
        var analysisService = new TelemetryAnalysisService();
        var controller = CreateTelemetryApiController(context);

        var request = new GenerateDataApiRequest
        {
            VehicleId = 999,
            TrackId = 999,
            SessionType = "PRACTICE 1"
        };

        // Act
        var result = await controller.GenerateDataApi(request, dataGenerator, analysisService) as NotFoundObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
    }

    // =========================================================================
    // 6. CSV IMPORT API VALIDATION
    // =========================================================================

    [Fact]
    public async Task ImportCsvApi_WithNullOrEmptyFile_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_ImportCsvApi_EmptyFile_" + Guid.NewGuid());
        var v = new Vehicle { VehicleId = 1, Name = "Cadillac", Manufacturer = "Cadillac", Class = "GTP", Engine = "V8", ImagePath = "/img/cad.jpg" };
        context.Vehicles.Add(v);
        await context.SaveChangesAsync();

        var controller = CreateTelemetryApiController(context);
        var csvService = new CsvImportService();
        var analysisService = new TelemetryAnalysisService();

        var emptyStream = new MemoryStream();
        var formFile = new FormFile(emptyStream, 0, 0, "csvFile", "test.csv");

        // Act
        var result = await controller.ImportCsvApi(
            csvFile: formFile,
            vehicleId: 1,
            trackId: 1,
            uploadToken: "tok_test",
            sessionType: "PRACTICE 1",
            csvService: csvService,
            analysisService: analysisService) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ImportCsvApi_WithNonExistentVehicle_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_ImportCsvApi_MissingVehicle_" + Guid.NewGuid());
        var controller = CreateTelemetryApiController(context);
        var csvService = new CsvImportService();
        var analysisService = new TelemetryAnalysisService();

        var stream = new MemoryStream(Encoding.UTF8.GetBytes("Lap,Time,Distance,Speed\n1,0.0,0,100\n"));
        var formFile = new FormFile(stream, 0, stream.Length, "csvFile", "test.csv");

        // Act
        var result = await controller.ImportCsvApi(
            csvFile: formFile,
            vehicleId: 999,
            trackId: 1,
            uploadToken: "tok_test",
            sessionType: "PRACTICE 1",
            csvService: csvService,
            analysisService: analysisService) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void ImportProgress_ReturnsStatusObject()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_ImportProgress_" + Guid.NewGuid());
        var controller = CreateTelemetryApiController(context);

        // Act
        var result = controller.ImportProgress("non_existent_token") as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }
}

