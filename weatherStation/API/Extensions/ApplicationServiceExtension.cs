using API.Auth.Netatmo.Interface;
using API.Auth.Netatmo.Options;
using API.Auth.Netatmo.Services;
using API.Background.Tasks;
using API.Filters;
using API.Helpers;
using API.Interface.Helpers;
using API.Interface.Logic;
using API.Interface.Services;
using API.Interface.Tasks;
using API.Logic;
using API.Options;
using API.Services;
using BackgroundNetatmoService = API.Background.Services.NetatmoService;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Persistence.Influx;
using Persistence.Influx.Interface;
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
        RegisterValidatedOptions<WeatherApiOptions>(services, configuration.GetSection("WeatherApi"));
        RegisterValidatedOptions<NetatmoOptions>(services, configuration.GetSection("Netatmo"));
        services.AddSingleton<IInfluxClientFactory, InfluxClientFactory>();
        services.AddSingleton<IInfluxWriteService, InfluxWriteService>();
        services.AddSingleton<IMemoryCache, MemoryCache>();
        services.AddSingleton<INetatmoService, NetatmoService>();
        services.AddSingleton<INetatmoTokenStore, NetatmoTokenStore>();
        services.AddSingleton<INetatmoLogicDataProvider, NetatmoLogicDataProvider>();
        services.AddSingleton<INetatmoTask, NetatmoTask>();
        
        services.AddHttpClient<INetatmoOAuthClient, NetatmoOAuthClient>();
        services.AddHttpClient();
        services.AddScoped<NetatmoAuthenticatedFilter>();
        services.AddTransient<ITaskDelayer, TaskDelayer>();
        services.AddHostedService<BackgroundNetatmoService>();
        return services;
    }

    /// <summary>
    /// Registers a validated options type: binds from configuration and adds IValidateOptions that calls Validate().
    /// </summary>
    /// <typeparam name="TOptions">The options type; must inherit ValidatedOptionsBase.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <param name="section">Configuration section to bind from.</param>
    private static void RegisterValidatedOptions<TOptions>(IServiceCollection services, IConfigurationSection section)
        where TOptions : ValidatedOptionsBase
    {
        services.Configure<TOptions>(section);
        services.AddSingleton<IValidateOptions<TOptions>, ValidatedOptionsValidator<TOptions>>();
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
