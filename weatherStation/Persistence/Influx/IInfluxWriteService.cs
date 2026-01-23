using Persistance.Models;

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
namespace Persistance.Influx;

/// <summary>
/// Writes weather readings to InfluxDB.
/// </summary>
public interface IInfluxWriteService
{
    /// <summary>
    /// Writes the provided readings to InfluxDB.
    /// </summary>
    /// <param name="readings">Readings to write.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Number of readings written.</returns>
    Task<int> WriteAsync(IReadOnlyCollection<WeatherReading> readings, CancellationToken cancellationToken);
}
