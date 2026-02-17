using InfluxDB.Client;

namespace Persistence.Influx;

/// <summary>
/// Interface for InfluxDB write client operations.
/// </summary>
public interface IInfluxWriteClient
{
    /// <summary>
    /// Gets the write API async instance.
    /// </summary>
    /// <returns>Write API async instance.</returns>
    IWriteApiAsync GetWriteApiAsync();
}
