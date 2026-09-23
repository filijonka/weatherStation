using Application.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherStation.Tests.Tests.Unit.ModelTest;

[TestFixture]
public sealed class JsonStationDataValidationTest
{
    [Test]
    public void Dashboard_IsValid_WhenEmpty_ReturnsFalse()
    {
        Dashboard dashboard = new Dashboard();
        Assert.That(dashboard.IsValid(), Is.False);
    }

    [Test]
    public void Dashboard_IsValid_WhenExtraNullAndNoOtherData_ReturnsFalse()
    {
        Dashboard dashboard = new Dashboard
        {
            Extra = null
        };

        Assert.That(dashboard.IsValid(), Is.False);
    }

    [Test]
    public void Dashboard_IsValid_WhenTimeUtcPresent_ReturnsTrue()
    {
        Dashboard dashboard = new Dashboard { TimeUtc = 1 };
        Assert.That(dashboard.IsValid(), Is.True);
    }

    [Test]
    public void Dashboard_IsValid_WhenExtraPresent_ReturnsTrue()
    {
        Dashboard dashboard = new Dashboard
        {
            Extra = new Dictionary<string, JToken>
            {
                ["some_new_field"] = JToken.FromObject(123)
            }
        };

        Assert.That(dashboard.IsValid(), Is.True);
    }

    [Test]
    public void Dashboard_IsValid_WhenTemperaturePresent_ReturnsTrue()
    {
        Dashboard dashboard = new Dashboard { Temperature = 12.3 };
        Assert.That(dashboard.IsValid(), Is.True);
    }

    [Test]
    public void Dashboard_IsValid_WhenPressurePresent_ReturnsTrue()
    {
        Dashboard dashboard = new Dashboard { Pressure = 1001.2 };
        Assert.That(dashboard.IsValid(), Is.True);
    }

    [Test]
    public void Dashboard_IsValid_WhenRainPresent_ReturnsTrue()
    {
        Dashboard dashboard = new Dashboard { Rain = 0.1 };
        Assert.That(dashboard.IsValid(), Is.True);
    }

    [Test]
    public void Dashboard_IsValid_WhenWindPresent_ReturnsTrue()
    {
        Dashboard dashboard = new Dashboard { WindStrength = 5 };
        Assert.That(dashboard.IsValid(), Is.True);
    }

    [Test]
    public void StationDevice_IsValid_RequiresDashboardValid()
    {
        Device device = new Device
        {
            Id = "AA:BB:CC:00:00:10",
            DashboardData = new Dashboard(),
            Modules = new List<StationModule>()
        };

        Assert.That(device.IsValid(), Is.False);

        device.DashboardData = new Dashboard { TimeUtc = 1 };
        Assert.That(device.IsValid(), Is.True);
    }

    [Test]
    public void StationDevice_IsValid_WhenIdMissing_ReturnsFalse()
    {
        Device device = new Device
        {
            Id = " ",
            DashboardData = new Dashboard { TimeUtc = 1 },
            Modules = new List<StationModule>()
        };

        Assert.That(device.IsValid(), Is.False);
    }

    [Test]
    public void StationDevice_IsValid_WhenDashboardNull_ReturnsFalse()
    {
        Device device = new Device
        {
            Id = "AA:BB:CC:00:00:10",
            DashboardData = null,
            Modules = new List<StationModule>()
        };

        Assert.That(device.IsValid(), Is.False);
    }

    [Test]
    public void StationDevice_IsValid_WhenModulesNull_ReturnsFalse()
    {
        Device device = new Device
        {
            Id = "AA:BB:CC:00:00:10",
            DashboardData = new Dashboard { TimeUtc = 1 },
            Modules = null
        };

        Assert.That(device.IsValid(), Is.False);
    }

    [Test]
    public void StationDevice_IsValid_WhenAnyModuleNull_ReturnsFalse()
    {
        Device device = new Device
        {
            Id = "AA:BB:CC:00:00:10",
            DashboardData = new Dashboard { TimeUtc = 1 },
            Modules = new List<StationModule> { null }
        };

        Assert.That(device.IsValid(), Is.False);
    }

    [Test]
    public void StationModule_IsValid_RequiresIdAndDashboardValid()
    {
        StationModule module = new StationModule
        {
            Id = string.Empty,
            DashboardData = new Dashboard { TimeUtc = 1 }
        };

        Assert.That(module.IsValid(), Is.False);

        module.Id = "AA:BB:CC:00:00:01";
        module.DashboardData = new Dashboard();
        Assert.That(module.IsValid(), Is.False);

        module.DashboardData = new Dashboard { TimeUtc = 1 };
        Assert.That(module.IsValid(), Is.True);
    }

    [Test]
    public void StationModule_IsValid_WhenDashboardNull_ReturnsFalse()
    {
        StationModule module = new StationModule
        {
            Id = "AA:BB:CC:00:00:01",
            DashboardData = null
        };

        Assert.That(module.IsValid(), Is.False);
    }

    [Test]
    public void JsonStationData_IsValid_WhenBodyNull_ReturnsFalse()
    {
        JsonStationData stationData = new JsonStationData
        {
            Body = null
        };

        Assert.That(stationData.IsValid(), Is.False);
    }

    [Test]
    public void StationBody_IsValid_WhenDevicesNull_ReturnsFalse()
    {
        StationBody body = new StationBody
        {
            Devices = null
        };

        Assert.That(body.IsValid(), Is.False);
    }

    [Test]
    public void StationBody_IsValid_WhenAnyDeviceNull_ReturnsFalse()
    {
        StationBody body = new StationBody
        {
            Devices = new List<Device> { null }
        };

        Assert.That(body.IsValid(), Is.False);
    }

    [Test]
    public void StationBody_IsValid_WhenOneValidDevice_ReturnsTrue()
    {
        StationBody body = new StationBody
        {
            Devices = new List<Device>
            {
                new Device
                {
                    Id = "AA:BB:CC:00:00:10",
                    DashboardData = new Dashboard { TimeUtc = 1 },
                    Modules = new List<StationModule>()
                }
            }
        };

        Assert.That(body.IsValid(), Is.True);
    }

    [Test]
    public void JsonStationData_IsValid_WhenNoDevices_ReturnsFalse()
    {
        JsonStationData stationData = new JsonStationData
        {
            Body = new StationBody
            {
                Devices = new List<Device>()
            }
        };

        Assert.That(stationData.IsValid(), Is.False);
    }

    [Test]
    public void JsonStationData_IsValid_WhenAnyDeviceInvalid_ReturnsFalse()
    {
        Device invalidDevice = new Device
        {
            Id = "AA:BB:CC:00:00:10",
            DashboardData = new Dashboard(),
            Modules = new List<StationModule>()
        };

        JsonStationData stationData = new JsonStationData
        {
            Body = new StationBody
            {
                Devices = new List<Device> { invalidDevice }
            }
        };

        Assert.That(stationData.IsValid(), Is.False);
    }

    [Test]
    public void StationDevice_IsValid_WhenAnyModuleInvalid_ReturnsFalse()
    {
        Device device = new Device
        {
            Id = "AA:BB:CC:00:00:10",
            DashboardData = new Dashboard { TimeUtc = 1 },
            Modules = new List<StationModule>
            {
                new StationModule { Id = "", DashboardData = new Dashboard { TimeUtc = 1 } }
            }
        };

        Assert.That(device.IsValid(), Is.False);
    }

    [Test]
    public async Task JsonStationData_IsValid_WhenFixtureLoaded_ReturnsTrue()
    {
        string fixturePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "response_stationdata.json");
        string responseBody = await File.ReadAllTextAsync(fixturePath, CancellationToken.None);

        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };

        JsonStationData stationData = JsonConvert.DeserializeObject<JsonStationData>(responseBody, settings);
        Assert.That(stationData, Is.Not.Null);
        Assert.That(stationData!.IsValid(), Is.True);
    }
}
