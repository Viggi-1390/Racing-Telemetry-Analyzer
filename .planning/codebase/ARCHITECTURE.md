# Architecture

**Analysis Date:** 2026-09-20

## Architectural Pattern

**Pattern:** Layered ASP.NET Core MVC with Repository/DbContext Data Access and Dedicated Domain Services

- **Presentation Layer:** ASP.NET Core MVC Controllers (`Controllers/`) returning Razor Views (`Views/`) and high-throughput JSON API endpoints (`Controllers/TelemetryApiController.cs`).
- **Domain & Analytics Layer:** Scoped services (`Services/`) managing telemetry physics calculations, heuristic recommendations, streaming CSV ingestion, and PDF generation.
- **Data Access Layer:** Entity Framework Core (`Data/ApplicationDbContext.cs`) utilizing code-first migrations and domain models (`Models/DomainModels.cs`).

## Component Boundaries

**Controllers (`Controllers/`):**
- `HomeController.cs`: Vehicle garage management, showcase priority (Cadillac V-Series.R / Audi R8), vehicle specification modal editing, creation, and deletion.
- `TrackController.cs`: Circuit catalog, right-click context menu management (Edit Track, Delete Track with safe cascade verification).
- `TelemetryController.cs`: Main telemetry workstation interface, session type selection, multi-lap continuous replay timeline, and CSV import dispatch.
- `TelemetryApiController.cs`: Fast JSON endpoints (`/api/telemetry/session/{id}`, `/api/telemetry/latest`) providing full multi-lap datasets for the client-side canvas engine.

**Services (`Services/`):**
- `TelemetryAnalysisService.cs` (`ITelemetryAnalysisService`): Deterministic driving analysis (peak braking pressure, apex minimum speed, throttle response time, motorcycle lean angle thresholding).
- `CsvImportService.cs`: High-performance streaming parser processing large datasets (80MB+, 63,000+ points) line-by-line with 1,000-point batching to prevent memory overflow and timeouts.
- `DataGeneratorService.cs`: Generates synthetic telemetry points for demonstration sessions across multiple vehicle categories (GT3, LMDh, MotoGP).
- `ReportService.cs`: Compiles lap metrics and recommendations into QuestPDF documents.

**Data & Models (`Data/`, `Models/`):**
- `DomainModels.cs`: Unified motorsport entity hierarchy (`Vehicle` -> `Session` -> `Lap` -> `TelemetryPoint`), supporting car telemetry (steering, 4-wheel suspension travel, 3-axis G-forces) and motorcycle telemetry (lean angle, dual brakes, fork/shock stroke).
- `ApplicationDbContext.cs`: EF Core mappings, foreign keys, 1-to-1 conditions/setup configurations, and TimeSpan tick conversions.

## Data Flow

```text
CSV Log File / Hardware Telemetry Stream
                 │
                 ▼
          CsvImportService
   (Streaming line-by-line parser)
                 │
       (1,000-row batch commits)
                 ▼
       ApplicationDbContext ──► SQL Server (RacingTelemetryDb)
                 │
                 ▼
   TelemetryController / TelemetryApiController
                 │
                 ▼
      TelemetryAnalysisService ──► Deterministic Driving Feedback
                 │
                 ▼
    Razor View (Index.cshtml) + HTML5 60FPS Canvas Scrubber + Chart.js
```

## Entry Points

**Application Bootstrap:**
- `Program.cs`: ASP.NET Core host initialization, DI service registration, Kestrel request limits (250MB), CORS policy, static file middleware, and MVC routing.

**Default Route:**
- `{controller=Home}/{action=Index}`: Garage and showcase view.

**Telemetry Workstation:**
- `{controller=Telemetry}/{action=Index}`: Analysis workspace and replay viewer.
- `{controller=Telemetry}/{action=Loading}`: Staged motorsport initialization sequence with laser scan silhouette reconstruction.

## Architectural Constraints

- **Threading & Concurrency:** Async/await throughout controller endpoints; concurrent progress polling handled via `ConcurrentDictionary`.
- **Large Dataset Memory Management:** Telemetry imports flush every 1,000 rows and clear the EF Core ChangeTracker to maintain constant memory consumption during 60k+ record imports.
- **Client Performance:** Telemetry timeline replay runs via requestAnimationFrame at 60 FPS directly on an HTML5 canvas to bypass DOM layout thrashing.

---

*Architecture analysis: 2026-09-20*
