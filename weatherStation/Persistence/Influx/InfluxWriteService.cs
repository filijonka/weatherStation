using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Options;
using Serilog;
using WeatherStation.Persistence.Models;

namespace WeatherStation.Persistence.Influx;

public sealed class InfluxWriteService : IInfluxWriteService
{
    private readonly IInfluxClientFactory influxClientFactory;
    private readonly InfluxOptions options;
    private readonly ILogger logger;

    public InfluxWriteService(
        IInfluxClientFactory influxClientFactory,
        IOptions<InfluxOptions> options,
        ILogger logger
    )
    {
        this.influxClientFactory = influxClientFactory;
        this.options = options.Value;
        this.logger = logger.ForContext<InfluxWriteService>();
    }

    public async Task<int> WriteAsync(IReadOnlyCollection<WeatherReading> readings, CancellationToken cancellationToken)
    {
        if (readings.Count == 0)
        {
            return 0;
        }

        List<PointData> points = new List<PointData>(readings.Count);
        foreach (WeatherReading reading in readings)
        {
            PointData point = PointData
                .Measurement("weather_reading")
                .Tag("stationId", reading.StationId)
                .Tag("sensor", reading.Sensor)
                .Tag("unit", reading.Unit)
                .Field("value", reading.Value)
                .Timestamp(reading.Timestamp.ToUniversalTime(), WritePrecision.Ns);

            points.Add(point);
        }

        this.logger.Information("Writing {Count} readings to InfluxDB bucket {Bucket}", points.Count, this.options.Bucket);

        IWriteApiAsync writeApi = this.influxClientFactory.GetClient().GetWriteApiAsync();
        await writeApi.WritePointsAsync(points, this.options.Bucket, this.options.Org, cancellationToken);

        return points.Count;
    }
}
