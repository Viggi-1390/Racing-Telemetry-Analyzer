using Microsoft.AspNetCore.Mvc;
using RacingTelemetryAnalyzer.Models;
using RacingTelemetryAnalyzer.Data;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace RacingTelemetryAnalyzer.Controllers
{
    public class TrackController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrackController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int vehicleId = 0)
        {
            var tracks = _context.Tracks.ToList();
            
            if (tracks.Count < 6)
            {
                var defaultTracks = new List<Track>
                {
                    new Track { Name = "LE MANS", Country = "France", Length = 13.626, NumberOfTurns = 38, ImagePath = "/images/tracks/LeMans.jpg" },
                    new Track { Name = "SPA-FRANCORCHAMPS", Country = "Belgium", Length = 7.004, NumberOfTurns = 19, ImagePath = "/images/tracks/Spa%20Francochamps.jpg" },
                    new Track { Name = "MONZA", Country = "Italy", Length = 5.793, NumberOfTurns = 11, ImagePath = "/images/tracks/Monza.jpg" },
                    new Track { Name = "SUZUKA", Country = "Japan", Length = 5.807, NumberOfTurns = 18, ImagePath = "/images/tracks/Suzuka.jpg" },
                    new Track { Name = "NURBURGRING", Country = "Germany", Length = 20.832, NumberOfTurns = 73, ImagePath = "/images/tracks/Nurburging.jpg" },
                    new Track { Name = "SILVERSTONE", Country = "UK", Length = 5.891, NumberOfTurns = 18, ImagePath = "/images/tracks/Silverstone.jpg" }
                };
                
                foreach (var dt in defaultTracks)
                {
                    if (!tracks.Any(t => t.Name == dt.Name))
                    {
                        _context.Tracks.Add(dt);
                        tracks.Add(dt);
                    }
                }
                _context.SaveChanges();
            }

            // Force update all paths to correct filenames
            bool needsSave = false;
            foreach (var t in tracks)
            {
                if (t.Name.ToUpper() == "LE MANS" && t.ImagePath != "/images/tracks/LeMans.jpg") { t.ImagePath = "/images/tracks/LeMans.jpg"; needsSave = true; }
                if (t.Name.ToUpper() == "SPA-FRANCORCHAMPS" && t.ImagePath != "/images/tracks/Spa%20Francochamps.jpg") { t.ImagePath = "/images/tracks/Spa%20Francochamps.jpg"; needsSave = true; }
                if (t.Name.ToUpper() == "MONZA" && t.ImagePath != "/images/tracks/Monza.jpg") { t.ImagePath = "/images/tracks/Monza.jpg"; needsSave = true; }
                if (t.Name.ToUpper() == "SUZUKA" && t.ImagePath != "/images/tracks/Suzuka.jpg") { t.ImagePath = "/images/tracks/Suzuka.jpg"; needsSave = true; }
                if (t.Name.ToUpper() == "NURBURGRING" && t.ImagePath != "/images/tracks/Nurburging.jpg") { t.ImagePath = "/images/tracks/Nurburging.jpg"; needsSave = true; }
                if (t.Name.ToUpper() == "SILVERSTONE" && t.ImagePath != "/images/tracks/Silverstone.jpg") { t.ImagePath = "/images/tracks/Silverstone.jpg"; needsSave = true; }
            }
            if (needsSave) _context.SaveChanges();

            ViewBag.VehicleId = vehicleId;
            return View(tracks);
        }

        public IActionResult AddTrack(int vehicleId = 0)
        {
            ViewBag.VehicleId = vehicleId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddTrack(Track track, IFormFile ImageFile, [FromQuery] int vehicleId = 0)
        {
            if (string.IsNullOrWhiteSpace(track.Name) || string.IsNullOrWhiteSpace(track.Country) || track.Length <= 0 || track.NumberOfTurns <= 0)
            {
                ModelState.AddModelError("", "Please provide all required fields correctly.");
                ViewBag.VehicleId = vehicleId;
                return View(track);
            }

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "tracks");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(fileStream);
                }
                
                track.ImagePath = $"/images/tracks/{uniqueFileName}";
            }

            if (string.IsNullOrEmpty(track.ImagePath))
            {
                track.ImagePath = "/images/tracks/default-track.jpg";
            }

            _context.Tracks.Add(track);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { vehicleId = vehicleId });
        }
    }
}
