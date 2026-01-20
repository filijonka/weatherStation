namespace API.Services;

public sealed class ExternalWeatherReading
{
    public string? StationId { get; set; }

    public string? Sensor { get; set; }

    public double Value { get; set; }

    public string? Unit { get; set; }

    public DateTime Timestamp { get; set; }
}
