# Testing Patterns

**Analysis Date:** 2026-09-20

## Test Framework

**Runner:**
- xUnit 2.5.3 with Microsoft.NET.Test.Sdk (17.8.0) and xunit.runner.visualstudio (2.5.3)
- Coverage collector: `coverlet.collector` (6.0.0)
- Config: `RacingTelemetryAnalyzer.Tests.csproj`

**Assertion Library:**
- xUnit assertions (`Assert.Equal`, `Assert.Contains`, `Assert.NotNull`)

**Run Commands:**
```bash
dotnet test ..\RacingTelemetryAnalyzer.Tests\RacingTelemetryAnalyzer.Tests.csproj          # Run all tests
dotnet test ..\RacingTelemetryAnalyzer.Tests\ --logger "console;verbosity=detailed"     # Verbose output
dotnet test ..\RacingTelemetryAnalyzer.Tests\ --collect:"XPlat Code Coverage"             # Run with code coverage
```

## Test File Organization

**Location:**
- Separate sibling test project: `RacingTelemetryAnalyzer.Tests/`
- Target project reference: `..\RacingTelemetryAnalyzer\RacingTelemetryAnalyzer.csproj`

**Naming:**
- Test files mirror tested classes with `Tests` suffix: `TelemetryAnalysisServiceTests.cs`
- Test methods follow `UnitOfWork_StateUnderTest_ExpectedBehavior`:
  - `AnalyzeLap_WithConservativeBraking_ReturnsBrakingRecommendation`
  - `AnalyzeLap_WithAggressiveThrottle_ReturnsThrottleRecommendation`

## Test Structure

**Suite Organization (AAA Pattern):**
```csharp
[Fact]
public void AnalyzeLap_WithConservativeBraking_ReturnsBrakingRecommendation()
{
    // Arrange
    var service = new TelemetryAnalysisService();
    var lap = new Lap { LapId = 1 };
    var telemetry = new List<TelemetryPoint>
    {
        new TelemetryPoint { Timestamp = TimeSpan.FromSeconds(1), Brake = 50 },
        new TelemetryPoint { Timestamp = TimeSpan.FromSeconds(2), Brake = 60 }
    };

    // Act
    var results = service.AnalyzeLap(lap, telemetry);

    // Assert
    Assert.Contains(results, r => r.RecommendationType == "Braking");
}
```

## Mocking & Isolation

**Patterns:**
- Pure domain services (`TelemetryAnalysisService`) instantiated directly with plain POCO collections.
- Database integration tests utilize EF Core In-Memory or disposable SQL LocalDB test databases.

## Coverage

**Current Status:**
- `TelemetryAnalysisService`: Unit tested across braking and throttle recommendation thresholds.
- Gaps:
  - `CsvImportService`: Needs integration tests with sample CSV files and progress tracking tokens.
  - `DataGeneratorService`: Needs tests verifying generated lap timestamps and telemetry ranges.
  - `TelemetryApiController`: Needs API integration tests for `/api/telemetry/session/{id}` and `/api/telemetry/latest`.

---

*Testing analysis: 2026-09-20*
