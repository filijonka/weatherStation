using System;

namespace Persistence.Influx.Interface;

/// <summary>
/// Factory for creating or retrieving InfluxDB clients.
/// </summary>
public interface IInfluxClientFactory : IDisposable
{
    /// <summary>
    /// Gets an InfluxDB write client instance.
    /// </summary>
    /// <returns>InfluxDB write client.</returns>
    IInfluxWriteClient GetWriteClient();
}
