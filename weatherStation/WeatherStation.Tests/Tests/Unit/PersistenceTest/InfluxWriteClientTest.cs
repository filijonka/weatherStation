using Persistence.Influx;

using InfluxDB.Client;
using Moq;

namespace WeatherStation.Tests.Tests.Unit.PersistenceTest;

[TestFixture]
public class InfluxWriteClientTest
{
    [Test]
    public void Test_GetWriteApiAsync_ReturnsValidApi()
    {
        var influxDbClient = new Mock<InfluxDBClient>("http://localhost:8086", "token");
        InfluxWriteClient client = new InfluxWriteClient(influxDbClient.Object);
        Assert.That(client.GetWriteApiAsync(), Is.Not.Null);
    }
}
