using API.Filters;
using NUnit.Framework;
using System;

namespace WeatherStation.Tests.Tests.Unit.FiltersTest;

[TestFixture]
public class NetatmoAuthenticatedAttributeTest
{
    [Test]
    public void Constructor_SetsTypeFilterCorrectly()
    {
        NetatmoAuthenticatedAttribute attribute = new NetatmoAuthenticatedAttribute();
        Assert.That(attribute.ImplementationType, Is.EqualTo(typeof(NetatmoAuthenticatedFilter)));
    }
}
