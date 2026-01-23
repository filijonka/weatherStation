using System;
using API.Interface;
using Persistance.Influx;

using Serilog;
using System.Threading.Tasks;
using System.Threading;

namespace API.Services;

/// <inheritdoc />
public sealed class WeatherIngestService : IWeatherIngestService
{
    private readonly IInfluxWriteService influxWriteService;
    private readonly ILogger logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherIngestService"/> class.
    /// </summary>
    /// <param name="influxWriteService">Influx write service.</param>
    /// <param name="logger">Serilog logger.</param>
    public WeatherIngestService(
        IInfluxWriteService influxWriteService,
        ILogger logger
    )
    {
        this.influxWriteService = influxWriteService;
        this.logger = logger.ForContext<WeatherIngestService>();
    }

    /// <inheritdoc />
    public Task<int> FetchAndStoreAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
