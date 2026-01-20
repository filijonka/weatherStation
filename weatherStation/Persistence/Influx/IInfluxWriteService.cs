using WeatherStation.Persistence.Models;

namespace WeatherStation.Persistence.Influx;

public interface IInfluxWriteService
{
    Task<int> WriteAsync(IReadOnlyCollection<WeatherReading> readings, CancellationToken cancellationToken);
}
