using System;
using System.Collections.Generic;
using System.Linq;
using RacingTelemetryAnalyzer.Models;
using RacingTelemetryAnalyzer.Services;
using Xunit;

namespace RacingTelemetryAnalyzer.Tests;

public class TelemetryAnalysisServiceTests
{
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

    [Fact]
    public void AnalyzeLap_WithAggressiveThrottle_ReturnsThrottleRecommendation()
    {
        // Arrange
        var service = new TelemetryAnalysisService();
        var lap = new Lap { LapId = 1 };
        var telemetry = new List<TelemetryPoint>();
        for (int i = 0; i < 15; i++)
        {
            telemetry.Add(new TelemetryPoint { Timestamp = TimeSpan.FromSeconds(i), Speed = 50, Throttle = 95 });
        }

        // Act
        var results = service.AnalyzeLap(lap, telemetry);

        // Assert
        Assert.Contains(results, r => r.RecommendationType == "Throttle");
    }
}
