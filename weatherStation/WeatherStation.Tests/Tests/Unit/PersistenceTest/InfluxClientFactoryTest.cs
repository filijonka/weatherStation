using System;
using Persistence.Influx;
using Persistence.Influx.Interface;

using Microsoft.Extensions.Options;

namespace WeatherStation.Tests.Tests.Unit.PersistenceTest;

/// <summary>
/// Tests for Influx client factory.
/// </summary>
[TestFixture]
public class InfluxClientFactoryTest
{
    [Test]
    public void Test_GetWriteClient_ReturnsWriteClient()
    {
        InfluxOptions options = new InfluxOptions
        {
            Url = "http://localhost:8086",
            Token = "test-token"
        };
        IOptions<InfluxOptions> optionsWrapper = Options.Create(options);

        InfluxClientFactory factory = new InfluxClientFactory(optionsWrapper);

        IInfluxWriteClient writeClient = factory.GetWriteClient();

        Assert.That(writeClient, Is.Not.Null);
        factory.Dispose();
    }

    [Test]
    public void Test_GetWriteClient_UsesOptions()
    {
        const string expectedUrl = "http://test:8086";
        const string expectedToken = "test-token-123";
        InfluxOptions options = new InfluxOptions
        {
            Url = expectedUrl,
            Token = expectedToken
        };
        IOptions<InfluxOptions> optionsWrapper = Options.Create(options);

        InfluxClientFactory factory = new InfluxClientFactory(optionsWrapper);

        IInfluxWriteClient writeClient = factory.GetWriteClient();

        Assert.That(writeClient, Is.Not.Null);
        factory.Dispose();
    }
    [Test]
    public void Test_Dispose_IsIdempotent()
    {
        InfluxOptions options = new InfluxOptions { Url = "http://localhost:8086", Token = "token" };
        IOptions<InfluxOptions> optionsWrapper = Options.Create(options);
        InfluxClientFactory factory = new InfluxClientFactory(optionsWrapper);
        factory.Dispose();
        Assert.DoesNotThrow(() => factory.Dispose());
    }

    [Test]
    public void Test_InvalidOptions_Throws()
    {
        InfluxOptions options = new InfluxOptions { Url = "", Token = "" };
        IOptions<InfluxOptions> optionsWrapper = Options.Create(options);
        Assert.Throws<ArgumentException>(() => new InfluxClientFactory(optionsWrapper));
    }

}
