using Persistance.Influx;

using InfluxDB.Client;
using Microsoft.Extensions.Options;

namespace WeatherStation.Tests.Tests.Unit.PersistenceTest;

/// <summary>
/// Tests for Influx client factory.
/// </summary>
[TestFixture]
public class InfluxClientFactoryTest
{
    /// <summary>
    /// Get write client should return write client.
    /// </summary>
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

    /// <summary>
    /// Get write client should use options.
    /// </summary>
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
}
