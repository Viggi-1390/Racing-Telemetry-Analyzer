🏁 Racing Telemetry Analyzer

A motorsport-focused telemetry analysis web application built with ASP.NET Core MVC (.NET 8), C#, SQL Server, Entity Framework Core, HTML/CSS/JavaScript, and CSV telemetry data.

Racing Telemetry Analyzer is designed as a digital motorsport engineering workstation for organizing vehicles and tracks, importing telemetry, analyzing laps, replaying sessions, and inspecting performance data.

Project: TYBSc IT Semester Project
Academic Year: 2026–2027

🚗 Core Features

Vehicle Management

Cars and motorcycles

Race and production vehicles

Vehicle specifications and custom images

Add, edit and delete vehicles

Right-click vehicle context menu

Vehicle-specific telemetry separation

Adding a vehicle does not automatically create telemetry

🏁 Track Management

Track name, country, length and turn count

Track images/maps

Add, edit and delete tracks

Right-click track context menu

Track selection connected to telemetry sessions

📊 Telemetry Import

CSV telemetry import supports channels including:

Timestamp and distance

Speed, RPM and gear

Throttle and brake

Steering

Car suspension FL/FR/RL/RR

Longitudinal, lateral and vertical G-force

Lap number

Large datasets are processed in batches while retaining the original telemetry points.

🏎️ Real Telemetry Testing

A major regression dataset is Audi R8 LMS GT3 Laguna Seca telemetry, containing approximately 63,000 points across 10 laps with rich speed, RPM, gear, throttle, brake, steering, suspension and G-force data.

It is used to verify the complete pipeline from CSV import → database → API → graph → replay → analysis.

🔄 Motorsport Sessions

Supported session types:

Practice 1

Practice 2

Practice 3

Qualifying

Sprint Qualifying

Sprint Race

Race

Multiple sessions can exist for a vehicle, and existing sessions can be selected without generating new telemetry.

📈 Telemetry Workstation

The Telemetry page includes:

Interactive telemetry graph

Speed, RPM, throttle, brake and steering channels

Lap list and lap analysis

Session information and session selection

Replay controls

G-force information

Suspension information

Braking and throttle analysis

Telemetry import

▶️ Replay

Replay follows the complete chronological session:

Lap 1 → Lap 2 → Lap 3 → ... → Final Lap

An intermediate lap ending should automatically continue into the next lap. Replay stops at the end of the final lap.

🛞 Car vs Motorcycle Telemetry

Cars can use four-corner suspension, steering and four-wheel information.

Motorcycles can use front/rear suspension, lean angle, front/rear braking and two-wheel information.

The motorcycle interface does not use a four-wheel car layout.

🔬 Performance Analysis

Current V1 analysis includes:

Lap times and best lap

Lap-by-lap telemetry

Delta information

Braking behaviour

Throttle application

Suspension behaviour

Longitudinal/lateral/vertical G-force

V1 primarily uses deterministic C# calculations and rule-based analysis. AI/ML race-engineer features are future scope.

🚦 Loading Screen

The loading screen uses two universal motorsport category silhouettes:

GT3 Racing Car

All cars use the same universal GT3-style technical silhouette.

Audi R8 LMS
Cadillac V-Series.R
Ferrari 499P
BMW M Hybrid V8
Future cars
      ↓
Universal GT3 Loading Silhouette

MotoGP Racing Bike

All motorcycles use the same universal MotoGP-style technical silhouette.

Any motorcycle
      ↓
Universal MotoGP Loading Silhouette

The selected vehicle's database/custom image is not used as the loading silhouette.

Loading assets are stored in:

wwwroot/images/loading/

🎨 Motorsport UI / UX

The project uses a unified dark motorsport engineering visual language:

Dark technical surfaces

Cyan telemetry accents

Orange racing accents

Technical grid/HUD styling

Motorsport cards and context menus

Responsive layouts

Subtle transitions and motion

Design inspiration comes from Animos, Godly.design, Transitions.dev, and Backgrounds Supply. These are references for visual and interaction direction, not copied branding or proprietary layouts.

🗄️ Database Model

The project uses SQL Server with Entity Framework Core.

Core entities include:

Vehicle
Track
Session
Lap
TelemetryPoint
TrackSection
VehicleSetup
SessionCondition
AnalysisResult
PersonalRecord
SessionNote
AnalysisSnapshot

High-level relationship:

Vehicle
   │
   └── Sessions
          │
          └── Laps
                 │
                 ├── Telemetry Points
                 └── Analysis Results

Track
   │
   └── Track Sections

🧩 Main Services

TelemetryImportService — CSV ingestion, validation, batching and persistence

TelemetryAnalysisService — telemetry/performance calculations

TelemetryReplayService — chronological replay

LapComparisonService — lap comparison and delta calculations

SuspensionAnalysisService — suspension analysis

BrakingAnalysisService — braking analysis

ThrottleAnalysisService — throttle analysis

TrackAnalysisService — track analysis

DeltaCalculationService — delta calculations

🛠️ Technology Stack

Area

Technology

Language

C#

Framework

ASP.NET Core MVC

Target

.NET 8

ORM

Entity Framework Core

Database

Microsoft SQL Server

Frontend

HTML5, CSS3, JavaScript

Telemetry Visualization

HTML Canvas / JavaScript

Data Import

CSV

IDE

Visual Studio 2026 / VS Code

OS

Windows 10/11

Version Control

Git + GitHub

📁 Project Structure

Racing-Telemetry-Analyzer/
├── Controllers/
├── Services/
├── Models/
├── Data/
├── Views/
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── images/
│       ├── cars/
│       ├── bikes/
│       ├── tracks/
│       └── loading/
├── Migrations/
├── RacingTelemetryAnalyzer.Tests/
└── README.md

🚀 Getting Started

Prerequisites

.NET 8 SDK

SQL Server or SQL Server LocalDB

Visual Studio 2026 or VS Code

Windows 10/11 recommended

Clone

git clone https://github.com/Viggi-1390/Racing-Telemetry-Analyzer.git
cd Racing-Telemetry-Analyzer

Restore

dotnet restore

Configure SQL Server

Set the connection string in appsettings.json.

Example:

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=RacingTelemetry;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}

Apply Migrations

dotnet ef database update

Build and Run

dotnet build
dotnet run

Open the local URL shown by ASP.NET Core.

📥 Telemetry Workflow

Select Vehicle
      ↓
Select Track
      ↓
Select Session Type
      ↓
Import CSV / Generate Demo Data
      ↓
Validate Telemetry
      ↓
Create Session
      ↓
Create Laps
      ↓
Store Telemetry Points
      ↓
Open Telemetry Workstation
      ↓
Analyze / Replay

🧪 Testing

The repository contains:

RacingTelemetryAnalyzer.Tests/

Run:

dotnet build
dotnet test

Important regression areas include vehicle/track CRUD, CSV import, large telemetry imports, multi-lap sessions, replay progression, session selection, car/bike telemetry, R8 LMS telemetry, visualization, and loading-screen selection.

🔒 Data Safety Rules

Adding a vehicle must not automatically create telemetry.

Telemetry belongs to a specific session and vehicle.

Existing telemetry should not be changed when adding unrelated vehicles.

Loading-screen visuals are independent of database vehicle images.

Cars always use the universal GT3 loading silhouette.

Bikes always use the universal MotoGP loading silhouette.

🔮 Future Scope

Possible future versions may include:

AI race-engineer analysis

AI anomaly detection

Performance prediction

Driver comparison/coaching

Real-time telemetry

Simulator integrations

OBD-II/CAN integration

Advanced tyre modelling

Engine/tyre temperature analysis

Fuel, ERS and DRS analysis

Advanced suspension analysis

3D vehicle/track visualization

Cloud session storage

Automated engineering reports

These are future extensions and are not required for the current V1 implementation.

👨‍💻 Project Information

Project: Racing Telemetry Analyzer
Student: Vighnesh (Viggi)
Course: TYBSc IT
Academic Year: 2026–2027

🏁 Mission

Turn raw telemetry into engineering insight.

Built for motorsport analysis and developed as a college project.

🏎️ Analyze. Replay. Understand. Improve.
