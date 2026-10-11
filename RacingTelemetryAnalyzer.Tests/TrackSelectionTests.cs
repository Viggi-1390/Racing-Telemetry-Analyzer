using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RacingTelemetryAnalyzer.Controllers;
using RacingTelemetryAnalyzer.Data;
using RacingTelemetryAnalyzer.Models;
using Xunit;

namespace RacingTelemetryAnalyzer.Tests;

public class TrackSelectionTests
{
    private ApplicationDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    private TrackController CreateTrackController(ApplicationDbContext context)
    {
        var controller = new TrackController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
        };
        return controller;
    }

    private HomeController CreateHomeController(ApplicationDbContext context)
    {
        var controller = new HomeController(NullLogger<HomeController>.Instance, context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
        };
        return controller;
    }

    private TelemetryApiController CreateTelemetryApiController(ApplicationDbContext context)
    {
        return new TelemetryApiController(context);
    }

    // =========================================================================
    // 1. TRACKS API READ-ONLY RETRIEVAL & EMPTY DATABASE SAFETY
    // =========================================================================

    [Fact]
    public async Task GetTracksApi_WhenTracksExist_ReturnsAllTrackRecordsWithMetadata()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_GetTracksApi_Populated_" + Guid.NewGuid());
        DbInitializer.SeedDefaultData(context);
        var controller = CreateTrackController(context);

        // Act
        var result = await controller.GetTracksApi() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);

        dynamic data = result.Value!;
        Assert.True((bool)data.GetType().GetProperty("success").GetValue(data, null));
        var tracks = data.GetType().GetProperty("tracks").GetValue(data, null) as System.Collections.IEnumerable;
        Assert.NotNull(tracks);

        var list = tracks.Cast<object>().ToList();
        Assert.Equal(7, list.Count);
    }

    [Fact]
    public async Task GetTracksApi_WhenDatabaseEmpty_ReturnsEmptyArrayAndDoesNotSeed()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_GetTracksApi_Empty_" + Guid.NewGuid());
        var controller = CreateTrackController(context);

        // Act
        var result = await controller.GetTracksApi() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);

        dynamic data = result.Value!;
        var tracks = data.GetType().GetProperty("tracks").GetValue(data, null) as System.Collections.IEnumerable;
        Assert.NotNull(tracks);
        var list = tracks.Cast<object>().ToList();
        Assert.Empty(list);

        // Ensure database remains completely unseeded
        Assert.False(await context.Tracks.AnyAsync(), "GET /api/tracks must not seed records");
        Assert.False(await context.Vehicles.AnyAsync(), "GET /api/tracks must not seed vehicles");
    }

    // =========================================================================
    // 2. VEHICLE PRESERVATION & VALIDATION
    // =========================================================================

    [Fact]
    public async Task GetVehicleApi_WithValidId_ReturnsExpectedVehicleDetailsForContext()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_GetVehicleApi_Valid_" + Guid.NewGuid());
        DbInitializer.SeedDefaultData(context);
        var homeController = CreateHomeController(context);

        var cadillac = await context.Vehicles.FirstAsync(v => v.Name.Contains("CADILLAC"));

        // Act
        var result = await homeController.GetVehicleApi(cadillac.VehicleId) as JsonResult;

        // Assert
        Assert.NotNull(result);
        dynamic data = result.Value!;
        Assert.True((bool)data.GetType().GetProperty("success").GetValue(data, null));

        dynamic vehicleDto = data.GetType().GetProperty("vehicle").GetValue(data, null);
        Assert.Equal(cadillac.VehicleId, (int)vehicleDto.GetType().GetProperty("id").GetValue(vehicleDto, null));
        Assert.Equal(cadillac.Name, (string)vehicleDto.GetType().GetProperty("name").GetValue(vehicleDto, null));
    }

    [Fact]
    public async Task GetVehicleApi_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_GetVehicleApi_NotFound_" + Guid.NewGuid());
        DbInitializer.SeedDefaultData(context);
        var homeController = CreateHomeController(context);

        // Act
        var result = await homeController.GetVehicleApi(99999);

        // Assert: Safe recovery on deleted or nonexistent vehicle
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetVehicleApi_WithInvalidId_ReturnsBadRequest(int invalidId)
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_GetVehicleApi_Invalid_" + Guid.NewGuid());
        var homeController = CreateHomeController(context);

        // Act
        var result = await homeController.GetVehicleApi(invalidId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task TrackIndex_WithValidVehicleId_PreservesAssignedVehicleInViewBag()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_TrackIndex_VehiclePreservation_" + Guid.NewGuid());
        DbInitializer.SeedDefaultData(context);
        var trackController = CreateTrackController(context);

        var ferrari = await context.Vehicles.FirstAsync(v => v.Name.Contains("FERRARI F1"));

        // Act
        var result = trackController.Index(ferrari.VehicleId) as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ferrari.VehicleId, trackController.ViewBag.VehicleId);
        var assigned = trackController.ViewBag.Vehicle as Vehicle;
        Assert.NotNull(assigned);
        Assert.Equal(ferrari.Name, assigned.Name);
    }

    // =========================================================================
    // 3. TRACK RETRIEVAL & CRUD BEHAVIOR
    // =========================================================================

    [Fact]
    public async Task GetTrackApi_WithValidId_ReturnsExpectedTrackDetails()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_GetTrackApi_Valid_" + Guid.NewGuid());
        DbInitializer.SeedDefaultData(context);
        var trackController = CreateTrackController(context);

        var monza = await context.Tracks.FirstAsync(t => t.Name == "MONZA");

        // Act
        var result = await trackController.GetTrackApi(monza.TrackId) as JsonResult;

        // Assert
        Assert.NotNull(result);
        dynamic data = result.Value!;
        Assert.True((bool)data.GetType().GetProperty("success").GetValue(data, null));

        dynamic trackDto = data.GetType().GetProperty("track").GetValue(data, null);
        Assert.Equal(monza.TrackId, (int)trackDto.GetType().GetProperty("id").GetValue(trackDto, null));
        Assert.Equal("MONZA", (string)trackDto.GetType().GetProperty("name").GetValue(trackDto, null));
        Assert.Equal(11, (int)trackDto.GetType().GetProperty("numberOfTurns").GetValue(trackDto, null));
    }

    [Fact]
    public async Task GetTrackApi_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_GetTrackApi_NotFound_" + Guid.NewGuid());
        DbInitializer.SeedDefaultData(context);
        var trackController = CreateTrackController(context);

        // Act
        var result = await trackController.GetTrackApi(99999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateTrackApi_WithValidData_CreatesNewTrackInDatabase()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_CreateTrackApi_Valid_" + Guid.NewGuid());
        var trackController = CreateTrackController(context);

        var newTrack = new Track
        {
            Name = "INTERLAGOS",
            Country = "Brazil",
            Length = 4.309,
            NumberOfTurns = 15,
            ImagePath = "/images/tracks/interlagos.jpg"
        };

        // Act
        var result = await trackController.CreateTrackApi(newTrack, null) as ObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(201, result.StatusCode);

        var created = await context.Tracks.FirstOrDefaultAsync(t => t.Name == "INTERLAGOS");
        Assert.NotNull(created);
        Assert.Equal("Brazil", created.Country);
        Assert.Equal(4.309, created.Length);
        Assert.Equal(15, created.NumberOfTurns);
    }

    [Fact]
    public async Task CreateTrackApi_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_CreateTrackApi_Invalid_" + Guid.NewGuid());
        var trackController = CreateTrackController(context);

        var invalidTrack = new Track
        {
            Name = "", // Invalid: blank name
            Country = "Nowhere",
            Length = -5, // Invalid: negative length
            NumberOfTurns = 0
        };

        // Act
        var result = await trackController.CreateTrackApi(invalidTrack, null) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task EditTrack_UpdatesExistingTrackDetails()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_EditTrack_Valid_" + Guid.NewGuid());
        DbInitializer.SeedDefaultData(context);
        var trackController = CreateTrackController(context);

        var targetTrack = await context.Tracks.FirstAsync();
        int targetId = targetTrack.TrackId;

        var updatedInfo = new Track
        {
            TrackId = targetId,
            Name = "MONZA RECONFIGURED",
            Country = "Italy",
            Length = 5.800,
            NumberOfTurns = 12
        };

        // Act
        var result = await trackController.EditTrack(targetId, updatedInfo, null) as JsonResult;

        // Assert
        Assert.NotNull(result);
        var updatedInDb = await context.Tracks.FindAsync(targetId);
        Assert.NotNull(updatedInDb);
        Assert.Equal("MONZA RECONFIGURED", updatedInDb.Name);
        Assert.Equal(12, updatedInDb.NumberOfTurns);
    }

    [Fact]
    public async Task DeleteTrackApi_RemovesTrackFromDatabase()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_DeleteTrackApi_" + Guid.NewGuid());
        DbInitializer.SeedDefaultData(context);
        var trackController = CreateTrackController(context);

        var trackToDelete = await context.Tracks.FirstAsync(t => t.Name == "SPA-FRANCORCHAMPS");
        int targetId = trackToDelete.TrackId;

        // Act
        var result = await trackController.DeleteTrackApi(targetId) as JsonResult;

        // Assert
        Assert.NotNull(result);
        Assert.False(await context.Tracks.AnyAsync(t => t.TrackId == targetId));

        // Subsequent lookup returns 404
        var checkResult = await trackController.GetTrackApi(targetId);
        Assert.IsType<NotFoundObjectResult>(checkResult);
    }

    // =========================================================================
    // 4. TELEMETRY ISOLATION & INTEGRATION SAFETY
    // =========================================================================

    [Fact]
    public async Task GetSessionTelemetry_WhenTrackIdMismatched_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_TelemetryIsolation_TrackMismatch_" + Guid.NewGuid());
        DbInitializer.SeedDefaultData(context);

        var vehicle = await context.Vehicles.FirstAsync();
        var track1 = await context.Tracks.FirstAsync();
        var track2 = await context.Tracks.Skip(1).FirstAsync();

        var session = new Session
        {
            Name = "PRACTICE 1",
            VehicleId = vehicle.VehicleId,
            TrackId = track1.TrackId,
            SessionType = "PRACTICE 1",
            Date = DateTime.UtcNow,
            Laps = new List<Lap> { new Lap { LapNumber = 1, LapTime = TimeSpan.FromSeconds(85) } }
        };
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var apiController = CreateTelemetryApiController(context);

        // Act: Request session with WRONG trackId (track2)
        var result = await apiController.GetSessionTelemetry(session.SessionId, vehicleId: vehicle.VehicleId, trackId: track2.TrackId);

        // Assert: Must strictly isolate and reject cross-track access
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetSessionTelemetry_WhenVehicleIdMismatched_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_TelemetryIsolation_VehicleMismatch_" + Guid.NewGuid());
        DbInitializer.SeedDefaultData(context);

        var vehicleA = await context.Vehicles.FirstAsync();
        var vehicleB = await context.Vehicles.Skip(1).FirstAsync();
        var track = await context.Tracks.FirstAsync();

        var session = new Session
        {
            Name = "PRACTICE 1",
            VehicleId = vehicleA.VehicleId,
            TrackId = track.TrackId,
            SessionType = "PRACTICE 1",
            Date = DateTime.UtcNow,
            Laps = new List<Lap> { new Lap { LapNumber = 1, LapTime = TimeSpan.FromSeconds(85) } }
        };
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var apiController = CreateTelemetryApiController(context);

        // Act: Request session with WRONG vehicleId (vehicleB)
        var result = await apiController.GetSessionTelemetry(session.SessionId, vehicleId: vehicleB.VehicleId, trackId: track.TrackId);

        // Assert: Must strictly isolate and reject cross-vehicle access
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetLatestTelemetry_StrictlyFiltersByVehicleAndTrack()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_TelemetryLatest_StrictFilter_" + Guid.NewGuid());
        DbInitializer.SeedDefaultData(context);

        var vehicle = await context.Vehicles.FirstAsync();
        var track1 = await context.Tracks.FirstAsync();
        var track2 = await context.Tracks.Skip(1).FirstAsync();

        // Session 1: on Track 1
        var session1 = new Session
        {
            Name = "RACE 1",
            VehicleId = vehicle.VehicleId,
            TrackId = track1.TrackId,
            SessionType = "RACE",
            Date = DateTime.UtcNow.AddHours(-2),
            Laps = new List<Lap> { new Lap { LapNumber = 1, LapTime = TimeSpan.FromSeconds(90) } }
        };

        // Session 2: on Track 2 (more recent)
        var session2 = new Session
        {
            Name = "QUALIFYING 1",
            VehicleId = vehicle.VehicleId,
            TrackId = track2.TrackId,
            SessionType = "QUALIFYING",
            Date = DateTime.UtcNow,
            Laps = new List<Lap> { new Lap { LapNumber = 1, LapTime = TimeSpan.FromSeconds(88) } }
        };

        context.Sessions.AddRange(session1, session2);
        await context.SaveChangesAsync();

        var apiController = CreateTelemetryApiController(context);

        // Act: Request latest for vehicle on Track 1
        var result = await apiController.GetLatestTelemetry(vehicleId: vehicle.VehicleId, trackId: track1.TrackId) as OkObjectResult;

        // Assert: Must return session1 (Track 1), NOT session2
        Assert.NotNull(result);
        dynamic data = result.Value!;
        int returnedSessionId = (int)data.GetType().GetProperty("sessionId").GetValue(data, null);
        Assert.Equal(session1.SessionId, returnedSessionId);
    }
}
