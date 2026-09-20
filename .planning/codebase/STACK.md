# Technology Stack

**Analysis Date:** 2026-09-20

## Languages

**Primary:**
- C# (.NET 8.0) - Backend MVC controllers, services, EF Core data access, QuestPDF reporting
- JavaScript (ES6+) - Frontend telemetry visualization, replay canvas loop, Chart.js graphs, streaming upload monitor

**Secondary:**
- Razor (HTML5 / CSS3) - Server-rendered Razor views and Blazor component

## Runtime

**Environment:**
- .NET 8.0 SDK / Runtime (ASP.NET Core Web Application)

**Package Manager:**
- NuGet (PackageReference in `RacingTelemetryAnalyzer.csproj`)
- Lockfile: Not committed (standard SDK project resolution)

## Frameworks

**Core:**
- ASP.NET Core 8.0 MVC - Web API, routing, server-rendered views
- Entity Framework Core 8.0.26 - Object-relational mapping (SqlServer provider)

**Testing:**
- xUnit / Moq in sibling test project `RacingTelemetryAnalyzer.Tests`

**Build/Dev:**
- `Microsoft.NET.Sdk.Web` - Build and packaging toolchain
- `Microsoft.EntityFrameworkCore.Tools` (10.0.11) - Design-time EF migrations tool

## Key Dependencies

**Critical:**
- `CsvHelper` (33.1.0) - High-throughput CSV parsing and fallback import
- `QuestPDF` (2026.8.0) - High-performance PDF generation for telemetry lap reports
- `Microsoft.EntityFrameworkCore.SqlServer` (8.0.26) - Database provider for Microsoft SQL Server

**Infrastructure:**
- `Microsoft.AspNetCore.Http.Features` - Multi-part file upload streaming and Kestrel 250MB buffer limits
- Chart.js (v4 CDN / local vendor) - Interactive telemetry channel charts

## Configuration

**Environment:**
- Configured via `appsettings.json` and `appsettings.Development.json`
- Key configs required: `ConnectionStrings:DefaultConnection` (SQL Server connection string)
- Kestrel & Form limits: 250MB body length configured in `Program.cs`

**Build:**
- Project file: `RacingTelemetryAnalyzer.csproj`
- Solution file: `RacingTelemetryAnalyzer.slnx`

## Platform Requirements

**Development:**
- Windows with .NET 8.0 SDK, LocalDB / SQL Server, PowerShell

**Production:**
- ASP.NET Core 8.0 Web Server (Kestrel / IIS / Docker container) with SQL Server database

---

*Stack analysis: 2026-09-20*
