
using Persistence.Influx.Interface;
using Persistence.Models.Interface;

using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InfluxDB.Client.Core.Exceptions;

namespace Persistence.Influx;

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
    public async Task<int> WriteAsync(IReadOnlyCollection<IInfluxPoint> readings, CancellationToken cancellationToken)
    {
        if (readings.Count == 0)
        {
            return 0;
        }

        List<PointData> points = new List<PointData>(readings.Count);
        foreach (IInfluxPoint reading in readings)
        {
            PointData point = PointData
                .Measurement(reading.Measurement);
            foreach (KeyValuePair<string, object> field in reading.Fields)
            {
                point = point.Field(field.Key, field.Value);
            }

            foreach (KeyValuePair<string, string> tag in reading.Tags)
            {
                point = point.Tag(tag.Key, tag.Value);
            }
            point = point.Timestamp(reading.TimestampUtc.ToUniversalTime(), WritePrecision.Ns);

            points.Add(point);
        }


        try
        {
            IWriteApiAsync writeApi = this.influxClientFactory.GetWriteClient().GetWriteApiAsync();
            await writeApi.WritePointsAsync(points, this.options.Bucket, this.options.Org, cancellationToken);
            this.logger.Information("Wrote {Count} readings to InfluxDB bucket {Bucket}", points.Count, this.options.Bucket);
        }
        catch (HttpException)
        {
            this.logger.Error("We got connection error from influxdb");
            return 0;
        }
        catch (Exception ex)
        {
            this.logger.Error(ex,"We got an exception in writing  points to influx");
            return 0;
        }
        return points.Count;
    }
}
