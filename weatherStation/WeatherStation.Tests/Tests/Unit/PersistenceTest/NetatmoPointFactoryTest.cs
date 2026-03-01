using System;
using Persistence.Models;
using Persistence.Models.Interface;

namespace WeatherStation.Tests.Tests.Unit.PersistenceTest;

[TestFixture]
public class NetatmoPointFactoryTest
{
    [Test]
    public void Test_Create_ThrowsForUnknownType()
    {
        NetatmoPointFactory factory = new NetatmoPointFactory("measurement");
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.Create("UnknownType"));
    }

    [TestCase("NAMain", typeof(Persistence.Models.NetatmoIndoorPoint))]
    [TestCase("NAModule1", typeof(Persistence.Models.NetatmoOutdoorPoint))]
    [TestCase("NAModule2", typeof(Persistence.Models.NetatmoWindPoint))]
    [TestCase("NAModule3", typeof(Persistence.Models.NetatmoRainPoint))]
    [TestCase("NAModule4", typeof(Persistence.Models.NetatmoIndoorPoint))]
    public void Test_Create_ReturnsCorrectType(string type, Type expectedType)
    {
        NetatmoPointFactory factory = new NetatmoPointFactory("measurement");
        IInfluxPoint point = factory.Create(type);
        Assert.That(point, Is.InstanceOf(expectedType));
    }
}
