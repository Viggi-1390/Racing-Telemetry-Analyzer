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

    public IActionResult Index()
    {
        var vehicles = _context.Vehicles.ToList();
        bool needsSave = false;
        
        // Seed default vehicles if none exist in the database yet
        if (!vehicles.Any())
        {
            vehicles = new List<Vehicle>
            {
              new Vehicle { Engine = "Hybrid V8", Name = "PORSCHE 963", Manufacturer = "Porsche", VehicleType = VehicleType.Car, VehicleCategory = VehicleCategory.Race, Class = "LMDh", Power = 680, Weight = 1030, TopSpeed = 330, ImagePath = "/images/cars/Porsche963.jpg" },
              new Vehicle { Engine = "Hybrid V6", Name = "FERRARI 499P", Manufacturer = "Ferrari", VehicleType = VehicleType.Car, VehicleCategory = VehicleCategory.Race, Class = "Hypercar", Power = 670, Weight = 1030, TopSpeed = 340, ImagePath = "/images/cars/Ferrari499p.jpg" },
              new Vehicle { Engine = "Hybrid V8", Name = "BMW M HYBRID V8", Manufacturer = "BMW", VehicleType = VehicleType.Car, VehicleCategory = VehicleCategory.Race, Class = "LMDh", Power = 640, Weight = 1030, TopSpeed = 325, ImagePath = "/images/cars/Bmwmhybrid.jpg" },
              new Vehicle { Engine = "V4", Name = "DUCATI PANIGALE V4 R", Manufacturer = "Ducati", VehicleType = VehicleType.Bike, VehicleCategory = VehicleCategory.Race, Class = "Superbike", Power = 237, Weight = 167, TopSpeed = 315, ImagePath = "/images/bikes/DucatiV4r.jpg" },
              new Vehicle { Engine = "Inline 4", Name = "YAMAHA R1", Manufacturer = "Yamaha", VehicleType = VehicleType.Bike, VehicleCategory = VehicleCategory.Race, Class = "Superbike", Power = 200, Weight = 201, TopSpeed = 299, ImagePath = "/images/bikes/YamahaR1.jpg" }
            };
            _context.Vehicles.AddRange(vehicles);
            needsSave = true;
        }

        // Remove any obsolete or duplicate entries if present
        var duplicateFerraris = vehicles.Where(v => v.Name == "FerrariF1").ToList();
        if (duplicateFerraris.Any())
        {
            _context.Vehicles.RemoveRange(duplicateFerraris);
            vehicles.RemoveAll(v => v.Name == "FerrariF1");
            needsSave = true;
        }

        var prodGt3rs = vehicles.Where(v => v.Name == "PORSCHE 911 GT3 RS" && v.VehicleCategory == VehicleCategory.Production).ToList();
        if (prodGt3rs.Any())
        {
            _context.Vehicles.RemoveRange(prodGt3rs);
            vehicles.RemoveAll(v => v.Name == "PORSCHE 911 GT3 RS" && v.VehicleCategory == VehicleCategory.Production);
            needsSave = true;
        }

        // Add single official FERRARI F1 if missing
        if (!vehicles.Any(v => v.Name == "FERRARI F1"))
        {
            var f1 = new Vehicle { Engine = "1.6L Turbo Hybrid V6", Name = "FERRARI F1", Manufacturer = "Ferrari", VehicleType = VehicleType.Car, VehicleCategory = VehicleCategory.Race, Class = "Formula 1", Power = 1000, Weight = 798, TopSpeed = 360, ImagePath = "/images/cars/9dd3c356-295c-4ef9-93bf-a24f413130e6_F1ferrari.jpg" };
            _context.Vehicles.Add(f1);
            vehicles.Add(f1);
            needsSave = true;
        }

        // Add Porsche 963 if missing
        if (!vehicles.Any(v => v.Name.Contains("PORSCHE 963")))
        {
            var porsche = new Vehicle { Engine = "Hybrid V8", Name = "PORSCHE 963", Manufacturer = "Porsche", VehicleType = VehicleType.Car, VehicleCategory = VehicleCategory.Race, Class = "LMDh", Power = 680, Weight = 1030, TopSpeed = 330, ImagePath = "/images/cars/Porsche963.jpg" };
            _context.Vehicles.Add(porsche);
            vehicles.Insert(0, porsche);
            needsSave = true;
        }

        // Add production vehicles if missing
        if (!vehicles.Any(v => v.Name == "FERRARI 296 GTB"))
        {
            var productionVehicles = new List<Vehicle>
            {
                new Vehicle { Engine = "Twin-Turbo V6 Hybrid", Name = "FERRARI 296 GTB", Manufacturer = "Ferrari", VehicleType = VehicleType.Car, VehicleCategory = VehicleCategory.Production, Class = "Supercar", Power = 818, Weight = 1470, TopSpeed = 330, ImagePath = "/images/cars/Ferrari 296 GTB.jpg" },
                new Vehicle { Engine = "Naturally Aspirated V10", Name = "LAMBORGHINI HURACAN STO", Manufacturer = "Lamborghini", VehicleType = VehicleType.Car, VehicleCategory = VehicleCategory.Production, Class = "Supercar", Power = 631, Weight = 1339, TopSpeed = 310, ImagePath = "/images/cars/Lamborghini Huracan STO.jpg" },
                new Vehicle { Engine = "Twin-Turbo Inline 6", Name = "BMW M4 CSL", Manufacturer = "BMW", VehicleType = VehicleType.Car, VehicleCategory = VehicleCategory.Production, Class = "Sports Car", Power = 543, Weight = 1625, TopSpeed = 307, ImagePath = "/images/cars/BMW M4 csl.jpg" },
                new Vehicle { Engine = "Twin-Turbo V8", Name = "MCLAREN 750S", Manufacturer = "McLaren", VehicleType = VehicleType.Car, VehicleCategory = VehicleCategory.Production, Class = "Supercar", Power = 740, Weight = 1277, TopSpeed = 332, ImagePath = "/images/cars/McLaren750s.jpg" },

                new Vehicle { Engine = "Inline 4", Name = "BMW M 1000 RR", Manufacturer = "BMW", VehicleType = VehicleType.Bike, VehicleCategory = VehicleCategory.Production, Class = "Superbike", Power = 212, Weight = 192, TopSpeed = 314, ImagePath = "/images/bikes/BmwM1000RR.jpg" },
                new Vehicle { Engine = "Inline 4", Name = "KAWASAKI NINJA ZX-10R", Manufacturer = "Kawasaki", VehicleType = VehicleType.Bike, VehicleCategory = VehicleCategory.Production, Class = "Superbike", Power = 203, Weight = 207, TopSpeed = 299, ImagePath = "/images/bikes/Kawasaki ZX10R.jpg" },
                new Vehicle { Engine = "Inline 4", Name = "HONDA CBR1000RR-R", Manufacturer = "Honda", VehicleType = VehicleType.Bike, VehicleCategory = VehicleCategory.Production, Class = "Superbike", Power = 214, Weight = 201, TopSpeed = 299, ImagePath = "/images/bikes/2020 Honda CBR1000RR-R.jpg" }
            };
            _context.Vehicles.AddRange(productionVehicles);
            vehicles.AddRange(productionVehicles);
            needsSave = true;
        }

        // Reconcile and fix any mismatched image paths on disk for known vehicles
        foreach (var v in vehicles)
        {
            if (v.Name == "FERRARI 296 GTB" && v.ImagePath != "/images/cars/Ferrari 296 GTB.jpg") { v.ImagePath = "/images/cars/Ferrari 296 GTB.jpg"; needsSave = true; }
            if (v.Name == "LAMBORGHINI HURACAN STO" && v.ImagePath != "/images/cars/Lamborghini Huracan STO.jpg") { v.ImagePath = "/images/cars/Lamborghini Huracan STO.jpg"; needsSave = true; }
            if (v.Name == "BMW M4 CSL" && v.ImagePath != "/images/cars/BMW M4 csl.jpg") { v.ImagePath = "/images/cars/BMW M4 csl.jpg"; needsSave = true; }
            if (v.Name == "MCLAREN 750S" && v.ImagePath != "/images/cars/McLaren750s.jpg") { v.ImagePath = "/images/cars/McLaren750s.jpg"; needsSave = true; }
            if (v.Name == "BMW M 1000 RR" && v.ImagePath != "/images/bikes/BmwM1000RR.jpg") { v.ImagePath = "/images/bikes/BmwM1000RR.jpg"; needsSave = true; }
            if (v.Name == "KAWASAKI NINJA ZX-10R" && v.ImagePath != "/images/bikes/Kawasaki ZX10R.jpg") { v.ImagePath = "/images/bikes/Kawasaki ZX10R.jpg"; needsSave = true; }
            if (v.Name == "HONDA CBR1000RR-R" && v.ImagePath != "/images/bikes/2020 Honda CBR1000RR-R.jpg") { v.ImagePath = "/images/bikes/2020 Honda CBR1000RR-R.jpg"; needsSave = true; }
            if (v.Name == "FERRARI F1" && v.ImagePath != "/images/cars/9dd3c356-295c-4ef9-93bf-a24f413130e6_F1ferrari.jpg") { v.ImagePath = "/images/cars/9dd3c356-295c-4ef9-93bf-a24f413130e6_F1ferrari.jpg"; needsSave = true; }
        }

        if (needsSave)
        {
            _context.SaveChanges();
        }

        return View(vehicles);
    }

    public IActionResult AddVehicle()
    {
        return View();
    }

        [HttpPost]
    public async Task<IActionResult> AddVehicle(Vehicle vehicle, IFormFile ImageFile)
    {
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

        var vehicle = await _context.Vehicles.Include(v => v.Sessions).FirstOrDefaultAsync(v => v.VehicleId == targetId);
        if (vehicle == null)
        {
            return NotFound(new { success = false, message = $"Vehicle ID {targetId} not found." });
        }

        try
        {
            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
            return Json(new { success = true, deletedId = targetId, name = vehicle.Name });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete vehicle ID {VehicleId}", targetId);
            return StatusCode(500, new { success = false, message = "Failed to delete vehicle: " + ex.Message });
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


