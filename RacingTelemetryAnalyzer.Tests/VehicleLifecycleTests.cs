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

    private HomeController CreateController(ApplicationDbContext context)
    {
        return new HomeController(NullLogger<HomeController>.Instance, context);
    }

    [Fact]
    public void Index_WhenDbIsEmpty_SeedsInitialFleet()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("Test_SeedInitialFleet_" + Guid.NewGuid());
        var controller = CreateController(context);

        // Act
        var result = controller.Index() as ViewResult;

        // Assert
        Assert.NotNull(result);
        var model = result.Model as List<Vehicle>;
        Assert.NotNull(model);
        Assert.NotEmpty(model);
        Assert.True(context.Vehicles.Any());
    }

    [Fact]
    public async Task Index_WhenVehicleDeleted_DoesNotRecreateDeletedVehicle()
    {
        // Arrange: Start with seeded database
        var dbName = "Test_DeleteVehiclePersistence_" + Guid.NewGuid();
        using (var context = CreateInMemoryDbContext(dbName))
        {
            var controller = CreateController(context);
            controller.Index(); // triggers initial seed
        }

        int targetVehicleId;
        string targetVehicleName;

        // Act 1: Delete a specific vehicle (e.g. Audi R8 or Cadillac or Ferrari F1)
        using (var context = CreateInMemoryDbContext(dbName))
        {
            var vehicleToDelete = await context.Vehicles.FirstOrDefaultAsync(v => v.Name.Contains("AUDI"));
            Assert.NotNull(vehicleToDelete);
            targetVehicleId = vehicleToDelete.VehicleId;
            targetVehicleName = vehicleToDelete.Name;

            context.Vehicles.Remove(vehicleToDelete);
            await context.SaveChangesAsync();

            // Verify it was deleted
            Assert.False(await context.Vehicles.AnyAsync(v => v.VehicleId == targetVehicleId));
        }

        // Act 2: Simulate page refresh / subsequent visits to Garage (HomeController.Index)
        using (var context = CreateInMemoryDbContext(dbName))
        {
            var controller = CreateController(context);
            var result = controller.Index() as ViewResult;
            Assert.NotNull(result);

            var model = result.Model as List<Vehicle>;
            Assert.NotNull(model);

            // Assert: The deleted vehicle MUST NOT be in the returned view model
            Assert.DoesNotContain(model, v => v.Name == targetVehicleName || v.VehicleId == targetVehicleId);

            // Assert: The deleted vehicle MUST NOT be recreated in the database
            Assert.False(await context.Vehicles.AnyAsync(v => v.Name == targetVehicleName || v.VehicleId == targetVehicleId));
        }

        // Act 3: Simulate another refresh
        using (var context = CreateInMemoryDbContext(dbName))
        {
            var controller = CreateController(context);
            var result = controller.Index() as ViewResult;
            Assert.NotNull(result);

            var model = result.Model as List<Vehicle>;
            Assert.NotNull(model);

            Assert.DoesNotContain(model, v => v.Name == targetVehicleName);
            Assert.False(await context.Vehicles.AnyAsync(v => v.Name == targetVehicleName));
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
        // Arrange
        var dbName = "Test_DeleteSpecificVehicle_" + vehicleName.Replace(" ", "_") + "_" + Guid.NewGuid();
        using (var context = CreateInMemoryDbContext(dbName))
        {
            var controller = CreateController(context);
            controller.Index(); // Seeds
        }

        // Delete the specified vehicle
        using (var context = CreateInMemoryDbContext(dbName))
        {
            var vehicle = await context.Vehicles.FirstOrDefaultAsync(v => v.Name == vehicleName);
            Assert.NotNull(vehicle);
            context.Vehicles.Remove(vehicle);
            await context.SaveChangesAsync();
        }

        // Refresh/Reload Garage
        using (var context = CreateInMemoryDbContext(dbName))
        {
            var controller = CreateController(context);
            var result = controller.Index() as ViewResult;
            Assert.NotNull(result);

            var model = result.Model as List<Vehicle>;
            Assert.NotNull(model);

            // Assert: Never recreated
            Assert.DoesNotContain(model, v => v.Name == vehicleName);
            Assert.False(await context.Vehicles.AnyAsync(v => v.Name == vehicleName));
        }
    }
}
