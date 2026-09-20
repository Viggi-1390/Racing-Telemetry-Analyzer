# External Integrations

**Analysis Date:** 2026-09-20

## APIs & External Services

**Telemetry CSV Streaming Ingestion:**
- Ingests high-frequency automotive telemetry CSV data (Motec, Assetto Corsa Competizione, rFactor 2, or custom hardware logger)
- Protocol: HTTP multipart/form-data upload streaming line-by-line via `CsvImportService`
- Processing Strategy: 1,000-row batch commits, periodic ChangeTracker clearing, non-blocking asynchronous progress reporting
- Endpoint: `/Telemetry/ImportCsv`

**PDF Export Engine:**
- QuestPDF fluent layout engine (Community license)
- Used for generating standalone A4 PDF telemetry debrief sheets and driver recommendations
- Service: `Services/ReportService.cs`

## Data Storage

**Databases:**
- Microsoft SQL Server (`(localdb)\mssqllocaldb`)
  - Connection: `ConnectionStrings:DefaultConnection` in `appsettings.json`
  - Client / ORM: Entity Framework Core 8.0 (`Data/ApplicationDbContext.cs`)
  - Database Name: `RacingTelemetryDb`
  - Managed Tables: `Vehicles`, `Tracks`, `TrackSections`, `Sessions`, `SessionConditions`, `VehicleSetups`, `Laps`, `TelemetryPoints`, `AnalysisResults`, `SessionNotes`

**File Storage:**
- Local filesystem storage (`wwwroot/images/cars/`, `wwwroot/images/tracks/`, `wwwroot/images/loading/`)

**Caching & State:**
- In-memory cache via thread-safe `ConcurrentDictionary<string, ImportProgress>` in `CsvImportService` for real-time streaming progress queries

## Authentication & Identity

**Auth Provider:**
- None / Anonymous access (Local motorsport engineering workstation / desktop tool)

## Monitoring & Observability

**Error Tracking:**
- ASP.NET Core built-in ILogger / Console logging (`appsettings.json` log level configuration)
- User-facing error handler: `/Home/Error` (`Models/ErrorViewModel.cs`)

---

*Integrations analysis: 2026-09-20*
