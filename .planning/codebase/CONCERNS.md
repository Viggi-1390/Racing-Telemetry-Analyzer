# Codebase Concerns

**Analysis Date:** 2026-09-20

## Tech Debt

**In-Memory CSV Progress Tracking:**
- Issue: Progress state is stored in `ConcurrentDictionary<string, ImportProgress>` inside `CsvImportService.cs`.
- Files: `Services/CsvImportService.cs`
- Impact: If the application scales beyond a single server process or recycles its app pool during an upload, active import progress will be lost.
- Fix approach: Implement `IDistributedCache` (Redis / SQL Server Cache) or SignalR websockets for push-based streaming progress.

**Database Cascading Deletion Safeguards:**
- Issue: EF Core foreign keys link `Track` -> `Session` -> `Lap` -> `TelemetryPoint`. Deleting a Track directly cascades and permanently purges all associated sessions and telemetry.
- Files: `Controllers/TrackController.cs`, `Data/ApplicationDbContext.cs`
- Impact: Accidentally deleting a track could destroy thousands of telemetry data points.
- Fix approach: Implement soft delete (`IsDeleted` flag) or enforce restrict deletion if dependent telemetry sessions exist.

## Performance Considerations

**Large Telemetry Payloads:**
- Problem: Complete telemetry sessions can contain 60,000+ points (~80MB raw data). Loading all points for all laps in a single JSON payload requires significant server memory and client JSON deserialization time.
- Files: `Controllers/TelemetryApiController.cs`, `Views/Telemetry/Index.cshtml`
- Current Mitigation: Single-lap active slices are rendered on the UI, and full-session playback uses asynchronous loading.
- Improvement path: Downsample or decimate points for macro-overview charts, loading high-frequency 60Hz points only for micro-zoom views.

## Fragile Areas

**Telemetry Replay Timeline Synchronization:**
- Files: `Views/Telemetry/Index.cshtml` (Replay Engine script)
- Why fragile: Replay loops must synchronize canvas drawing, lap switching, S1/S2/S3 sector highlights, and tyre heat simulations across varying sample frequencies (10Hz vs 60Hz).
- Safe modification: When editing replay loops, test across both single-lap and multi-lap sessions to prevent timer desync or boundary stuttering.

## Scaling Limits

**LocalDB Dependency:**
- Current capacity: Single local developer workstation (`(localdb)\mssqllocaldb`).
- Limit: LocalDB cannot serve remote connections or run in Linux container environments.
- Scaling path: Configure standard SQL Server / Azure SQL database with credentials via environment variables for cloud deployment.

## Test Coverage Gaps

**Untested Services:**
- What's not tested: `CsvImportService`, `DataGeneratorService`, MVC Controllers.
- Files: `Services/CsvImportService.cs`, `Services/DataGeneratorService.cs`
- Risk: Regressions in CSV streaming or calculation formulas could occur unnoticed.
- Priority: Medium

---

*Concerns audit: 2026-09-20*
