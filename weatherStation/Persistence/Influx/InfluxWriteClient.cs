using InfluxDB.Client;

namespace Persistence.Influx;

/// <summary>
/// Wrapper for InfluxDB client that provides write operations.
/// </summary>
public sealed class InfluxWriteClient : IInfluxWriteClient
{
    private readonly InfluxDBClient client;

    /// <summary>
    /// Initializes a new instance of the <see cref="InfluxWriteClient"/> class.
    /// </summary>
    /// <param name="client">InfluxDB client instance.</param>
    public InfluxWriteClient(InfluxDBClient client)
    {
        this.client = client;
    }

    /// <inheritdoc />
    public IWriteApiAsync GetWriteApiAsync()
    {
        return this.client.GetWriteApiAsync();
    }
}
