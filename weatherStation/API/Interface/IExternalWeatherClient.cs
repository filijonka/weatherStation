using Persistance.Models;

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
namespace API.Interface;

/// <summary>
/// Client abstraction for fetching external weather readings.
/// </summary>
public interface IExternalWeatherClient
{
    /// <summary>
    /// Fetches readings from the external weather source.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>Collection of normalized weather readings.</returns>
    Task<IReadOnlyCollection<WeatherReading>> GetReadingsAsync(CancellationToken cancellationToken);
}
