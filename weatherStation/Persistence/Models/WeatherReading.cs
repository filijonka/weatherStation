namespace WeatherStation.Persistence.Models;

public record WeatherReading(
    string StationId,
    string Sensor,
    double Value,
    string Unit,
    DateTime Timestamp
);
