using API.Interface;
using WeatherStation.Persistence.Influx;
using WeatherStation.Persistence.Models;
using ILogger = Serilog.ILogger;

namespace API.Services;

public sealed class WeatherIngestService : IWeatherIngestService
{
    private readonly IExternalWeatherClient externalWeatherClient;
    private readonly IInfluxWriteService influxWriteService;
    private readonly ILogger logger;

    public WeatherIngestService(
        IExternalWeatherClient externalWeatherClient,
        IInfluxWriteService influxWriteService,
        ILogger logger
    )
    {
        this.externalWeatherClient = externalWeatherClient;
        this.influxWriteService = influxWriteService;
        this.logger = logger.ForContext<WeatherIngestService>();
    }

    public async Task<int> FetchAndStoreAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<WeatherReading> readings = await this.externalWeatherClient.GetReadingsAsync(cancellationToken);
        int writtenCount = await this.influxWriteService.WriteAsync(readings, cancellationToken);

        this.logger.Information("Ingested {Count} readings", writtenCount);
        return writtenCount;
    }
}
