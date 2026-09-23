# Coding Conventions

**Analysis Date:** 2026-09-20

## Naming Patterns

**Files:**
- C# Classes and Interfaces: PascalCase matching type name (e.g., `TelemetryAnalysisService.cs`, `ITelemetryAnalysisService.cs`, `DomainModels.cs`)
- Razor Views: PascalCase (e.g., `Index.cshtml`, `Loading.cshtml`, `_Layout.cshtml`)
- CSS Stylesheets: kebab-case (e.g., `racing-design-system.css`, `site.css`)
- Vector Graphics & Assets: kebab-case (e.g., `gt3-racing-loader.svg`, `motogp-racing-loader.svg`)

**Functions & Methods:**
- PascalCase for all C# methods: `AnalyzeLap`, `StreamAndImportCsvAsync`, `GetSessionTelemetry`, `ValidateOrDefault`
- Asynchronous methods suffixed with `Async`: `StreamAndImportCsvAsync`, `GetSessionTelemetry`
- JavaScript functions: camelCase (`animateLaserScan`, `formatSeconds`, `updateTelemetryReadout`)

**Variables & Fields:**
- Private class fields: `_camelCase` with leading underscore (e.g., `_context`, `_dataGenerator`, `_analysisService`)
- Method parameters and local variables: `camelCase` (e.g., `vehicleId`, `lapNumber`, `telemetryData`, `trackLengthMeters`)
- Constants and Enum values: PascalCase (e.g., `VehicleType.Car`, `MotorsportSessionType.SprintQualifying`)

**Types & Models:**
- PascalCase for classes, structs, records, and enums: `Vehicle`, `TelemetryPoint`, `AnalysisResult`, `ImportProgress`
- Interfaces prefixed with `I`: `ITelemetryAnalysisService`

## Code Style

**Formatting & Language Level:**
- C# 12 / .NET 8.0 language features
- File-scoped namespace declarations: `namespace RacingTelemetryAnalyzer.Services;`
- Implicit usings enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Standard 4-space indentation for C#, 4 spaces for HTML/Razor, 2 or 4 for JavaScript

**Import / Using Organization:**
- Order:
  1. System namespaces (`System`, `System.Collections.Generic`, `System.Linq`, `System.Threading.Tasks`)
  2. Microsoft framework namespaces (`Microsoft.AspNetCore.Mvc`, `Microsoft.EntityFrameworkCore`)
  3. Third-party packages (`CsvHelper`)
  4. Project namespaces (`RacingTelemetryAnalyzer.Data`, `RacingTelemetryAnalyzer.Models`, `RacingTelemetryAnalyzer.Services`)

## Architectural & Design Patterns

**Dependency Injection:**
- Constructor injection pattern for all ASP.NET Core controllers and domain services
- Scoped lifetime registration in `Program.cs`: `builder.Services.AddScoped<ITelemetryAnalysisService, TelemetryAnalysisService>()`

**Entity Framework Querying:**
- Use `.AsNoTracking()` for read-only query paths (e.g., `TelemetryApiController`) to minimize memory allocation
- Explicit eager loading using `.Include()` (e.g., `.Include(s => s.Laps).Include(s => s.Vehicle)`)
- For bulk data insertion (60k+ telemetry points): batch insertion with explicit `SaveChangesAsync()` followed by `ChangeTracker.Clear()` to avoid tracking bloat

**Error Handling:**
- Guard clauses with early exits: check for null entities before processing and return appropriate HTTP status (`NotFound()`, `RedirectToAction()`)
- Asynchronous try-catch in streaming operations (`CsvImportService`) updating progress error state

**Logging:**
- ASP.NET Core `ILogger<T>` and diagnostic console logs for telemetry stream timing and benchmark points

---

*Convention analysis: 2026-09-20*
