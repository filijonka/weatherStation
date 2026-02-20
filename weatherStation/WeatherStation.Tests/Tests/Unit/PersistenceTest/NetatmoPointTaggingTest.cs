using Application.Models;
using Persistence.Models;
using Persistence.Models.Interface;

namespace WeatherStation.Tests.Tests.Unit.PersistenceTest;

/// <summary>
/// Tests for module room tag enrichment on Netatmo points.
/// </summary>
[TestFixture]
public class NetatmoPointTaggingTest
{
    [TestCase("NAModule1")]
    [TestCase("NAModule2")]
    [TestCase("NAModule3")]
    [TestCase("NAModule4")]
    public void Test_Initialize_Module_AddsRoomTags(string moduleType)
    {
        NetatmoPointFactory factory = new NetatmoPointFactory("netatmo");
        IInfluxPoint point = factory.Create(moduleType);

        StationModule module = new StationModule
        {
            Id = "AA:BB:CC:00:00:01",
            Type = moduleType,
            ModuleName = "Module",
            RoomId = "room-1",
            RoomName = "Living",
            DashboardData = new Dashboard
            {
                TimeUtc = 1,
                Temperature = 20,
                Humidity = 50,
                Rain = 0,
                WindStrength = 2
            }
        };

        point.Initialize(module);

        Assert.That(point.Tags.ContainsKey("room_id"), Is.True);
        Assert.That(point.Tags["room_id"], Is.EqualTo("room-1"));
        Assert.That(point.Tags.ContainsKey("room_name"), Is.True);
        Assert.That(point.Tags["room_name"], Is.EqualTo("Living"));
    }

    [Test]
    public void Test_Initialize_Module_WithoutRoomName_DoesNotAddRoomNameTag()
    {
        NetatmoPointFactory factory = new NetatmoPointFactory("netatmo");
        IInfluxPoint point = factory.Create("NAModule1");

        StationModule module = new StationModule
        {
            Id = "AA:BB:CC:00:00:01",
            Type = "NAModule1",
            ModuleName = "Module",
            RoomId = "room-1",
            RoomName = string.Empty,
            DashboardData = new Dashboard { TimeUtc = 1, Temperature = 20 }
        };

        point.Initialize(module);

        Assert.That(point.Tags.ContainsKey("room_id"), Is.True);
        Assert.That(point.Tags.ContainsKey("room_name"), Is.False);
    }
}
