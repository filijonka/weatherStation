using API.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Generic;
using System.Linq;
using API.Interface.Services;
using Persistence.Influx;
using Persistence.Influx.Interface;

namespace WeatherStation.Tests.Tests.Unit.ExtensionTest;

/// <summary>
/// Tests for application service extension.
/// </summary>
[TestFixture]
public class ApplicationServiceExtensionTest : TestBase
{
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
            Assert.That(services.Any(s => s.ServiceType == typeof(IInfluxClientFactory)), Is.True);
            Assert.That(services.Any(s => s.ServiceType == typeof(IInfluxWriteService)), Is.True);
            Assert.That(services.Any(s => s.ServiceType == typeof(INetatmoService)), Is.True);
            Assert.That(services.Any(s => s.ServiceType == typeof(API.Auth.Netatmo.Interface.INetatmoTokenStore)), Is.True);
            Assert.That(services.Any(s => s.ServiceType == typeof(API.Auth.Netatmo.Interface.INetatmoOAuthClient)), Is.True);
        });
    }

    [Test]
    public void Test_AddApplicationServices_ConfiguresOptions()
    {
        List<KeyValuePair<string, string>> settings = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("Influx:Url", "http://localhost:8086"),
            new KeyValuePair<string, string>("Influx:Token", "token"),
            new KeyValuePair<string, string>("Influx:Org", "org"),
            new KeyValuePair<string, string>("Influx:Bucket", "bucket"),
            new KeyValuePair<string, string>("WeatherApi:BaseUrl", "https://api.example.com"),
            new KeyValuePair<string, string>("WeatherApi:ApiKey", "key"),
            new KeyValuePair<string, string>("Netatmo:ClientId", "test-client"),
            new KeyValuePair<string, string>("Netatmo:ClientSecret", "secret"),
            new KeyValuePair<string, string>("Netatmo:RedirectUri", "http://localhost/callback"),
            new KeyValuePair<string, string>("Netatmo:Scopes", "read_station"),
            new KeyValuePair<string, string>("Netatmo:AuthorizeUrl", "https://api.netatmo.com/oauth2/authorize"),
            new KeyValuePair<string, string>("Netatmo:TokenUrl", "https://api.netatmo.com/oauth2/token"),
            new KeyValuePair<string, string>("Netatmo:TokenFilePath", "tokens.json")
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        ServiceCollection services = new ServiceCollection();
        services.AddApplicationServices(configuration);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IOptions<InfluxOptions> influxOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<InfluxOptions>>();
        Assert.That(influxOptions, Is.Not.Null);
        Assert.DoesNotThrow(() => _ = influxOptions!.Value);

        IOptions<API.Options.WeatherApiOptions> weatherApiOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<API.Options.WeatherApiOptions>>();
        Assert.That(weatherApiOptions, Is.Not.Null);
        Assert.DoesNotThrow(() => _ = weatherApiOptions!.Value);

        IOptions<API.Auth.Netatmo.Options.NetatmoOptions> netatmoOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<API.Auth.Netatmo.Options.NetatmoOptions>>();
        Assert.That(netatmoOptions, Is.Not.Null);
        Assert.DoesNotThrow(() => _ = netatmoOptions!.Value);
    }

    [Test]
    public void Test_AddApplicationServices_WhenNetatmoOptionsInvalid_ThrowsWhenResolved()
    {
        List<KeyValuePair<string, string>> settings = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("Netatmo:ClientId", "id")
            // Missing ClientSecret, RedirectUri, Scopes, AuthorizeUrl, TokenUrl, TokenFilePath
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        ServiceCollection services = new ServiceCollection();
        services.AddApplicationServices(configuration);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        IOptions<API.Auth.Netatmo.Options.NetatmoOptions> options = serviceProvider.GetService<IOptions<API.Auth.Netatmo.Options.NetatmoOptions>>();
        Assert.That(options, Is.Not.Null);
        Assert.Throws<OptionsValidationException>(() => _ = options!.Value);
    }

    [Test]
    public void Test_CreateLogger_ReturnsLogger()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .Build();

        Serilog.ILogger result = ApplicationServiceExtension.CreateLogger(configuration);

        Assert.That(result, Is.Not.Null);
    }

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
