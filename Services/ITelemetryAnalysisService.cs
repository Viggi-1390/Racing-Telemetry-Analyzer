using System.Collections.Generic;
using RacingTelemetryAnalyzer.Models;

namespace RacingTelemetryAnalyzer.Services;

public interface ITelemetryAnalysisService
{
    IEnumerable<AnalysisResult> AnalyzeLap(Lap lap, IEnumerable<TelemetryPoint> telemetryData);
}
