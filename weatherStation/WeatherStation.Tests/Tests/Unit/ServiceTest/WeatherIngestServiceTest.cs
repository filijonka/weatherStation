using API.Interface;
using API.Services;

using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Threading;
using Persistance.Influx;
using Serilog;
using WeatherStation.Tests.Tests;
using Moq;
using It = Moq.It;

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
