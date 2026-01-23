using InfluxDB.Client;
using Microsoft.Extensions.Options;
using System;
namespace Persistance.Influx;

/// <inheritdoc />
public sealed class InfluxClientFactory : IInfluxClientFactory, IDisposable
{
    private readonly InfluxOptions options;
    private readonly InfluxDBClient influxDbClient;
    private bool isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="InfluxClientFactory"/> class.
    /// </summary>
    /// <param name="options">InfluxDB options.</param>
    public InfluxClientFactory(IOptions<InfluxOptions> options)
    {
        this.options = options.Value;
        this.influxDbClient = new InfluxDBClient(this.options.Url, this.options.Token);
    }

    /// <inheritdoc />
    public IInfluxWriteClient GetWriteClient()
    {
        return new InfluxWriteClient(this.influxDbClient);
    }

    /// <summary>
    /// Disposes the underlying InfluxDB client.
    /// </summary>
    public void Dispose()
    {
        if (this.isDisposed)
        {
            return;
        }

        this.influxDbClient.Dispose();
        this.isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
