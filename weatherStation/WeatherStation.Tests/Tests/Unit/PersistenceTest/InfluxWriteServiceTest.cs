using Persistance.Influx;
using Persistance.Models;

using InfluxDB.Client.Writes;
using It = Moq.It;
using Microsoft.Extensions.Options;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InfluxDB.Client;

namespace WeatherStation.Tests.Tests.Unit.PersistenceTest;

/// <summary>
/// Tests for Influx write service.
/// </summary>
[TestFixture]
public class InfluxWriteServiceTest
{
    [Test]
    public async Task Test_Write_EmptyCollection_ReturnsZero()
    {
        Mock<IInfluxClientFactory> clientFactory = new ServiceTestMockBuilder<IInfluxClientFactory>.Builder()
            .Build();
        InfluxOptions options = new InfluxOptions
        {
            Bucket = "test-bucket",
            Org = "test-org"
        };
        IOptions<InfluxOptions> optionsWrapper = Options.Create(options);
        ILogger logger = new LoggerConfiguration().CreateLogger();

        InfluxWriteService service = new InfluxWriteService(clientFactory.Object, optionsWrapper, logger);

        int result = await service.WriteAsync(new List<WeatherReading>(), CancellationToken.None);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task Test_Write_ConvertsToPointsAndWrites()
    {
        Mock<IWriteApiAsync> writeApi = new Mock<IWriteApiAsync>();
        Mock<IInfluxWriteClient> writeClient = new Mock<IInfluxWriteClient>();
        writeClient.Setup(c => c.GetWriteApiAsync()).Returns(writeApi.Object);

        Mock<IInfluxClientFactory> clientFactory = new ServiceTestMockBuilder<IInfluxClientFactory>.Builder()
            .Setup(f => f.GetWriteClient(), writeClient.Object)
            .Build();

        InfluxOptions options = new InfluxOptions
        {
            Bucket = "test-bucket",
            Org = "test-org"
        };
        IOptions<InfluxOptions> optionsWrapper = Options.Create(options);
        ILogger logger = new LoggerConfiguration().CreateLogger();

        InfluxWriteService service = new InfluxWriteService(clientFactory.Object, optionsWrapper, logger);

        List<WeatherReading> readings = new List<WeatherReading>
        {
            new WeatherReading("station-1", "temperature", 22.5, "°C", DateTime.UtcNow)
        };

        int result = await service.WriteAsync(readings, CancellationToken.None);

        Assert.That(result, Is.EqualTo(1));
        writeApi.Verify(w => w.WritePointsAsync(
            It.IsAny<List<PointData>>(),
            "test-bucket",
            "test-org",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Test_Write_InfluxError_ThrowsException()
    {
        Mock<IWriteApiAsync> writeApi = new Mock<IWriteApiAsync>();
        writeApi.Setup(w => w.WritePointsAsync(
            It.IsAny<List<PointData>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("InfluxDB error"));

        Mock<IInfluxWriteClient> writeClient = new Mock<IInfluxWriteClient>();
        writeClient.Setup(c => c.GetWriteApiAsync()).Returns(writeApi.Object);

        Mock<IInfluxClientFactory> clientFactory = new ServiceTestMockBuilder<IInfluxClientFactory>.Builder()
            .Setup(f => f.GetWriteClient(), writeClient.Object)
            .Build();

        InfluxOptions options = new InfluxOptions
        {
            Bucket = "test-bucket",
            Org = "test-org"
        };
        IOptions<InfluxOptions> optionsWrapper = Options.Create(options);
        ILogger logger = new LoggerConfiguration().CreateLogger();

        InfluxWriteService service = new InfluxWriteService(clientFactory.Object, optionsWrapper, logger);

        List<WeatherReading> readings = new List<WeatherReading>
        {
            new WeatherReading("station-1", "temperature", 22.5, "°C", DateTime.UtcNow)
        };

        Assert.ThrowsAsync<InvalidOperationException>(async () => await service.WriteAsync(readings, CancellationToken.None));
    }
}
