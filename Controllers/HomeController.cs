using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RacingTelemetryAnalyzer.Models;
using RacingTelemetryAnalyzer.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RacingTelemetryAnalyzer.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index(int? vehicleId = null)
    {
        ViewBag.SelectedVehicleId = vehicleId;
        var vehicles = _context.Vehicles.ToList();
        bool needsSave = false;
        
        // Seed default vehicles ONLY if the database has zero vehicles (e.g. brand-new database)
        if (!vehicles.Any())
        {
            vehicles = new List<Vehicle>
            {
                new Vehicle { Engine = "5.5L DOHC V8 Hybrid", Name = "CADILLAC V-SERIES.R", Manufacturer = "Cadillac", VehicleType = VehicleType.Car, VehicleCategory = VehicleCategory.Race, Class = "GTP / LMDh", Power = 670, Weight = 1030, TopSpeed = 338, ImagePath = "/images/cars/CadillacVSeriesR.jpg" },
                new Vehicle { Engine = "1.6L Turbo Hybrid V6", Name = "FERRARI F1", Manufacturer = "Ferrari", VehicleType = VehicleType.Car, VehicleCategory = VehicleCategory.Race, Class = "Formula 1", Power = 1000, Weight = 798, TopSpeed = 360, ImagePath = "/images/cars/FerrariF1Studio.jpg" },
                new Vehicle { Engine = "Hybrid V8", Name = "PORSCHE 963", Manufacturer = "Porsche", VehicleType = VehicleType.Car, VehicleCategory = VehicleCategory.Race, Class = "LMDh", Power = 680, Weight = 1030, TopSpeed = 330, ImagePath = "/images/cars/Porsche963Studio.jpg" },
                new Vehicle { Engine = "5.2L Naturally Aspirated V10", Name = "AUDI R8 LMS GT3", Manufacturer = "Audi", VehicleType = VehicleType.Car, VehicleCategory = VehicleCategory.Race, Class = "GT3", Power = 585, Weight = 1225, TopSpeed = 305, ImagePath = "/images/cars/AudiR8LMS.jpg" },
                new Vehicle { Engine = "Hybrid V6", Name = "FERRARI 499P", Manufacturer = "Ferrari", VehicleType = VehicleType.Car, VehicleCategory = VehicleCategory.Race, Class = "Hypercar", Power = 670, Weight = 1030, TopSpeed = 340, ImagePath = "/images/cars/Ferrari499p.jpg" },
                new Vehicle { Engine = "Hybrid V8", Name = "BMW M HYBRID V8", Manufacturer = "BMW", VehicleType = VehicleType.Car, VehicleCategory = VehicleCategory.Race, Class = "LMDh", Power = 640, Weight = 1030, TopSpeed = 325, ImagePath = "/images/cars/Bmwmhybrid.jpg" },
                new Vehicle { Engine = "V4", Name = "DUCATI PANIGALE V4 R", Manufacturer = "Ducati", VehicleType = VehicleType.Bike, VehicleCategory = VehicleCategory.Race, Class = "Superbike", Power = 237, Weight = 167, TopSpeed = 315, ImagePath = "/images/bikes/DucatiV4r.jpg" },
                new Vehicle { Engine = "Inline 4", Name = "YAMAHA R1", Manufacturer = "Yamaha", VehicleType = VehicleType.Bike, VehicleCategory = VehicleCategory.Race, Class = "Superbike", Power = 200, Weight = 201, TopSpeed = 299, ImagePath = "/images/bikes/YamahaR1.jpg" },
                new Vehicle { Engine = "Twin-Turbo V6 Hybrid", Name = "FERRARI 296 GTB", Manufacturer = "Ferrari", VehicleType = VehicleType.Car, VehicleCategory = VehicleCategory.Production, Class = "Supercar", Power = 818, Weight = 1470, TopSpeed = 330, ImagePath = "/images/cars/Ferrari 296 GTB.jpg" },
                new Vehicle { Engine = "Naturally Aspirated V10", Name = "LAMBORGHINI HURACAN STO", Manufacturer = "Lamborghini", VehicleType = VehicleType.Car, VehicleCategory = VehicleCategory.Production, Class = "Supercar", Power = 631, Weight = 1339, TopSpeed = 310, ImagePath = "/images/cars/Lamborghini Huracan STO.jpg" },
                new Vehicle { Engine = "Twin-Turbo Inline 6", Name = "BMW M4 CSL", Manufacturer = "BMW", VehicleType = VehicleType.Car, VehicleCategory = VehicleCategory.Production, Class = "Sports Car", Power = 543, Weight = 1625, TopSpeed = 307, ImagePath = "/images/cars/BMW M4 csl.jpg" },
                new Vehicle { Engine = "Twin-Turbo V8", Name = "MCLAREN 750S", Manufacturer = "McLaren", VehicleType = VehicleType.Car, VehicleCategory = VehicleCategory.Production, Class = "Supercar", Power = 740, Weight = 1277, TopSpeed = 332, ImagePath = "/images/cars/McLaren750s.jpg" },
                new Vehicle { Engine = "Inline 4", Name = "BMW M 1000 RR", Manufacturer = "BMW", VehicleType = VehicleType.Bike, VehicleCategory = VehicleCategory.Production, Class = "Superbike", Power = 212, Weight = 192, TopSpeed = 314, ImagePath = "/images/bikes/BmwM1000RR.jpg" },
                new Vehicle { Engine = "Inline 4", Name = "KAWASAKI NINJA ZX-10R", Manufacturer = "Kawasaki", VehicleType = VehicleType.Bike, VehicleCategory = VehicleCategory.Production, Class = "Superbike", Power = 203, Weight = 207, TopSpeed = 299, ImagePath = "/images/bikes/Kawasaki ZX10R.jpg" },
                new Vehicle { Engine = "Inline 4", Name = "HONDA CBR1000RR-R", Manufacturer = "Honda", VehicleType = VehicleType.Bike, VehicleCategory = VehicleCategory.Production, Class = "Superbike", Power = 214, Weight = 201, TopSpeed = 299, ImagePath = "/images/bikes/2020 Honda CBR1000RR-R.jpg" }
            };
            _context.Vehicles.AddRange(vehicles);
            needsSave = true;
        }

        // Reconcile and fix any mismatched image paths on disk for known vehicles
        foreach (var v in vehicles)
        {
            if (v.Name == "CADILLAC V-SERIES.R" && v.ImagePath != "/images/cars/CadillacVSeriesR.jpg") { v.ImagePath = "/images/cars/CadillacVSeriesR.jpg"; needsSave = true; }
            if (v.Name == "AUDI R8 LMS GT3" && v.ImagePath != "/images/cars/AudiR8LMS.jpg") { v.ImagePath = "/images/cars/AudiR8LMS.jpg"; needsSave = true; }
            if (v.Name == "PORSCHE 963" && v.ImagePath != "/images/cars/Porsche963Studio.jpg") { v.ImagePath = "/images/cars/Porsche963Studio.jpg"; needsSave = true; }
            if (v.Name == "FERRARI 296 GTB" && v.ImagePath != "/images/cars/Ferrari 296 GTB.jpg") { v.ImagePath = "/images/cars/Ferrari 296 GTB.jpg"; needsSave = true; }
            if (v.Name == "LAMBORGHINI HURACAN STO" && v.ImagePath != "/images/cars/Lamborghini Huracan STO.jpg") { v.ImagePath = "/images/cars/Lamborghini Huracan STO.jpg"; needsSave = true; }
            if (v.Name == "BMW M4 CSL" && v.ImagePath != "/images/cars/BMW M4 csl.jpg") { v.ImagePath = "/images/cars/BMW M4 csl.jpg"; needsSave = true; }
            if (v.Name == "MCLAREN 750S" && v.ImagePath != "/images/cars/McLaren750s.jpg") { v.ImagePath = "/images/cars/McLaren750s.jpg"; needsSave = true; }
            if (v.Name == "BMW M 1000 RR" && v.ImagePath != "/images/bikes/BmwM1000RR.jpg") { v.ImagePath = "/images/bikes/BmwM1000RR.jpg"; needsSave = true; }
            if (v.Name == "KAWASAKI NINJA ZX-10R" && v.ImagePath != "/images/bikes/Kawasaki ZX10R.jpg") { v.ImagePath = "/images/bikes/Kawasaki ZX10R.jpg"; needsSave = true; }
            if (v.Name == "HONDA CBR1000RR-R" && v.ImagePath != "/images/bikes/2020 Honda CBR1000RR-R.jpg") { v.ImagePath = "/images/bikes/2020 Honda CBR1000RR-R.jpg"; needsSave = true; }
            if (v.Name == "FERRARI F1" && v.ImagePath != "/images/cars/FerrariF1Studio.jpg") { v.ImagePath = "/images/cars/FerrariF1Studio.jpg"; needsSave = true; }
            if (v.Name.Contains("DESMOSEDICI") && v.ImagePath != "/images/bikes/MotoGPStudio.jpg") { v.ImagePath = "/images/bikes/MotoGPStudio.jpg"; needsSave = true; }
        }

        if (needsSave)
        {
            _context.SaveChanges();
        }

        // Ensure Cadillac V-Series.R is primary/first in the garage list
        var orderedVehicles = vehicles
            .OrderByDescending(v => (v.Name.Contains("CADILLAC") || v.Name.Contains("V-SERIES")) ? 1 : 0)
            .ThenByDescending(v => v.VehicleCategory == VehicleCategory.Race ? 1 : 0)
            .ThenBy(v => v.Name)
            .ToList();

        return View(orderedVehicles);
    }

    public IActionResult AddVehicle()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddVehicle([FromForm] Vehicle? vehicle, IFormFile? ImageFile = null)
    {
        if (vehicle == null)
        {
            return BadRequest(new { success = false, message = "Vehicle data is required." });
        }
        vehicle.Engine = vehicle.Engine ?? "Unknown"; 
        vehicle.Class = vehicle.Class ?? "Unclassified";

        if (ImageFile != null && ImageFile.Length > 0)
        {
            var subFolder = vehicle.VehicleType == VehicleType.Bike ? "bikes" : "cars";
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", subFolder);
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageFile.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await ImageFile.CopyToAsync(fileStream);
            }
            
            vehicle.ImagePath = $"/images/{subFolder}/{uniqueFileName}";
        }

        if (string.IsNullOrEmpty(vehicle.ImagePath))
        {
            vehicle.ImagePath = vehicle.VehicleType == VehicleType.Bike 
                ? "/images/bikes/default-bike.jpg"
                : "/images/cars/default-car.jpg";
        }

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> GetVehicle(int? id)
    {
        int targetId = id ?? 0;
        if (targetId <= 0)
        {
            return BadRequest(new { success = false, message = "Valid Vehicle ID is required." });
        }

        var vehicle = await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.VehicleId == targetId);
        if (vehicle == null)
        {
            return NotFound(new { success = false, message = $"Vehicle ID {targetId} was not found." });
        }

        return Json(new
        {
            success = true,
            vehicle = new
            {
                id = vehicle.VehicleId,
                name = vehicle.Name,
                manufacturer = vehicle.Manufacturer,
                type = vehicle.VehicleType.ToString().ToLower(),
                category = vehicle.VehicleCategory.ToString().ToLower(),
                vClass = vehicle.Class ?? (vehicle.VehicleType == VehicleType.Car ? "Race Car" : "Superbike"),
                engine = vehicle.Engine ?? "Race Engine",
                power = vehicle.Power,
                weight = vehicle.Weight,
                topSpeed = vehicle.TopSpeed,
                imagePath = vehicle.ImagePath
            }
        });
    }

    [HttpGet("/api/vehicles/{id:int}")]
    public async Task<IActionResult> GetVehicleApi(int id)
    {
        return await GetVehicle(id);
    }

    [HttpPost]
    public async Task<IActionResult> EditVehicle(int? id, [FromForm] Vehicle? formVehicle, IFormFile? ImageFile)
    {
        Vehicle? updatedVehicle = formVehicle;

        // Support application/json payloads as well
        if (Request.HasJsonContentType())
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var jsonBody = await reader.ReadToEndAsync();
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                updatedVehicle = System.Text.Json.JsonSerializer.Deserialize<Vehicle>(jsonBody, options);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Invalid JSON payload: " + ex.Message });
            }
        }

        int targetId = id ?? updatedVehicle?.VehicleId ?? 0;
        if (targetId <= 0)
        {
            return BadRequest(new { success = false, message = "Valid Vehicle ID is required." });
        }

        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == targetId);
        if (vehicle == null)
        {
            return NotFound(new { success = false, message = $"Vehicle ID {targetId} not found." });
        }

        if (updatedVehicle == null)
        {
            return BadRequest(new { success = false, message = "Vehicle data is required." });
        }

        // Validate stats
        if (string.IsNullOrWhiteSpace(updatedVehicle.Name))
        {
            return BadRequest(new { success = false, message = "Vehicle name cannot be blank." });
        }
        if (string.IsNullOrWhiteSpace(updatedVehicle.Manufacturer))
        {
            return BadRequest(new { success = false, message = "Manufacturer cannot be blank." });
        }
        if (updatedVehicle.Power <= 0 || updatedVehicle.Power > 3000)
        {
            return BadRequest(new { success = false, message = "Horsepower must be a positive value between 1 and 3000 HP." });
        }
        if (updatedVehicle.Weight <= 0 || updatedVehicle.Weight > 10000)
        {
            return BadRequest(new { success = false, message = "Weight must be a positive value between 1 and 10000 kg." });
        }
        if (updatedVehicle.TopSpeed <= 0 || updatedVehicle.TopSpeed > 600)
        {
            return BadRequest(new { success = false, message = "Top speed must be a positive value between 1 and 600 km/h." });
        }

        vehicle.Name = updatedVehicle.Name.Trim().ToUpper();
        vehicle.Manufacturer = updatedVehicle.Manufacturer.Trim();
        vehicle.VehicleType = updatedVehicle.VehicleType;
        vehicle.VehicleCategory = updatedVehicle.VehicleCategory;
        vehicle.Class = !string.IsNullOrWhiteSpace(updatedVehicle.Class) ? updatedVehicle.Class.Trim() : (vehicle.VehicleType == VehicleType.Car ? "Race Car" : "Superbike");
        vehicle.Engine = !string.IsNullOrWhiteSpace(updatedVehicle.Engine) ? updatedVehicle.Engine.Trim() : (vehicle.VehicleType == VehicleType.Car ? "Hybrid V8" : "Inline-4");
        vehicle.Power = updatedVehicle.Power;
        vehicle.Weight = updatedVehicle.Weight;
        vehicle.TopSpeed = updatedVehicle.TopSpeed;

        if (ImageFile != null && ImageFile.Length > 0)
        {
            var subFolder = vehicle.VehicleType == VehicleType.Bike ? "bikes" : "cars";
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", subFolder);
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageFile.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await ImageFile.CopyToAsync(fileStream);
            }

            vehicle.ImagePath = $"/images/{subFolder}/{uniqueFileName}";
        }

        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            vehicle = new
            {
                id = vehicle.VehicleId,
                name = vehicle.Name,
                manufacturer = vehicle.Manufacturer,
                type = vehicle.VehicleType.ToString().ToLower(),
                category = vehicle.VehicleCategory.ToString().ToLower(),
                vClass = vehicle.Class,
                engine = vehicle.Engine,
                power = vehicle.Power,
                weight = vehicle.Weight,
                topSpeed = vehicle.TopSpeed,
                imagePath = vehicle.ImagePath
            }
        });
    }

    [HttpPost("/api/vehicles/{id:int}")]
    public async Task<IActionResult> EditVehicleApi(int id, [FromForm] Vehicle? formVehicle, IFormFile? ImageFile)
    {
        return await EditVehicle(id, formVehicle, ImageFile);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteVehicle([FromForm] int? id)
    {
        int targetId = id ?? 0;
        if (targetId <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid Vehicle ID." });
        }

        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == targetId);
        if (vehicle == null)
        {
            return NotFound(new { success = false, message = $"Vehicle ID {targetId} not found." });
        }

        string vehicleName = vehicle.Name;
        string? imagePath = vehicle.ImagePath;

        _context.Database.SetCommandTimeout(180);
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Identify all sessions strictly belonging to this vehicle
            var sessionIds = await _context.Sessions
                .Where(s => s.VehicleId == targetId)
                .Select(s => s.SessionId)
                .ToListAsync();

            if (sessionIds.Count > 0)
            {
                // 2. Identify all laps belonging to these sessions
                var lapIds = await _context.Laps
                    .Where(l => sessionIds.Contains(l.SessionId))
                    .Select(l => l.LapId)
                    .ToListAsync();

                if (lapIds.Count > 0)
                {
                    // 3. Batch delete TelemetryPoints at the database level to prevent memory exhaustion and lock escalation
                    string lapIdsFormatted = string.Join(",", lapIds);
                    int deletedInBatch;
                    do
                    {
                        deletedInBatch = await _context.Database.ExecuteSqlRawAsync(
                            $"DELETE TOP (10000) FROM TelemetryPoints WHERE LapId IN ({lapIdsFormatted});");
                    } while (deletedInBatch > 0);

                    // 4. Delete AnalysisResults
                    await _context.AnalysisResults
                        .Where(ar => lapIds.Contains(ar.LapId))
                        .ExecuteDeleteAsync();

                    // 5. Delete Laps
                    await _context.Laps
                        .Where(l => lapIds.Contains(l.LapId))
                        .ExecuteDeleteAsync();
                }

                // 6. Delete Session-level dependent records
                await _context.SessionNotes
                    .Where(sn => sessionIds.Contains(sn.SessionId))
                    .ExecuteDeleteAsync();

                await _context.VehicleSetups
                    .Where(vs => sessionIds.Contains(vs.SessionId))
                    .ExecuteDeleteAsync();

                await _context.SessionConditions
                    .Where(sc => sessionIds.Contains(sc.SessionId))
                    .ExecuteDeleteAsync();

                // 7. Delete Sessions
                await _context.Sessions
                    .Where(s => sessionIds.Contains(s.SessionId))
                    .ExecuteDeleteAsync();
            }

            // 8. Delete Vehicle entity
            await _context.Vehicles
                .Where(v => v.VehicleId == targetId)
                .ExecuteDeleteAsync();

            await transaction.CommitAsync();

            // 9. Clean up uploaded vehicle image if applicable
            if (!string.IsNullOrWhiteSpace(imagePath) && imagePath.Contains("_"))
            {
                try
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/', '\\'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
                catch { /* Ignore non-fatal file cleanup errors */ }
            }

            _logger.LogInformation("Successfully deleted vehicle {VehicleName} (ID: {VehicleId}) and all associated telemetry records.", vehicleName, targetId);
            return Json(new { success = true, deletedId = targetId, name = vehicleName });
        }
        catch (Exception ex)
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch
            {
                // Transaction may have already been aborted or completed by the database
            }

            var sb = new System.Text.StringBuilder();
            var curr = ex;
            int depth = 0;
            while (curr != null)
            {
                sb.AppendLine($"[Level {depth}] {curr.GetType().FullName}: {curr.Message}");
                curr = curr.InnerException;
                depth++;
            }
            _logger.LogError(ex, "Failed to delete vehicle ID {VehicleId}. Detailed Exception Chain:\n{Chain}", targetId, sb.ToString());

            var rootCause = ex.GetBaseException().Message;
            return StatusCode(500, new { success = false, message = "Failed to delete vehicle: " + rootCause });
        }
    }

    [HttpPost("/api/vehicles/{id:int}/delete")]
    [HttpDelete("/api/vehicles/{id:int}")]
    public async Task<IActionResult> DeleteVehicleApi(int id)
    {
        return await DeleteVehicle(id);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}


