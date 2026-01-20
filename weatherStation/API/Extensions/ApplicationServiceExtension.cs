using API.Interface;
using API.Options;
using API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using WeatherStation.Persistence.Influx;

namespace API.Extensions;

/// <summary>
/// Helper static class for adding services.
/// </summary>
public static class ApplicationServiceExtension
{
    /// <summary>
    /// Adds application services for this API.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns>IServiceCollection</returns>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddOpenApi();
        services.Configure<InfluxOptions>(configuration.GetSection("Influx"));
        services.Configure<WeatherApiOptions>(configuration.GetSection("WeatherApi"));
        services.AddSingleton<Serilog.ILogger>(_ => Log.Logger);
        services.AddSingleton<IInfluxClientFactory, InfluxClientFactory>();
        services.AddSingleton<IInfluxWriteService, InfluxWriteService>();
        services.AddHttpClient<IExternalWeatherClient, ExternalWeatherClient>();
        services.AddSingleton<IWeatherIngestService, WeatherIngestService>();

        return services;
    }
}
