using API.Services;
using API.Interface.Logic;
using Application.Models;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Persistence.Influx.Interface;
using Persistence.Models.Interface;

namespace WeatherStation.Tests.Tests.Unit.ServiceTest;

/// <summary>
/// Tests for weather ingest service.
/// </summary>
[TestFixture]
public class NetatmoServiceTest
{
    [Test]
    public void Test_FetchAndStoreAsync_WhenRoomMapEmpty_ThrowsInvalidOperationException()
    {
        Mock<IInfluxWriteService> influxWriteService = new Mock<IInfluxWriteService>();
        Mock<INetatmoLogicDataProvider> provider = new Mock<INetatmoLogicDataProvider>();
        provider
            .Setup(p => p.GetModuleRoomMap(It.IsAny<JsonHome>()))
            .Returns(new Dictionary<string, ModuleRoomInfo>());

        ILogger logger = new LoggerConfiguration().CreateLogger();
        NetatmoService service = new NetatmoService(influxWriteService.Object, provider.Object, logger);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.FetchAndStoreAsync("token", CancellationToken.None));
    }

    [Test]
    public async Task Test_FetchAndStoreAsync_ReturnsCount_ForDeviceAndModules()
    {
        Mock<IInfluxWriteService> influxWriteService = new Mock<IInfluxWriteService>();
        influxWriteService
            .Setup(s => s.WriteAsync(It.IsAny<IReadOnlyCollection<IInfluxPoint>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        Mock<INetatmoLogicDataProvider> provider = new Mock<INetatmoLogicDataProvider>();

        Dictionary<string, ModuleRoomInfo> roomMap = new Dictionary<string, ModuleRoomInfo>
        {
            ["AA:BB:CC:00:00:01"] = new ModuleRoomInfo("room-1", "Living"),
            ["AA:BB:CC:00:00:02"] = new ModuleRoomInfo("room-2", "Outdoor")
        };

        provider
            .Setup(p => p.GetModuleRoomMap(It.IsAny<JsonHome>()))
            .Returns(roomMap);

        JsonStationData stationData = new JsonStationData
        {
            Body = new StationBody
            {
                Devices = new List<Device>
                {
                    new Device
                    {
                        Id = "AA:BB:CC:00:00:10",
                        Type = "NAMain",
                        DashboardData = new Dashboard { TimeUtc = 1 },
                        Modules = new List<StationModule>
                        {
                            new StationModule
                            {
                                Id = "AA:BB:CC:00:00:01",
                                Type = "NAModule1",
                                ModuleName = "Outdoor Module",
                                DashboardData = new Dashboard { TimeUtc = 1 }
                            },
                            new StationModule
                            {
                                Id = "AA:BB:CC:00:00:02",
                                Type = "NAModule3",
                                ModuleName = "Rain Gauge",
                                DashboardData = new Dashboard { TimeUtc = 1 }
                            }
                        }
                    }
                }
            }
        };

        provider
            .Setup(p => p.GetModuleDataAsync("token", "AA:BB:CC:00:00:01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stationData);
        provider
            .Setup(p => p.GetModuleDataAsync("token", "AA:BB:CC:00:00:02", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stationData);

        ILogger logger = new LoggerConfiguration().CreateLogger();
        NetatmoService service = new NetatmoService(influxWriteService.Object, provider.Object, logger);

        int result = await service.FetchAndStoreAsync("token", CancellationToken.None);

        Assert.That(result, Is.EqualTo(3));

        provider.Verify(
            p => p.GetModuleRoomMap(It.IsAny<JsonHome>()),
            Times.Once);

        provider.Verify(
            p => p.GetModuleDataAsync("token", "AA:BB:CC:00:00:01", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}