# Codebase Structure

**Analysis Date:** 2026-09-20

## Directory Layout

```text
RacingTelemetryAnalyzer/
├── Controllers/              # MVC and Web API controllers
│   ├── HomeController.cs     # Garage, vehicle spec editing & showcase
│   ├── TelemetryApiController.cs # JSON endpoints for telemetry replay
│   ├── TelemetryController.cs    # Workstation view & session selection
│   └── TrackController.cs    # Circuit catalog & right-click track CRUD
├── Services/                 # Business logic, analytics, import & export
│   ├── CsvImportService.cs   # Streaming CSV parser with batch commits
│   ├── DataGeneratorService.cs # Synthetic telemetry data generator
│   ├── ITelemetryAnalysisService.cs # Contract for telemetry analyzers
│   ├── ReportService.cs      # QuestPDF telemetry report builder
│   └── TelemetryAnalysisService.cs # Rule-based driving advice engine
├── Models/                   # Domain entities and view models
│   ├── DomainModels.cs       # Vehicle, Track, Session, Lap, TelemetryPoint
│   └── ErrorViewModel.cs     # Error handling view model
├── Data/                     # EF Core DbContext and database configurations
│   └── ApplicationDbContext.cs # EF Core DbContext and model mappings
├── Migrations/               # EF Core database migrations
├── Views/                    # Server-rendered Razor views
│   ├── Home/                 # Garage view and vehicle specs modal
│   │   ├── Index.cshtml
│   │   └── AddVehicle.cshtml
│   ├── Telemetry/            # Workstation and staged loading screens
│   │   ├── Index.cshtml
│   │   └── Loading.cshtml
│   ├── Track/                # Track catalog and right-click CRUD modal
│   │   ├── Index.cshtml
│   │   └── AddTrack.cshtml
│   └── Shared/               # Master layout and error templates
│       ├── _Layout.cshtml
│       └── Error.cshtml
├── Components/               # Razor / Blazor components
│   └── TelemetryRollingGraph.razor # Live rolling telemetry visualizer
├── wwwroot/                  # Client-side static assets
│   ├── css/                  # racing-design-system.css, site.css
│   ├── js/                   # Canvas replay engine and client scripts
│   └── images/               # Car, track, and loading vector graphics
├── Program.cs                # Application entry point and DI configuration
├── appsettings.json          # Configuration and SQL connection strings
└── RacingTelemetryAnalyzer.csproj # Project definitions and NuGet packages
```

## Directory Purposes

**`Controllers/`:**
- Purpose: HTTP request processing, Razor view rendering, and JSON API responses.
- Key files: `TelemetryController.cs`, `TelemetryApiController.cs`, `HomeController.cs`, `TrackController.cs`.

**`Services/`:**
- Purpose: Core application business logic, telemetry algorithms, and stream processing.
- Key files: `TelemetryAnalysisService.cs`, `CsvImportService.cs`, `DataGeneratorService.cs`, `ReportService.cs`.

**`Models/`:**
- Purpose: Entity models representing the motorsport domain and view data transfer objects.
- Key files: `DomainModels.cs`.

**`Data/`:**
- Purpose: Data persistence layer using Entity Framework Core.
- Key files: `ApplicationDbContext.cs`.

**`Views/`:**
- Purpose: User interface templates rendered via ASP.NET Core Razor.
- Key files: `Telemetry/Index.cshtml`, `Telemetry/Loading.cshtml`, `Home/Index.cshtml`, `Track/Index.cshtml`.

**`wwwroot/`:**
- Purpose: Static web assets delivered to the client browser.
- Key files: `css/racing-design-system.css`, `images/loading/gt3-racing-loader.svg`, `images/loading/motogp-racing-loader.svg`.

## Naming Conventions

**Files:**
- C# Classes / Interfaces: PascalCase (e.g., `TelemetryAnalysisService.cs`, `ITelemetryAnalysisService.cs`)
- Razor Views: PascalCase (e.g., `Index.cshtml`, `Loading.cshtml`)
- Static Web Assets: kebab-case (e.g., `racing-design-system.css`, `gt3-racing-loader.svg`)

**Directories:**
- C# Namespaces / Folders: PascalCase (e.g., `Controllers`, `Services`, `Models`, `Data`)

## Where to Add New Code

**New Telemetry Metric / Channel:**
- Domain definition: Add property to `TelemetryPoint` in `Models/DomainModels.cs`.
- Analysis logic: Update `AnalyzeLap` in `Services/TelemetryAnalysisService.cs`.
- UI Display: Add channel card in `Views/Telemetry/Index.cshtml` and canvas line plot in `wwwroot/js/telemetry-canvas.js`.

**New Motorsport Vehicle or Track:**
- Seed data: Seed in `Data/ApplicationDbContext.cs` or create via `HomeController.cs` / `TrackController.cs`.
- Static Artwork: Place images in `wwwroot/images/cars/` or `wwwroot/images/tracks/`.

---

*Structure analysis: 2026-09-20*
