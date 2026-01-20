using WeatherStation.Persistence.Models;

namespace API.Interface;

public interface IExternalWeatherClient
{
    Task<IReadOnlyCollection<WeatherReading>> GetReadingsAsync(CancellationToken cancellationToken);
}
