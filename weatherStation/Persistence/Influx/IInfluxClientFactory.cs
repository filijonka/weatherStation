using Persistance.Influx;

namespace Persistence.Influx;

/// <summary>
/// Factory for creating or retrieving InfluxDB clients.
/// </summary>
public interface IInfluxClientFactory
{
    /// <summary>
    /// Gets an InfluxDB write client instance.
    /// </summary>
    /// <returns>InfluxDB write client.</returns>
    IInfluxWriteClient GetWriteClient();
}
