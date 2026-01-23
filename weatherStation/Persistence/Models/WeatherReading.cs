using System;
namespace Persistance.Models;

/// <summary>
/// Normalized weather reading persisted to storage.
/// </summary>
public record WeatherReading(
    string StationId,
    string Sensor,
    double Value,
    string Unit,
    DateTime Timestamp
);
