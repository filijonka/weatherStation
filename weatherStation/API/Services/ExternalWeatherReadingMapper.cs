using WeatherStation.Persistence.Models;

namespace API.Services;

public static class ExternalWeatherReadingMapper
{
    public static bool TryMap(
        ExternalWeatherReading source,
        out WeatherReading reading
    )
    {
        if (string.IsNullOrWhiteSpace(source.StationId) ||
            string.IsNullOrWhiteSpace(source.Sensor) ||
            string.IsNullOrWhiteSpace(source.Unit) ||
            source.Timestamp == default)
        {
            reading = new WeatherReading(string.Empty, string.Empty, 0, string.Empty, default);
            return false;
        }

        reading = new WeatherReading(
            source.StationId,
            source.Sensor,
            source.Value,
            source.Unit,
            source.Timestamp
        );

        return true;
    }
}
