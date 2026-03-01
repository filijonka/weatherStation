using Application.Models;
using Persistence.Influx;
using Persistence.Influx.Interface;
using Persistence.Models;
using Persistence.Models.Interface;

using InfluxDB.Client.Writes;
using Microsoft.Extensions.Options;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using InfluxDB.Client;
using InfluxDB.Client.Core.Exceptions;
using Newtonsoft.Json;

namespace WeatherStation.Tests.Tests.Unit.PersistenceTest;

/// <summary>
/// Tests for Influx write service.
/// </summary>
[TestFixture]
public class InfluxWriteServiceTest
{
    private IReadOnlyCollection<IInfluxPoint> testPoints;

    [SetUp]
    public void SetUp()
    {
        // Load and deserialize test data
        string path = Path.Combine(AppContext.BaseDirectory, "Files", "response_stationdata.json");
        string stationJson = File.ReadAllText(path);

        JsonStationData stationData =
            JsonConvert.DeserializeObject<JsonStationData>(stationJson);

        List<IInfluxPoint> points = new List<IInfluxPoint>();
        foreach (Device device in stationData.Body.Devices)
        {
            // Indoor point
            NetatmoIndoorPoint indoor = new NetatmoIndoorPoint("indoor");
            indoor.Initialize(device);
            points.Add(indoor);

            // Outdoor, Rain, Wind modules
            foreach (StationModule module in device.Modules)
            {
                switch (module.Type)
                {
                    case "NAModule1": // Outdoor
                        NetatmoOutdoorPoint outdoor = new NetatmoOutdoorPoint("outdoor");
                        outdoor.Initialize(module);
                        points.Add(outdoor);
                        break;
                    case "NAModule3": // Rain
                        NetatmoRainPoint rain = new NetatmoRainPoint("rain");
                        rain.Initialize(module);
                        points.Add(rain);
                        break;
                    case "NAModule2": // Wind
                        NetatmoWindPoint wind = new NetatmoWindPoint("wind");
                        wind.Initialize(module);
                        points.Add(wind);
                        break;
                    case "NAModule4": // Indoor module (Bedroom)
                        NetatmoIndoorPoint bedroom = new NetatmoIndoorPoint("bedroom");
                        bedroom.Initialize(module);
                        points.Add(bedroom);
                        break;
                }
            }
        }

        testPoints = points;
    }

    [Test]
    public async Task Test_Write_EmptyCollection_ReturnsZero()
    {
        //Arrange
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

        //Act
        int result = await service.WriteAsync(new List<IInfluxPoint>(), CancellationToken.None);

        //Assert
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task Test_Write_NonEmptyCollection_ReturnsCount()
    {
        //Arrange
        Mock<IWriteApiAsync> writeApi = new Mock<IWriteApiAsync>();

        Mock<IInfluxWriteClient> writeClient = new Mock<IInfluxWriteClient>();
        writeClient.Setup(x => x.GetWriteApiAsync()).Returns(writeApi.Object);

        Mock<IInfluxClientFactory> clientFactory = new Mock<IInfluxClientFactory>();
        clientFactory.Setup(x => x.GetWriteClient()).Returns(writeClient.Object);

        InfluxOptions options = new InfluxOptions { Bucket = "bucket", Org = "org" };
        IOptions<InfluxOptions> optionsWrapper = Options.Create(options);

        ILogger logger = new LoggerConfiguration().CreateLogger();
        InfluxWriteService service = new InfluxWriteService(clientFactory.Object, optionsWrapper, logger);

        // Use realistic test data
        int result = await service.WriteAsync(new List<IInfluxPoint>(testPoints), CancellationToken.None);
        Assert.That(result, Is.EqualTo(testPoints.Count));
    }

    [Test]
    public async Task Test_Write_ErrorHandling_LogsHttpException()
    {
        //Arrange
        Mock<IWriteApiAsync> writeApi = new Mock<IWriteApiAsync>();
        writeApi.Setup(x => x.WritePointsAsync(
                It.IsAny<IEnumerable<PointData>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new HttpException("http error", 500));

        Mock<IInfluxWriteClient> writeClient = new Mock<IInfluxWriteClient>();
        writeClient.Setup(x => x.GetWriteApiAsync()).Returns(writeApi.Object);

        Mock<IInfluxClientFactory> clientFactory = new Mock<IInfluxClientFactory>();
        clientFactory.Setup(x => x.GetWriteClient()).Returns(writeClient.Object);

        InfluxOptions options = new InfluxOptions { Bucket = "bucket", Org = "org" };
        IOptions<InfluxOptions> optionsWrapper = Options.Create(options);

        Mock<ILogger> loggerMock = new Mock<ILogger>();
        loggerMock.Setup(x => x.ForContext<It.IsAnyType>()).Returns(loggerMock.Object);

        InfluxWriteService service = new InfluxWriteService(clientFactory.Object, optionsWrapper, loggerMock.Object);

        int result = await service.WriteAsync(new List<IInfluxPoint>(testPoints), CancellationToken.None);

        writeApi.Verify(x => x.WritePointsAsync(
                It.IsAny<IEnumerable<PointData>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());

        Assert.That(result, Is.EqualTo(0));
        loggerMock.Verify(
            x => x.Error(It.Is<string>(msg => msg.Contains("We got connection error"))),
            Times.AtLeastOnce()
        );
    }

    [Test]
    public async Task Test_Write_ErrorHandling_LogsGenericException()
    {
        Mock<IWriteApiAsync> writeApi = new Mock<IWriteApiAsync>();
        writeApi.Setup(x => x.WritePointsAsync(
                It.IsAny<IEnumerable<PointData>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new Exception("generic error"));

        Mock<IInfluxWriteClient> writeClient = new Mock<IInfluxWriteClient>();
        writeClient.Setup(x => x.GetWriteApiAsync()).Returns(writeApi.Object);

        Mock<IInfluxClientFactory> clientFactory = new Mock<IInfluxClientFactory>();
        clientFactory.Setup(x => x.GetWriteClient()).Returns(writeClient.Object);

        InfluxOptions options = new InfluxOptions { Bucket = "bucket", Org = "org" };
        IOptions<InfluxOptions> optionsWrapper = Options.Create(options);

        Mock<ILogger> loggerMock = new Mock<ILogger>();
        loggerMock.Setup(x => x.ForContext<It.IsAnyType>())
            .Returns(loggerMock.Object);

        InfluxWriteService service = new InfluxWriteService(clientFactory.Object, optionsWrapper, loggerMock.Object);

        int result = await service.WriteAsync(new List<IInfluxPoint>(testPoints), CancellationToken.None);
        Assert.That(result, Is.EqualTo(0));
        loggerMock.Verify(
            x => x.Error(
                It.IsAny<Exception>(),
                It.Is<string>(msg => msg.Contains("We got an exception")
                )
            ),
            Times.AtLeastOnce()
        );
    }
}