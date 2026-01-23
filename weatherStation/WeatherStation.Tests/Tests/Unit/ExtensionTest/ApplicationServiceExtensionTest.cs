using API.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using WeatherStation.Tests.Tests;
using Moq;
using It = Moq.It;

namespace WeatherStation.Tests.Tests.Unit.ExtensionTest;

/// <summary>
/// Tests for application service extension.
/// </summary>
[TestFixture]
public class ApplicationServiceExtensionTest : TestBase
{
    /// <summary>
    /// Add application services should register all services.
    /// </summary>
    [Test]
    public void Test_AddApplicationServices_RegistersAllServices()
    {
        ServiceCollection services = new ServiceCollection();
        Mock<IConfigurationSection> influxSection = new Mock<IConfigurationSection>();
        Mock<IConfigurationSection> weatherApiSection = new Mock<IConfigurationSection>();
        Mock<IConfigurationSection> netatmoSection = new Mock<IConfigurationSection>();
    
        Mock<IConfiguration> configurationMock = new ServiceTestMockBuilder<IConfiguration>.Builder()
            .Setup(c => c.GetSection("Influx"), influxSection.Object)
            .Setup(c => c.GetSection("WeatherApi"), weatherApiSection.Object)
            .Setup(c => c.GetSection("Netatmo"), netatmoSection.Object)
            .Build();

        services.AddApplicationServices(configurationMock.Object);

        Assert.Multiple(() =>
        {
            Assert.That(services.Any(s => s.ServiceType == typeof(Persistance.Influx.IInfluxClientFactory)), Is.True);
            Assert.That(services.Any(s => s.ServiceType == typeof(Persistance.Influx.IInfluxWriteService)), Is.True);
            Assert.That(services.Any(s => s.ServiceType == typeof(API.Interface.IWeatherIngestService)), Is.True);
            Assert.That(services.Any(s => s.ServiceType == typeof(API.Auth.Netatmo.Interface.INetatmoTokenStore)), Is.True);
            Assert.That(services.Any(s => s.ServiceType == typeof(API.Auth.Netatmo.Interface.INetatmoOAuthClient)), Is.True);
        });
    }

    /// <summary>
    /// Add application services should configure options.
    /// </summary>
    [Test]
    public void Test_AddApplicationServices_ConfiguresOptions()
    {
        List<KeyValuePair<string, string>> settings = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("Influx:Url", "http://localhost:8086"),
            new KeyValuePair<string, string>("WeatherApi:BaseUrl", "https://api.example.com"),
            new KeyValuePair<string, string>("Netatmo:ClientId", "test-client")
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        ServiceCollection services = new ServiceCollection();
        services.AddApplicationServices(configuration);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        Microsoft.Extensions.Options.IOptions<Persistance.Influx.InfluxOptions> influxOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<Persistance.Influx.InfluxOptions>>();
        Assert.That(influxOptions, Is.Not.Null);

        Microsoft.Extensions.Options.IOptions<API.Options.WeatherApiOptions> weatherApiOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<API.Options.WeatherApiOptions>>();
        Assert.That(weatherApiOptions, Is.Not.Null);

        Microsoft.Extensions.Options.IOptions<API.Auth.Netatmo.Options.NetatmoOptions> netatmoOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<API.Auth.Netatmo.Options.NetatmoOptions>>();
        Assert.That(netatmoOptions, Is.Not.Null);
    }

    /// <summary>
    /// Create logger should return logger.
    /// </summary>
    [Test]
    public void Test_CreateLogger_ReturnsLogger()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .Build();

        Serilog.ILogger result = ApplicationServiceExtension.CreateLogger(configuration);

        Assert.That(result, Is.Not.Null);
    }

    /// <summary>
    /// Create logger should read from configuration.
    /// </summary>
    [Test]
    public void Test_CreateLogger_ReadsFromConfiguration()
    {
        List<KeyValuePair<string, string>> settings = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("Serilog:MinimumLevel:Default", "Information")
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        Serilog.ILogger result = ApplicationServiceExtension.CreateLogger(configuration);

        Assert.That(result, Is.Not.Null);
    }
}
