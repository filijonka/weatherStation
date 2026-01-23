using Persistance.Models;

using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using InfluxDB.Client;
using Microsoft.Extensions.Options;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
namespace Persistance.Influx;

/// <inheritdoc />
public sealed class InfluxWriteService : IInfluxWriteService
{
    private readonly IInfluxClientFactory influxClientFactory;
    private readonly InfluxOptions options;
    private readonly ILogger logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InfluxWriteService"/> class.
    /// </summary>
    /// <param name="influxClientFactory">Influx client factory.</param>
    /// <param name="options">Influx options.</param>
    /// <param name="logger">Serilog logger.</param>
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

    /// <inheritdoc />
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

        IWriteApiAsync writeApi = this.influxClientFactory.GetWriteClient().GetWriteApiAsync();
        await writeApi.WritePointsAsync(points, this.options.Bucket, this.options.Org, cancellationToken);

        return points.Count;
    }
}
