using System.Threading.Tasks;
using System.Threading;
namespace API.Interface;

/// <summary>
/// Service abstraction for ingesting weather data.
/// </summary>
public interface IWeatherIngestService
{
    /// <summary>
    /// Fetches external data and persists it to storage.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Number of readings written.</returns>
    Task<int> FetchAndStoreAsync(CancellationToken cancellationToken);
}
