# 🏁 Racing Telemetry Analyzer

A high-performance ASP.NET Core application for capturing, analyzing, and optimizing motorsport telemetry data. Think of it as your pit crew chief in digital form—extracting actionable performance insights from every lap.

## 🏎️ Quick Start - Get on Track

### Prerequisites
- **.NET 8.0** (or later)
- **SQL Server** (local or remote)
- **Visual Studio 2026** or VS Code

### Installation & Setup

```bash
# 1. Clone your pit garage
git clone https://github.com/Viggi-1390/Racing-Telemetry-Analyzer.git
cd Racing-Telemetry-Analyzer

# 2. Restore dependencies
dotnet restore

# 3. Update the database connection string in appsettings.json
# Point it to your SQL Server instance

# 4. Apply the race setup (migrations)
dotnet ef database update

# 5. Fire up the engine
dotnet run
```

Navigate to `https://localhost:5001` and you're on the grid! 🚦

---

## 🏁 What's Under the Hood

### Architecture: The Pit Crew Structure

```
Racing Telemetry Analyzer
├── Controllers/         # Race control tower
│   ├── HomeController   # Dashboard & main strategy
│   ├── TelemetryController  # Live data ingestion
│   ├── TelemetryApiController  # Real-time data feeds
│   └── TrackController  # Circuit management
├── Services/            # Performance engineering team
│   ├── TelemetryAnalysisService    # Lap analysis & metrics
│   ├── CsvImportService            # Telemetry data upload
│   ├── DataGeneratorService        # Simulator/test data
│   └── ReportService               # PDF generation
├── Models/              # Vehicle setup specs
│   └── DomainModels.cs  # Vehicle, Track, Session, Lap, Telemetry data
├── Views/               # Driver display screens
│   ├── Home/            # Dashboard & navigation
│   ├── Telemetry/       # Real-time graphs & lap analysis
│   ├── Track/           # Circuit data & sections
│   └── Shared/          # Common layouts
├── Components/          # Interactive instruments
│   └── TelemetryRollingGraph.razor  # Real-time telemetry visualizer
└── Data/                # Garage database
    └── ApplicationDbContext.cs  # EF Core database schema
```

---

## 🚗 Core Features - Pit Box Arsenal

### Vehicle Management
- **Support for multiple vehicle types**: Cars & Bikes
- **Categories**: Race vehicles & Production road cars
- **Detailed specifications**: Engine power, weight, top speed, transmission type
- **Vehicle imagery**: Store photos of your racing machinery

### Track Management
- **Circuit database**: Name, location, length, number of turns
- **Track sections**: Define critical zones (Eau Rouge, Copse Corner, etc.)
- **Visual references**: Store track layout images

### Session Management
- **Multiple session types**: Practice, Qualifying, Race
- **Comprehensive logging**: Vehicle setup, weather conditions, ambient temps
- **Track conditions**: Dry, wet, variable

### Telemetry Data Capture
Granular telemetry points per lap with:
- **Core metrics**: Speed, RPM, gear, throttle, brake input
- **Dimensional data**: Distance covered, timestamp within lap
- **Vehicle-specific sensors**:
  - **Cars**: Steering angle, suspension load (FL/FR/RL/RR)
  - **Bikes**: Lean angle, brake distribution (front/rear)

### Lap Analysis Engine
Advanced performance metrics extracted per lap:
- **Sector times** (Sector 1, 2, 3)
- **Speed delta**: Entry → minimum speed → exit through corners
- **Braking efficiency**: Braking percentage per corner
- **Throttle response time**: How quickly acceleration builds
- **Section-by-section recommendations**: AI-generated setup tips
- **Lap validity tracking**: Flag outliers and invalid runs

### Tyre Telemetry
Real-time tyre health monitoring:
- **Temperature & pressure** tracking
- **Wear percentage** calculation
- **Status indicators**: Optimal, Warning, Critical
- **Smart detection**: Automatically adapts for bikes (2 tyres) vs cars (4 tyres)

### Data Import & Export
- **CSV import**: Upload telemetry logs from data acquisition systems
- **PDF reports**: Generate professional performance reports
- **Session notes**: Attach driver/engineer feedback to sessions or laps

---

## 🛠️ Tech Stack - The Mechanics

| Component | Technology | Version |
|-----------|-----------|---------|
| **Backend** | ASP.NET Core | 8.0+ |
| **Web Framework** | ASP.NET Core MVC + Razor Pages | |
| **Database** | SQL Server + Entity Framework Core | 8.0 |
| **Frontend** | HTML5, CSS3, JavaScript | |
| **Interactive UI** | Razor Components (Blazor) | |
| **Data Parsing** | CsvHelper | 33.1.0+ |
| **PDF Generation** | QuestPDF | 2026.8.0+ |

---

## 📊 Database Schema - The Telemetry Blueprint

### Entities & Relationships

**Vehicle** — The car or bike  
↓ (owns many)  
**Session** — A track outing (practice, qualifying, race)  
├─ **SessionCondition** — Weather & environmental data  
├─ **VehicleSetup** — Suspension, wing, brake bias tuning  
├─ **Lap** — Individual lap in the session  
│  ├─ **TelemetryPoint** — Granular sensor readings per lap  
│  └─ **AnalysisResult** — Corner-by-corner performance analysis  
└─ **SessionNote** — Driver notes or engineer comments  

**Track** — The circuit  
└─ **TrackSection** — Named corners and zones (e.g., "Turn 3 - Radillon")

---

## 🎯 Key Services - The Strategy Team

### TelemetryAnalysisService
Processes raw telemetry data to extract:
- Sector times and lap deltas
- Speed profiles through corners
- Braking efficiency metrics
- Throttle application patterns
- Actionable recommendations for improvement

### CsvImportService
Ingests telemetry from common data acquisition formats and maps them to the telemetry schema.

### DataGeneratorService
Generates synthetic race data for testing and demos—useful for continuous integration or sandbox environments.

### ReportService
Exports session data into professional PDF reports with:
- Lap-by-lap breakdowns
- Performance trends
- Comparative analysis

---

## 🖥️ Views & UI - The Driver's Dashboard

### Home Dashboard
- Session overview
- Vehicle inventory
- Track selection
- Quick links to latest sessions

### Telemetry Analysis View
- **Real-time telemetry graphs** (JavaScript + Razor Components)
- **Lap comparison** (best vs. current)
- **Sector analysis** with detailed metrics
- **Corner-by-corner breakdown**
- **Tyre health indicators**
- **Rolling graphs** for live data streams

### Track Management
- Circuit database browse
- Track section editor
- Condition logging

---

## 🚀 API Endpoints - Pit-to-Car Communications

### TelemetryApiController
Provides REST endpoints for:
- **GET** `/api/telemetry/{sessionId}` — Fetch session telemetry
- **POST** `/api/telemetry/import` — Upload CSV telemetry
- **GET** `/api/analysis/{lapId}` — Get lap analysis results

### TrackController
Endpoints for:
- **GET** `/track/list` — All circuits
- **POST** `/track/create` — New track entry
- **GET** `/track/{trackId}/sections` — Track sections

---

## 📁 Project Structure Deep Dive

```
/Controllers
  ├─ HomeController.cs        → Dashboard, session browsing
  ├─ TelemetryController.cs    → Telemetry UI & data views
  ├─ TelemetryApiController.cs → RESTful API for telemetry
  └─ TrackController.cs        → Track CRUD operations

/Services
  ├─ ITelemetryAnalysisService.cs → Interface for analysis
  ├─ TelemetryAnalysisService.cs   → Core analysis logic
  ├─ CsvImportService.cs           → CSV parsing & import
  ├─ DataGeneratorService.cs       → Test data generation
  └─ ReportService.cs              → PDF export

/Models
  └─ DomainModels.cs    → All entity classes (Vehicle, Track, Session, Lap, etc.)

/Data
  └─ ApplicationDbContext.cs  → EF Core DbContext

/Views
  ├─ Home/           → Dashboard & landing pages
  ├─ Telemetry/      → Live data visualizations
  ├─ Track/          → Circuit management UI
  └─ Shared/         → Layouts & partials

/Components
  └─ TelemetryRollingGraph.razor  → Blazor component for live graphs

/Migrations
  └─ 20260829154959_InitialCreate  → Database schema setup

/wwwroot
  ├─ css/            → Stylesheets
  ├─ js/             → JavaScript utilities
  ├─ images/         → Vehicle & track photos
  └─ lib/            → Third-party libraries
```

---

## 🔧 Configuration - Tuning Your Setup

### appsettings.json
Update the SQL Server connection string to point to your database:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=RacingTelemetry;Trusted_Connection=true;"
  }
}
```

### appsettings.Development.json
Override settings for local development (logging, debugging, etc.).

---

## 📖 Entity Relationships - The Grid

```
Vehicle (1) ──→ (many) Session
            ├─ Name, Manufacturer
            ├─ Power, Weight, TopSpeed
            └─ VehicleType (Car/Bike)

Session (1) ──→ (many) Lap
         ├─ Date, SessionType
         ├─ Vehicle (FK)
         ├─ Track (FK)
         ├─ SessionCondition (1-to-1)
         ├─ VehicleSetup (1-to-1)
         └─ SessionNote (many)

Lap (1) ──→ (many) TelemetryPoint
    ├─ LapNumber
    ├─ LapTime, Sector Times
    ├─ IsValid
    └─ AnalysisResult (many)

TelemetryPoint → Captures:
    ├─ Speed, RPM, Gear
    ├─ Throttle, Brake %
    ├─ Vehicle-specific sensors
    └─ Timestamp, Distance

Track (1) ──→ (many) TrackSection
         ├─ Name, Country, Length
         └─ NumberOfTurns

AnalysisResult → Performance metrics
         ├─ Speed deltas
         ├─ Braking efficiency
         └─ Recommendations
```

---

## 🎮 Usage Scenarios - Pit Crew Workflows

### Scenario 1: Record a Race Weekend
1. Add vehicle to the garage
2. Create a new session (Practice → Qualifying → Race)
3. Log environmental conditions
4. Input vehicle setup (wing angles, brake bias, etc.)
5. Record lap data (manual or CSV import)
6. System auto-generates sector analysis & recommendations
7. Export PDF report for debrief

### Scenario 2: Analyze Telemetry Post-Session
1. Browse completed sessions
2. View real-time telemetry graphs
3. Compare best lap vs. current lap
4. Identify problem corners (high-speed understeer at Turn 7?)
5. Review AI recommendations for setup tweaks
6. Export findings as PDF

### Scenario 3: Track Development
1. Add new circuit to the database
2. Define named sections (Turn 1 - Qualifying Corner, etc.)
3. Log track conditions over time
4. Build a historical performance baseline
5. Use for future session comparisons

---

## 🧪 Testing & Development

### Generating Sample Data
The `DataGeneratorService` creates realistic race data for testing:

```csharp
var testData = await _dataGeneratorService.GenerateSessionDataAsync(vehicleId, trackId);
```

### Running Locally
```bash
# Debug mode with hot reload
dotnet run --environment Development

# Release mode
dotnet build -c Release && dotnet bin/Release/net8.0/RacingTelemetryAnalyzer.dll
```

---

## 🐛 Troubleshooting - Pit Stop Guide

### Database Connection Issues
- Verify SQL Server is running
- Check connection string in `appsettings.json`
- Ensure database has been created with `dotnet ef database update`

### Missing Telemetry Data
- Confirm CSV import file format matches expected schema
- Check `TelemetryPoint` model for required fields

### Performance Graphs Not Loading
- Verify JavaScript files in `/wwwroot/js` are accessible
- Check browser console for errors
- Ensure Razor Components are properly registered

---

## 🤝 Contributing

Developed by **Viggi (Vighnesh)** as a semester project.  
Built with ❤️ and caffeine. 🏁☕

---

## 🔗 Resources

- [ASP.NET Core Docs](https://docs.microsoft.com/en-us/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core)
- [CsvHelper Documentation](https://joshclose.github.io/CsvHelper)
- [QuestPDF](https://www.questpdf.com)

---

**Remember: Good telemetry data is the foundation of good engineering decisions.** 🏁

Keep pushing those lap times! 🚗💨
