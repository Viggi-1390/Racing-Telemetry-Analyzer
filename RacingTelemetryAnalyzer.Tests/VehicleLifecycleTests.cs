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

public class VehicleLifecycleTests
{
    private ApplicationDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private HomeController CreateHomeController(ApplicationDbContext context)
    {
        return new HomeController(NullLogger<HomeController>.Instance, context);
    }

    private TrackController CreateTrackController(ApplicationDbContext context)
    {
        return new TrackController(context);
    }

    // =========================================================================
    // 1. FRESH EMPTY DATABASE — GET REQUESTS MUST BE SAFE AND NEVER SEED
    // =========================================================================

    [Fact]
    public void Index_WhenDbIsEmpty_ReturnsEmptyAndDoesNotSeed()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_EmptyDb_HomeIndex_" + Guid.NewGuid());
        var controller = CreateHomeController(context);

        // Act
        var result = controller.Index() as ViewResult;

        // Assert
        Assert.NotNull(result);
        var model = result.Model as List<Vehicle>;
        Assert.NotNull(model);
        Assert.Empty(model);
        Assert.False(context.Vehicles.Any(), "GET /Home/Index must not seed vehicles into an empty database");
        Assert.False(context.Tracks.Any(), "GET /Home/Index must not seed tracks into an empty database");
    }

    [Fact]
    public async Task GetVehiclesApi_WhenDbIsEmpty_ReturnsEmptyAndDoesNotSeed()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_EmptyDb_VehiclesApi_" + Guid.NewGuid());
        var controller = CreateHomeController(context);

        // Act
        var result = await controller.GetVehiclesApi() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.False(await context.Vehicles.AnyAsync(), "GET /api/vehicles must not seed vehicles into an empty database");
        Assert.False(await context.Tracks.AnyAsync(), "GET /api/vehicles must not seed tracks into an empty database");
    }

    [Fact]
    public void TrackIndex_WhenDbIsEmpty_ReturnsEmptyAndDoesNotSeed()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_EmptyDb_TrackIndex_" + Guid.NewGuid());
        var controller = CreateTrackController(context);

        // Act
        var result = controller.Index() as ViewResult;

        // Assert
        Assert.NotNull(result);
        var model = result.Model as List<Track>;
        Assert.NotNull(model);
        Assert.Empty(model);
        Assert.False(context.Tracks.Any(), "GET /Track/Index must not seed tracks into an empty database");
        Assert.False(context.Vehicles.Any(), "GET /Track/Index must not seed vehicles into an empty database");
    }

    [Fact]
    public async Task GetTracksApi_WhenDbIsEmpty_ReturnsEmptyAndDoesNotSeed()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_EmptyDb_TracksApi_" + Guid.NewGuid());
        var controller = CreateTrackController(context);

        // Act
        var result = await controller.GetTracksApi() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.False(await context.Tracks.AnyAsync(), "GET /api/tracks must not seed tracks into an empty database");
        Assert.False(await context.Vehicles.AnyAsync(), "GET /api/tracks must not seed vehicles into an empty database");
    }

    [Fact]
    public async Task CompletelyEmptyDatabase_RemainsEmptyAcrossMultipleGetRequests()
    {
        // Arrange: Start with an unseeded, empty database
        var dbName = "Test_EmptyDb_MultipleGets_" + Guid.NewGuid();

        using (var context = CreateInMemoryDbContext(dbName))
        {
            var homeController = CreateHomeController(context);
            var trackController = CreateTrackController(context);

            // Repeated GET requests
            await homeController.GetVehiclesApi();
            homeController.Index();
            await trackController.GetTracksApi();
            trackController.Index();

            // Assert: Database must strictly remain empty
            Assert.False(await context.Vehicles.AnyAsync(), "Vehicles must remain empty after multiple GET calls");
            Assert.False(await context.Tracks.AnyAsync(), "Tracks must remain empty after multiple GET calls");
        }
    }

    // =========================================================================
    // 2. EXPLICIT SEEDING MECHANISMS (DbInitializer, POST Endpoints)
    // =========================================================================

    [Fact]
    public async Task DbInitializer_WhenFreshDatabase_SeedsDefaultFleetAndCircuits()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_DbInitializer_Fresh_" + Guid.NewGuid());

        // Act: Run explicit seeder
        bool seeded = await DbInitializer.SeedDefaultDataAsync(context);

        // Assert
        Assert.True(seeded, "DbInitializer should report that data was seeded");
        Assert.Equal(15, await context.Vehicles.CountAsync());
        Assert.Equal(7, await context.Tracks.CountAsync());

        // Act 2: Second run must be idempotent (no duplicates)
        bool reseeded = await DbInitializer.SeedDefaultDataAsync(context);
        Assert.False(reseeded, "Second seed run must report no changes");
        Assert.Equal(15, await context.Vehicles.CountAsync());
        Assert.Equal(7, await context.Tracks.CountAsync());
    }

    [Fact]
    public async Task DbInitializer_WhenPartiallyInitialized_VehiclesExist_SeedsOnlyMissingTracks()
    {
        // Arrange: Database already has custom vehicles, but 0 tracks
        using var context = CreateInMemoryDbContext("Test_DbInitializer_MissingTracks_" + Guid.NewGuid());
        context.Vehicles.Add(new Vehicle
        {
            Name = "CUSTOM PROTOTYPE",
            Manufacturer = "Custom",
            VehicleType = VehicleType.Car,
            VehicleCategory = VehicleCategory.Race,
            Class = "LMP1",
            Engine = "V8 Twin Turbo",
            ImagePath = "/images/cars/custom.jpg",
            Power = 1000,
            Weight = 850,
            TopSpeed = 350
        });
        await context.SaveChangesAsync();

        // Act
        bool modified = await DbInitializer.SeedDefaultDataAsync(context);

        // Assert
        Assert.True(modified);
        Assert.Equal(1, await context.Vehicles.CountAsync()); // Existing vehicle was preserved, default fleet not added
        Assert.Equal(7, await context.Tracks.CountAsync());   // Missing tracks were seeded
    }

    [Fact]
    public async Task DbInitializer_WhenPartiallyInitialized_TracksExist_SeedsOnlyMissingVehicles()
    {
        // Arrange: Database already has a track, but 0 vehicles
        using var context = CreateInMemoryDbContext("Test_DbInitializer_MissingVehicles_" + Guid.NewGuid());
        context.Tracks.Add(new Track
        {
            Name = "CUSTOM CIRCUIT",
            Country = "Customland",
            Length = 4.5,
            NumberOfTurns = 12,
            ImagePath = "/images/tracks/custom.jpg"
        });
        await context.SaveChangesAsync();

        // Act
        bool modified = await DbInitializer.SeedDefaultDataAsync(context);

        // Assert
        Assert.True(modified);
        Assert.Equal(15, await context.Vehicles.CountAsync()); // Missing fleet was seeded
        Assert.Equal(1, await context.Tracks.CountAsync());   // Existing track was preserved
    }

    [Fact]
    public async Task SeedSystemApi_WhenFreshDatabase_SeedsBothVehiclesAndTracks()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_SeedSystemApi_" + Guid.NewGuid());
        var controller = CreateHomeController(context);

        // Act
        var result = await controller.SeedSystemApi() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.True(await context.Vehicles.AnyAsync());
        Assert.True(await context.Tracks.AnyAsync());
    }

    [Fact]
    public async Task SeedEndpoints_WhenIndividualCalled_SeedCorrectTables()
    {
        var dbName = "Test_IndividualSeedEndpoints_" + Guid.NewGuid();

        // Seed vehicles only
        using (var context = CreateInMemoryDbContext(dbName))
        {
            var homeController = CreateHomeController(context);
            var result = await homeController.SeedVehiclesApi() as OkObjectResult;
            Assert.NotNull(result);
            Assert.True(await context.Vehicles.AnyAsync());
            Assert.False(await context.Tracks.AnyAsync());
        }

        // Seed tracks only
        using (var context = CreateInMemoryDbContext(dbName))
        {
            var trackController = CreateTrackController(context);
            var result = await trackController.SeedTracksApi() as OkObjectResult;
            Assert.NotNull(result);
            Assert.True(await context.Vehicles.AnyAsync());
            Assert.True(await context.Tracks.AnyAsync());
        }
    }

    // =========================================================================
    // 3. VEHICLE DELETION PERSISTENCE
    // =========================================================================

    [Fact]
    public async Task Index_WhenVehicleDeleted_DoesNotRecreateDeletedVehicle()
    {
        var dbName = "Test_DeleteVehiclePersistence_" + Guid.NewGuid();
        using (var context = CreateInMemoryDbContext(dbName))
        {
            DbInitializer.SeedDefaultData(context);
        }

        int targetVehicleId;
        string targetVehicleName;

        // Act 1: Delete a specific vehicle
        using (var context = CreateInMemoryDbContext(dbName))
        {
            var vehicleToDelete = await context.Vehicles.FirstOrDefaultAsync(v => v.Name.Contains("AUDI"));
            Assert.NotNull(vehicleToDelete);
            targetVehicleId = vehicleToDelete.VehicleId;
            targetVehicleName = vehicleToDelete.Name;

            context.Vehicles.Remove(vehicleToDelete);
            await context.SaveChangesAsync();
            Assert.False(await context.Vehicles.AnyAsync(v => v.VehicleId == targetVehicleId));
        }

        // Act 2: Simulate page refresh (HomeController.Index)
        using (var context = CreateInMemoryDbContext(dbName))
        {
            var controller = CreateHomeController(context);
            var result = controller.Index() as ViewResult;
            Assert.NotNull(result);

            var model = result.Model as List<Vehicle>;
            Assert.NotNull(model);

            Assert.DoesNotContain(model, v => v.Name == targetVehicleName || v.VehicleId == targetVehicleId);
            Assert.False(await context.Vehicles.AnyAsync(v => v.Name == targetVehicleName || v.VehicleId == targetVehicleId));
        }
    }

    [Theory]
    [InlineData("CADILLAC V-SERIES.R")]
    [InlineData("FERRARI F1")]
    [InlineData("PORSCHE 963")]
    [InlineData("AUDI R8 LMS GT3")]
    [InlineData("FERRARI 296 GTB")]
    public async Task Index_WhenAnySpecificVehicleDeleted_NeverRecreatesIt(string vehicleName)
    {
        var dbName = "Test_DeleteSpecificVehicle_" + vehicleName.Replace(" ", "_") + "_" + Guid.NewGuid();
        using (var context = CreateInMemoryDbContext(dbName))
        {
            DbInitializer.SeedDefaultData(context);
        }

        using (var context = CreateInMemoryDbContext(dbName))
        {
            var vehicle = await context.Vehicles.FirstOrDefaultAsync(v => v.Name == vehicleName);
            Assert.NotNull(vehicle);
            context.Vehicles.Remove(vehicle);
            await context.SaveChangesAsync();
        }

        using (var context = CreateInMemoryDbContext(dbName))
        {
            var controller = CreateHomeController(context);
            var result = controller.Index() as ViewResult;
            Assert.NotNull(result);

            var model = result.Model as List<Vehicle>;
            Assert.NotNull(model);

            Assert.DoesNotContain(model, v => v.Name == vehicleName);
            Assert.False(await context.Vehicles.AnyAsync(v => v.Name == vehicleName));
        }
    }

    [Fact]
    public async Task GetVehiclesApi_WhenVehicleDeleted_NeverRecreatesIt()
    {
        var dbName = "Test_ApiDeleteVehiclePersistence_" + Guid.NewGuid();
        using (var context = CreateInMemoryDbContext(dbName))
        {
            DbInitializer.SeedDefaultData(context);
        }

        int targetVehicleId;
        string targetVehicleName;

        using (var context = CreateInMemoryDbContext(dbName))
        {
            var vehicleToDelete = await context.Vehicles.FirstOrDefaultAsync(v => v.Name.Contains("AUDI"));
            Assert.NotNull(vehicleToDelete);
            targetVehicleId = vehicleToDelete.VehicleId;
            targetVehicleName = vehicleToDelete.Name;

            context.Vehicles.Remove(vehicleToDelete);
            await context.SaveChangesAsync();
            Assert.False(await context.Vehicles.AnyAsync(v => v.VehicleId == targetVehicleId));
        }

        using (var context = CreateInMemoryDbContext(dbName))
        {
            var controller = CreateHomeController(context);
            var result = await controller.GetVehiclesApi() as OkObjectResult;
            Assert.NotNull(result);

            Assert.False(await context.Vehicles.AnyAsync(v => v.Name == targetVehicleName || v.VehicleId == targetVehicleId));
        }
    }

    // =========================================================================
    // 4. ALL VEHICLES DELETED — REMAINS EMPTY
    // =========================================================================

    [Fact]
    public async Task GetVehiclesApi_WhenAllVehiclesDeleted_FleetRemainsEmptyOnNextCall()
    {
        var dbName = "Test_DeleteAllVehicles_" + Guid.NewGuid();
        using (var context = CreateInMemoryDbContext(dbName))
        {
            DbInitializer.SeedDefaultData(context);
            Assert.True(await context.Vehicles.AnyAsync());
            Assert.True(await context.Tracks.AnyAsync());
        }

        // Delete ALL vehicles
        using (var context = CreateInMemoryDbContext(dbName))
        {
            var allVehicles = await context.Vehicles.ToListAsync();
            context.Vehicles.RemoveRange(allVehicles);
            await context.SaveChangesAsync();
            Assert.False(await context.Vehicles.AnyAsync());
        }

        // Subsequent API call
        using (var context = CreateInMemoryDbContext(dbName))
        {
            var controller = CreateHomeController(context);
            var result = await controller.GetVehiclesApi() as OkObjectResult;
            Assert.NotNull(result);

            Assert.False(await context.Vehicles.AnyAsync(), "Vehicles must remain empty after all are deleted");
            Assert.True(await context.Tracks.AnyAsync(), "Tracks must still be preserved");
        }

        // Subsequent Razor Index call
        using (var context = CreateInMemoryDbContext(dbName))
        {
            var controller = CreateHomeController(context);
            var result = controller.Index() as ViewResult;
            Assert.NotNull(result);
            var model = result.Model as List<Vehicle>;
            Assert.NotNull(model);
            Assert.Empty(model);
            Assert.False(await context.Vehicles.AnyAsync());
        }
    }

    // =========================================================================
    // 5. ALL TRACKS DELETED — REMAINS EMPTY
    // =========================================================================

    [Fact]
    public async Task TrackIndex_WhenSpecificTrackDeleted_DoesNotResurrectDeletedTrack()
    {
        var dbName = "Test_DeleteTrackPersistence_" + Guid.NewGuid();
        using (var context = CreateInMemoryDbContext(dbName))
        {
            DbInitializer.SeedDefaultData(context);
            Assert.True(await context.Tracks.AnyAsync(t => t.Name == "MONZA"));
        }

        using (var context = CreateInMemoryDbContext(dbName))
        {
            var monza = await context.Tracks.FirstOrDefaultAsync(t => t.Name == "MONZA");
            Assert.NotNull(monza);
            context.Tracks.Remove(monza);
            await context.SaveChangesAsync();
            Assert.False(await context.Tracks.AnyAsync(t => t.Name == "MONZA"));
        }

        using (var context = CreateInMemoryDbContext(dbName))
        {
            var trackController = CreateTrackController(context);
            var result = trackController.Index() as ViewResult;
            Assert.NotNull(result);

            Assert.False(await context.Tracks.AnyAsync(t => t.Name == "MONZA"));
        }
    }

    [Fact]
    public async Task TrackIndex_WhenAllTracksDeleted_DoesNotReseedTracksWhenVehiclesExist()
    {
        var dbName = "Test_DeleteAllTracks_" + Guid.NewGuid();
        using (var context = CreateInMemoryDbContext(dbName))
        {
            DbInitializer.SeedDefaultData(context);
        }

        using (var context = CreateInMemoryDbContext(dbName))
        {
            var allTracks = await context.Tracks.ToListAsync();
            context.Tracks.RemoveRange(allTracks);
            await context.SaveChangesAsync();
            Assert.False(await context.Tracks.AnyAsync());
        }

        using (var context = CreateInMemoryDbContext(dbName))
        {
            var trackController = CreateTrackController(context);
            var result = trackController.Index() as ViewResult;
            Assert.NotNull(result);

            Assert.False(await context.Tracks.AnyAsync(), "Tracks must remain empty after all tracks deleted");
            Assert.True(await context.Vehicles.AnyAsync(), "Vehicles must remain intact");
        }

        using (var context = CreateInMemoryDbContext(dbName))
        {
            var trackController = CreateTrackController(context);
            var result = await trackController.GetTracksApi() as OkObjectResult;
            Assert.NotNull(result);

            Assert.False(await context.Tracks.AnyAsync());
        }
    }

    // =========================================================================
    // 6. BOTH ALL VEHICLES AND ALL TRACKS DELETED — MUST NOT RESEED ON GET!
    // =========================================================================

    [Fact]
    public async Task AllVehiclesAndAllTracksDeleted_BothRemainEmptyAfterSubsequentGetRequests()
    {
        var dbName = "Test_DeleteBothAllVehiclesAndAllTracks_" + Guid.NewGuid();

        // 1. Initial state: seeded
        using (var context = CreateInMemoryDbContext(dbName))
        {
            DbInitializer.SeedDefaultData(context);
            Assert.True(await context.Vehicles.AnyAsync());
            Assert.True(await context.Tracks.AnyAsync());
        }

        // 2. User deletes ALL vehicles AND ALL tracks
        using (var context = CreateInMemoryDbContext(dbName))
        {
            context.Vehicles.RemoveRange(await context.Vehicles.ToListAsync());
            context.Tracks.RemoveRange(await context.Tracks.ToListAsync());
            await context.SaveChangesAsync();

            Assert.False(await context.Vehicles.AnyAsync());
            Assert.False(await context.Tracks.AnyAsync());
        }

        // 3. User visits Home/Index, api/vehicles, Track/Index, and api/tracks
        using (var context = CreateInMemoryDbContext(dbName))
        {
            var homeController = CreateHomeController(context);
            var trackController = CreateTrackController(context);

            var homeViewResult = homeController.Index() as ViewResult;
            Assert.NotNull(homeViewResult);
            Assert.Empty((List<Vehicle>)homeViewResult.Model!);

            var vehiclesApiResult = await homeController.GetVehiclesApi() as OkObjectResult;
            Assert.NotNull(vehiclesApiResult);

            var trackViewResult = trackController.Index() as ViewResult;
            Assert.NotNull(trackViewResult);
            Assert.Empty((List<Track>)trackViewResult.Model!);

            var tracksApiResult = await trackController.GetTracksApi() as OkObjectResult;
            Assert.NotNull(tracksApiResult);

            // CRITICAL ASSERTION: The bug where an empty DB auto-seeded on GET is eliminated!
            Assert.False(await context.Vehicles.AnyAsync(), "Vehicles must NOT be resurrected when both vehicles and tracks were deleted");
            Assert.False(await context.Tracks.AnyAsync(), "Tracks must NOT be resurrected when both vehicles and tracks were deleted");
        }
    }
}
