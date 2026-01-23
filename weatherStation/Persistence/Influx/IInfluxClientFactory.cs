using InfluxDB.Client;
namespace Persistance.Influx;

/// <summary>
/// Factory for creating or retrieving InfluxDB clients.
/// </summary>
public interface IInfluxClientFactory
{
    /// <summary>
    /// Gets an InfluxDB client instance.
    /// </summary>
    /// <returns>InfluxDB client.</returns>
    InfluxDBClient GetClient();
}
