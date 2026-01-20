using API.Services;
using WeatherStation.Persistence.Models;

namespace Tests;

[TestFixture]
public class ExternalWeatherReadingMapperTests
{
    [Test]
    public void Test_TryMap_ValidPayload_ReturnsTrue()
    {
        ExternalWeatherReading payload = new ExternalWeatherReading
        {
            StationId = "station-1",
            Sensor = "temperature",
            Unit = "celsius",
            Value = 12.5,
            Timestamp = new DateTime(2026, 1, 20, 10, 0, 0, DateTimeKind.Utc)
        };

        bool result = ExternalWeatherReadingMapper.TryMap(payload, out WeatherReading reading);

        Assert.That(result, Is.True);
        Assert.That(reading.StationId, Is.EqualTo("station-1"));
        Assert.That(reading.Sensor, Is.EqualTo("temperature"));
        Assert.That(reading.Unit, Is.EqualTo("celsius"));
        Assert.That(reading.Value, Is.EqualTo(12.5));
        Assert.That(reading.Timestamp, Is.EqualTo(new DateTime(2026, 1, 20, 10, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void Test_TryMap_MissingStation_ReturnsFalse()
    {
        ExternalWeatherReading payload = new ExternalWeatherReading
        {
            StationId = "",
            Sensor = "temperature",
            Unit = "celsius",
            Value = 12.5,
            Timestamp = DateTime.UtcNow
        };

        bool result = ExternalWeatherReadingMapper.TryMap(payload, out WeatherReading reading);

        Assert.That(result, Is.False);
        Assert.That(reading.StationId, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Test_TryMap_DefaultTimestamp_ReturnsFalse()
    {
        ExternalWeatherReading payload = new ExternalWeatherReading
        {
            StationId = "station-1",
            Sensor = "temperature",
            Unit = "celsius",
            Value = 12.5,
            Timestamp = default
        };

        bool result = ExternalWeatherReadingMapper.TryMap(payload, out WeatherReading reading);

        Assert.That(result, Is.False);
        Assert.That(reading.StationId, Is.EqualTo(string.Empty));
    }
}
