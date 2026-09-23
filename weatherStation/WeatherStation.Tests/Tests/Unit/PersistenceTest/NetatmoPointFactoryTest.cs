using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Persistence.Influx;
using Persistence.Models;
using Persistence.Models.Interface;

namespace WeatherStation.Tests.Tests.Unit.PersistenceTest;

[TestFixture]
public class NetatmoPointFactoryTest : TestBase
{
    private Mock<IOptions<InfluxOptions>> optionMock;
    [SetUp]
    public void FactorySetup()
    {
        string optionString =
            "{\"Url\": \"http://influxdb:8181\",\"Token\": \"\",\"Bucket\": \"readings\",\"Tables\": {\"NAMain\": \"indoor\",\"NAModule1\": \"outdoor\"}}";
        InfluxOptions option = JsonConvert.DeserializeObject<InfluxOptions>(optionString);
        optionMock= new Mock<IOptions<InfluxOptions>>();
        optionMock.Setup(o => o.Value).Returns(option);
    }
    [Test]
    public void Test_Create_ThrowsForUnknownType()
    {
        NetatmoPointFactory factory = new NetatmoPointFactory(optionMock.Object,"measurement");
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.Create("UnknownType"));
    }

    [TestCase("NAMain", typeof(Persistence.Models.NetatmoIndoorPoint))]
    [TestCase("NAModule1", typeof(Persistence.Models.NetatmoOutdoorPoint))]
    [TestCase("NAModule2", typeof(Persistence.Models.NetatmoWindPoint))]
    [TestCase("NAModule3", typeof(Persistence.Models.NetatmoRainPoint))]
    [TestCase("NAModule4", typeof(Persistence.Models.NetatmoIndoorPoint))]
    public void Test_Create_ReturnsCorrectType(string type, Type expectedType)
    {
        NetatmoPointFactory factory = new NetatmoPointFactory(optionMock.Object,"measurement");
        IInfluxPoint point = factory.Create(type);
        Assert.That(point, Is.InstanceOf(expectedType));
    }

}
