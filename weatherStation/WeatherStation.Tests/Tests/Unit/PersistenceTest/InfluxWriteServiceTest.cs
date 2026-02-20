using Persistance.Influx;
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
using Persistence.Influx;
using Persistence.Influx.Interface;
using Persistence.Models;
using Persistence.Models.Interface;

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

        int result = await service.WriteAsync(new List<IInfluxPoint>(), CancellationToken.None);

        Assert.That(result, Is.EqualTo(0));
    }

}
