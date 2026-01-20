using InfluxDB.Client;
using Microsoft.Extensions.Options;

namespace WeatherStation.Persistence.Influx;

public sealed class InfluxClientFactory : IInfluxClientFactory, IDisposable
{
    private readonly InfluxOptions options;
    private readonly InfluxDBClient influxDbClient;
    private bool isDisposed;

    public InfluxClientFactory(IOptions<InfluxOptions> options)
    {
        this.options = options.Value;
        this.influxDbClient = InfluxDBClientFactory.Create(this.options.Url, this.options.Token);
    }

    public InfluxDBClient GetClient()
    {
        return this.influxDbClient;
    }

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
