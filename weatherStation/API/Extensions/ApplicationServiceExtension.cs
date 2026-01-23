using API.Auth.Netatmo.Interface;
using API.Auth.Netatmo.Options;
using API.Auth.Netatmo.Services;
using API.Interface;
using API.Options;
using API.Services;
using Persistance.Influx;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace API.Extensions;

/// <summary>
/// Helper static class for adding services.
/// </summary>
public static class ApplicationServiceExtension
{
    /// <summary>
    /// Adds application services for this API.
    /// </summary>
    /// <param name="services">Service collection to add registrations to.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<InfluxOptions>(configuration.GetSection("Influx"));
        services.Configure<WeatherApiOptions>(configuration.GetSection("WeatherApi"));
        services.Configure<NetatmoOptions>(configuration.GetSection("Netatmo"));
        services.AddSingleton<IInfluxClientFactory, InfluxClientFactory>();
        services.AddSingleton<IInfluxWriteService, InfluxWriteService>();
        services.AddSingleton<IWeatherIngestService, WeatherIngestService>();
        services.AddSingleton<INetatmoTokenStore, NetatmoTokenStore>();
        services.AddHttpClient<INetatmoOAuthClient, NetatmoOAuthClient>();

        return services;
    }

    /// <summary>
    /// Creates the Serilog logger instance.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>Configured Serilog logger instance.</returns>
    public static ILogger CreateLogger(IConfiguration configuration)
    {
        LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext();

        ILogger logger = loggerConfiguration.CreateLogger();

        return logger;
    }
}
