using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RacingTelemetryAnalyzer.Controllers;
using RacingTelemetryAnalyzer.Models;

namespace RacingTelemetryAnalyzer.Data;

public static class DbInitializer
{
    /// <summary>
    /// Explicitly seeds default vehicles and tracks into the database if their respective tables are empty.
    /// This should only be invoked from explicit initialization workflows (CLI flag, setup endpoint, or tests),
    /// never on normal GET requests.
    /// </summary>
    public static async Task<bool> SeedDefaultDataAsync(ApplicationDbContext context)
    {
        bool modified = false;

        if (!await context.Vehicles.AnyAsync())
        {
            var seedFleet = HomeController.GetDefaultSeedFleet();
            await context.Vehicles.AddRangeAsync(seedFleet);
            modified = true;
        }

        if (!await context.Tracks.AnyAsync())
        {
            var seedTracks = TrackController.GetDefaultTracks();
            await context.Tracks.AddRangeAsync(seedTracks);
            modified = true;
        }

        if (modified)
        {
            await context.SaveChangesAsync();
        }

        return modified;
    }

    /// <summary>
    /// Synchronous explicit seeder for startup or unit test configurations.
    /// </summary>
    public static bool SeedDefaultData(ApplicationDbContext context)
    {
        bool modified = false;

        if (!context.Vehicles.Any())
        {
            var seedFleet = HomeController.GetDefaultSeedFleet();
            context.Vehicles.AddRange(seedFleet);
            modified = true;
        }

        if (!context.Tracks.Any())
        {
            var seedTracks = TrackController.GetDefaultTracks();
            context.Tracks.AddRange(seedTracks);
            modified = true;
        }

        if (modified)
        {
            context.SaveChanges();
        }

        return modified;
    }
}

