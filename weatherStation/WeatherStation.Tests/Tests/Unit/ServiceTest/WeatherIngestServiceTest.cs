using API.Services;

using Moq;
using Serilog;
using System;
using System.Threading;
using Persistance.Influx;

namespace WeatherStation.Tests.Tests.Unit.ServiceTest;

/// <summary>
/// Tests for weather ingest service.
/// </summary>
[TestFixture]
public class WeatherIngestServiceTest
{
    /// <summary>
    /// Fetch and store should throw not implemented exception.
    /// </summary>
    [Test]
    public void Test_FetchAndStore_ThrowsNotImplementedException()
    {
        Mock<IInfluxWriteService> influxWriteService = new ServiceTestMockBuilder<IInfluxWriteService>.Builder()
            .Build();
        ILogger logger = new LoggerConfiguration().CreateLogger();

        WeatherIngestService service = new WeatherIngestService(influxWriteService.Object, logger);

        Assert.ThrowsAsync<NotImplementedException>(async () => await service.FetchAndStoreAsync(CancellationToken.None));
    }
}
