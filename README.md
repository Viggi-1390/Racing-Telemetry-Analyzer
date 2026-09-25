# 🏁 Racing Telemetry Analyzer

**A pit-lane engineering workstation for motorsport telemetry analysis** — built with ASP.NET Core MVC (.NET 8), C#, SQL Server, and Entity Framework Core.

Transform raw telemetry into actionable performance insights. Analyze lap data, replay sessions, compare braking zones, and optimize your racing line—all in one unified dashboard.

---

## 🏁 Mission

> **Turn raw telemetry into engineering insight.**

Built for motorsport enthusiasts and engineers who demand precision. Whether you're analyzing track days, sim racing, or developing racing software—this workstation gives you the tools to understand your data, replay your sessions, and improve your craft.

**Analyze. Replay. Understand. Improve. 🏎️**

---

## 🚗 What's In The Garage?

### Vehicle Management
- **Multi-platform support** — Cars (4-wheel suspension, steering) and motorcycles (lean angle, dual braking)
- **Vehicle categories** — GT3 Racing, GTP-LMDh, Formula 1, Motorcycle, Production, and All Machines
- **Custom images** — Upload vehicle liveries and reference photos
- **CRUD operations** — Add, edit, delete with right-click context menu
- **Vehicle-specific telemetry isolation** — Telemetry is strictly isolated by VehicleId, TrackId, and SessionId
- **No automatic telemetry creation** — Adding a vehicle creates the vehicle only; telemetry is imported or explicitly generated

### 🏁 Track Management
- **Track registry** — Name, country, length, corner count, track maps
- **Track sections** — Break circuits into analyzable segments
- **CRUD operations** — Right-click context menu for quick edits
- **Session linkage** — Telemetry tied directly to the selected track
- **Track-specific telemetry isolation** — Switching tracks never falls back to another track's session
- **Track selection UI** — Dedicated track selection flow integrated with the red/black cinematic visual system

### 📊 Telemetry Import (The Pit Telemetry Feed)
CSV import supports **all critical channels**:
- Timestamp, lap distance, speed, RPM, gear
- Throttle %, brake %, steering angle
- **Car-specific:** Suspension (FL/FR/RL/RR), steering, lateral/longitudinal/vertical G-force, position (X/Z)
- **Bike-specific:** Lean angle, front/rear brake %, front/rear suspension
- **Batch processing** for datasets up to 250 MB (original telemetry points retained)

**Real-world test dataset:** Audi R8 LMS GT3 @ Laguna Seca — 63,000 telemetry points across 10 laps (speed, RPM, gear, throttle, brake, steering, suspension, G-force).

### 🔄 Motorsport Sessions
Fully-modeled race calendar:
- Practice 1, 2, 3 → Qualifying → Sprint Qualifying → Sprint Race → Race
- Multiple sessions per vehicle (no auto-generation)
- Session selection without overwriting existing data

### 📈 Telemetry Workstation
Your digital engineer's console includes:
- **Interactive telemetry graph** — Speed, RPM, throttle, brake, steering and other available channels
- **Responsive engineering dashboard** — Large-screen 16:9 layout that expands into the available viewport
- **Lap list & analysis** — Best lap, lap deltas, comparative performance
- **Session replay** — Chronological playback from Lap 1 through final lap
- **Performance breakdown** — Braking behavior, throttle response, suspension movement, G-force loads
- **Tyre information** — Temperature, pressure, wear percentage, status alerts
- **Track map & weather** — Session-specific track and condition information
- **No-data state** — Clearly identifies when the selected vehicle/track has no telemetry instead of loading unrelated data

### ▶️ Replay (The Onboard Camera Feed)
- Chronological session progression: Lap 1 → 2 → 3 → ... → Final Lap
- Automatic lap continuation
- Stops at checkered flag
- Full telemetry sync during playback

### 🛞 Car vs Motorcycle Separation
**Cars:**
- Four-wheel suspension mapping (FL, FR, RL, RR)
- Steering angle telemetry
- Four-corner G-force & position tracking

**Motorcycles:**
- Front/rear suspension only
- Lean angle & banking data
- Dual braking system (front/rear)
- Two-wheel contact analysis

UI automatically switches layout and tyre-status presentation based on vehicle type.

### 🔬 Performance Analysis (V1 Engine)
Current analysis suite:
- **Lap metrics** — Lap times, best lap, consistency
- **Braking analysis** — Braking point consistency, brake pressure curves, trail-braking effectiveness
- **Throttle application** — Response time, application smoothness, wheel spin indicators
- **Suspension behavior** — Bump compliance, roll rates, damping characteristics
- **G-force analysis** — Longitudinal, lateral, vertical loads across track sections
- **Delta information** — Comparison against best lap or reference baseline
- **Replay synchronization** — Graph cursor, lap progression, and playback remain synchronized across the complete session

*V1 uses deterministic C# calculations and rule-based analysis. AI/ML "race engineer" features are reserved for future iterations.*

### 🎨 Motorsport UI / UX
Unified cinematic red/black engineering aesthetic:
- Dark technical surfaces with matte finishes
- **Red and black visual identity** across Garage, Track Selection, Loading, and Telemetry
- **Cyan telemetry accents** for engineering data
- Red/orange racing accents for actions and warnings
- Technical grid/HUD styling with metric typography
- Motorsport-specific cards, selectors, context menus, and status panels
- Responsive desktop layouts designed around 16:9 / 1920×1080 workstations
- **No unnecessary loading-screen transitions or animations**
- Fast loading flow with only a short intentional visual display time

Design inspiration: Animos, Godly.design, Transitions.dev, Backgrounds Supply (visual direction only—no branding/layout reproduction).

---

## 🗄️ Database Architecture

**SQL Server + Entity Framework Core** with these core entities:

```
Vehicle
  └── Sessions
       ├── Lap (LapNumber, LapTime, IsValid)
       │    ├── TelemetryPoint (Timestamp, Speed, RPM, Throttle, Brake, Steering, G-force, etc.)
       │    └── AnalysisResult (EntrySpeed, MinSpeed, ExitSpeed, Recommendations)
       ├── VehicleSetup (FrontWing, RearWing, RideHeight, BrakeBias)
       ├── SessionCondition (AirTemp, TrackTemp, Weather, WindSpeed, TrackCondition)
       └── SessionNote (Lap-specific or Section-specific annotations)

Track
  └── TrackSection (StartDistance, EndDistance, Name)
```

**High-volume data handling:**
- Telemetry batching for large imports (250 MB server limits configured)
- Database indices optimized for telemetry/session queries
- Efficient foreign key relationships (no data duplication)
- Large datasets are rendered efficiently while retaining the original telemetry data
- Session/vehicle/track filtering prevents unrelated telemetry from being loaded

---

## 🧩 Architecture & Services

| Service | Purpose |
|---------|---------|
| **TelemetryImportService** | CSV parsing, validation, batching, database persistence |
| **TelemetryAnalysisService** | Lap metrics, sector performance, consistency calculations |
| **DataGeneratorService** | Demo telemetry generation for testing |
| **CsvImportService** | File handling, channel mapping, data cleaning |

**API Endpoints:**
- `/api/telemetry` — Query telemetry points by lap/session
- `/api/sessions` — CRUD operations on sessions
- `/api/analysis` — Retrieve performance analysis results

**CORS enabled** for cross-origin requests (dev/testing).

---

## 🛠️ Technology Stack

| Layer | Technology |
|-------|-----------|
| **Language** | C# 12 |
| **Framework** | ASP.NET Core MVC (.NET 8) |
| **Database** | SQL Server + Entity Framework Core 8.0 |
| **Frontend** | HTML5, CSS3, JavaScript (Canvas for graphs) |
| **Data Format** | CSV (import), JSON (API responses) |
| **IDE** | Visual Studio 2026 / VS Code |
| **OS** | Windows 10/11 |
| **VCS** | Git + GitHub |

---

## 📁 Project Structure

```
Racing-Telemetry-Analyzer/
├── Controllers/                    # MVC route handlers
│   ├── HomeController.cs
│   ├── TelemetryController.cs
│   ├── TelemetryApiController.cs
│   └── TrackController.cs
├── Services/                       # Business logic
│   ├── TelemetryImportService.cs
│   ├── TelemetryAnalysisService.cs
│   ├── CsvImportService.cs
│   └── DataGeneratorService.cs
├── Models/                         # Domain entities
│   ├── DomainModels.cs             # Vehicle, Track, Session, Lap, TelemetryPoint, etc.
│   └── ErrorViewModel.cs
├── Data/                           # EF Core context
│   └── ApplicationDbContext.cs
├── Views/                          # Razor views
│   ├── Home/
│   ├── Telemetry/
│   ├── Track/
│   └── Shared/
├── Components/
│   └── Canvas-based telemetry visualization in JavaScript
├── wwwroot/                        # Static assets
│   ├── css/                        # Motorsport styling
│   ├── js/                         # Client-side logic
│   ├── images/
│   │   ├── cars/
│   │   ├── bikes/
│   │   ├── tracks/
│   │   └── loading/                # Cinematic loading-screen assets
│   └── svg/                        # Inline SVG assets
├── Migrations/                     # EF Core schema versions
├── RacingTelemetryAnalyzer.Tests/  # Unit tests
├── appsettings.json                # Configuration
└── README.md                       # You are here
```

---

## 🚀 Quick Start

### Prerequisites
- **.NET 8 SDK** (or later)
- **SQL Server** (or SQL Server LocalDB)
- **Visual Studio 2026** or **VS Code**
- **Windows 10/11** (recommended)

### Clone & Setup
```bash
git clone https://github.com/Viggi-1390/Racing-Telemetry-Analyzer.git
cd Racing-Telemetry-Analyzer
```

### Restore Dependencies
```bash
dotnet restore
```

### Configure SQL Server
Edit `appsettings.json` with your connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=RacingTelemetry;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**Local development example** (SQL Server LocalDB):
```
Server=(localdb)\\mssqllocaldb;Database=RacingTelemetry;Trusted_Connection=True;
```

### Apply Database Migrations
```bash
dotnet ef database update
```

### Build & Run
```bash
dotnet build
dotnet run
```

Open the local URL (typically `https://localhost:7000`).

---

## 📥 Telemetry Workflow (The Pit Strategy)

```
1. Select Vehicle (car or bike)
       ↓
2. Select Track (and review track sections)
       ↓
3. Choose Session Type (Practice, Qualifying, Race, etc.)
       ↓
4. Import Telemetry
       • CSV upload → validation → batch ingestion
       • OR generate demo data for testing
       ↓
5. Create Session
       • Name, date, condition logging
       ↓
6. Build Lap Structure
       • Parse telemetry into laps
       • Validate lap boundaries
       ↓
7. Store Telemetry Points
       • Speed, RPM, throttle, brake, suspension, G-force, etc.
       ↓
8. Open Telemetry Workstation
       • Interactive graphs, lap list, session info
       ↓
9. Analyze & Replay
       • Review performance, spot patterns
       • Replay session chronologically
       • Annotate findings
```

---

## 🧪 Testing

### Run Test Suite
```bash
dotnet build
dotnet test
```

### Regression Test Areas
- Vehicle/Track CRUD (create, read, update, delete)
- CSV import validation and batch processing
- Large telemetry import (63k+ points)
- Multi-lap session handling
- Replay progression (Lap 1 → Final Lap)
- Session selection (no data overwrites)
- Vehicle + Track + Session isolation
- No-data state for a vehicle/track combination
- Car vs. motorcycle telemetry separation
- **Real data:** Audi R8 LMS @ Laguna Seca (10 laps, 63k points)
- Visualization rendering and interactivity
- Responsive telemetry layout at desktop resolutions and browser zoom levels
- Dynamic selected-vehicle loading screen

---

## 🔒 Data Safety Rules (Pit Lane Protocols)

✅ **Must enforce:**
1. Adding a vehicle does NOT automatically create telemetry
2. Telemetry is strictly scoped to the exact **VehicleId + TrackId + SessionId**
3. Adding unrelated vehicles must NOT mutate existing telemetry
4. A selected vehicle/track with no telemetry must show a no-data state rather than falling back to another session
5. The application must NEVER silently switch to another vehicle or track to populate telemetry
6. Loading-screen visuals use the currently selected vehicle image and must not trigger unnecessary telemetry loading
7. Existing telemetry import and session data must remain isolated when new vehicles or tracks are added
8. Vehicle type controls the appropriate car/bike telemetry and status presentation

---

## 🔮 Future Scope (The Upgrade Garage)

Planned features for future iterations:
- **AI Race Engineer** — Automated performance coaching and optimization hints
- **Anomaly Detection** — Automatically flag unusual behavior (spins, lock-ups, etc.)
- **Performance Prediction** — Estimate lap time based on setup changes
- **Driver Comparison** — Head-to-head lap analysis
- **Real-Time Telemetry** — Live streaming from trackside
- **Simulator Integration** — Import telemetry from iRacing, Assetto Corsa, etc.
- **CAN/OBD-II Integration** — Direct vehicle data feeds
- **Advanced Tyre Modelling** — Temperature, compound, degradation analysis
- **Fuel, ERS, DRS Analysis** — Hybrid/electric/DRS energy management
- **3D Visualization** — Vehicle and track 3D rendering
- **Cloud Storage** — Sync sessions across devices
- **Automated Reports** — Not part of the current V1 scope

*These are aspirational and not required for V1.*

---

## 👨‍💻 Project Information

| Field | Value |
|-------|-------|
| **Project Name** | Racing Telemetry Analyzer |
| **Developer** | Vighnesh (Viggi) |
| **Repository** | [GitHub](https://github.com/Viggi-1390/Racing-Telemetry-Analyzer) |

---


