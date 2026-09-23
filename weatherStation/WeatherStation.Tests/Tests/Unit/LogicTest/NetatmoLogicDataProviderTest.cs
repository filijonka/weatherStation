using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Application.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using Serilog;
using API.Logic;
using API.Auth.Netatmo.Options;
using Moq.Protected;

namespace WeatherStation.Tests.Tests.Unit.LogicTest;

[TestFixture]
public class NetatmoLogicDataProviderTest
{
    private Mock<IHttpClientFactory> httpClientFactoryMock;
    private IOptions<NetatmoOptions> options;
    private IMemoryCache memoryCache;
    private Mock<ILogger> loggerMock;
    private NetatmoLogicDataProvider provider;

    [SetUp]
    public void SetUp()
    {
        httpClientFactoryMock = new Mock<IHttpClientFactory>();
        options = Options.Create(new NetatmoOptions { TokenUrl = "https://api.netatmo.com/oauth2/token" });
        memoryCache = new MemoryCache(new MemoryCacheOptions());
        loggerMock = new Mock<ILogger>();
        loggerMock.Setup(x => x.ForContext<It.IsAnyType>()).Returns(loggerMock.Object);
        provider = new NetatmoLogicDataProvider(httpClientFactoryMock.Object, options, memoryCache, loggerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        memoryCache?.Dispose();
    }

    [Test]
    public async Task GetHomesDataAsync_ReturnsCachedObject()
    {
        var cached = new JsonHome { Status = "ok" };
        memoryCache.Set("netatmo:homes", cached);
        var result = await provider.GetHomesDataAsync("token", new string[0], CancellationToken.None);
        Assert.That(result, Is.EqualTo(cached));
    }

    [Test]
    public void GetHomesDataAsync_ThrowsOnHttpError()
    {
        Mock<HttpMessageHandler> handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("error") });
        var client = new HttpClient(handler.Object);
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        memoryCache.Remove("netatmo:homes");
        Assert.ThrowsAsync<HttpRequestException>(async () =>
            await provider.GetHomesDataAsync("token", new string[0], CancellationToken.None));
    }

    [Test]
    public void GetHomesDataAsync_ThrowsOnNullDeserialization()
    {
        Mock<HttpMessageHandler> handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null") });
        HttpClient client = new HttpClient(handler.Object);
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        memoryCache.Remove("netatmo:homes");
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await provider.GetHomesDataAsync("token", new string[0], CancellationToken.None));
    }

    [Test]
    public void GetModuleRoomMap_ReturnsCorrectMapping()
    {
        JsonHome home = new JsonHome
        {
            Body = new Body
            {
                Homes = new List<Home>
                {
                    new Home
                    {
                        Rooms = new List<Room> { new Room { Id = "room1", Name = "Room1" } },
                        Modules = new List<Module> { new Module { Id = "mod1", RoomId = "room1" } }
                    }
                }
            }
        };
        var map = provider.GetModuleRoomMap(home);
        Assert.That(map["mod1"].RoomId, Is.EqualTo("room1"));
        Assert.That(map["mod1"].RoomName, Is.EqualTo("Room1"));
    }

    [Test]
    public void BuildHomesDataQueryString_WithDuplicates_Deduplicates()
    {
        // Test that duplicates are removed
        var gatewayTypes = new[] { "NLG", "OTH", "NBG", "BNMH", "BNS", "NLG", "OTH", "NBG", "BNMH", "BNS", "NLG" };
        var result = typeof(NetatmoLogicDataProvider)
            .GetMethod("BuildHomesDataQueryString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .Invoke(null, new object[] { gatewayTypes }) as string;
        var count = result.Split("gateway_types=").Length - 1;
        // Method uses HashSet which deduplicates, so 11 items with duplicates become 5 unique
        Assert.That(count, Is.EqualTo(5));
    }

    [Test]
    public void BuildHomesDataQueryString_WithMaxValidTypes_ReturnsAll()
    {
        // Currently only 5 valid gateway types exist: NLG, OTH, NBG, BNMH, BNS
        // This tests that all 5 are included when provided
        // Note: The cap at 10 (selected.Count >= 10) cannot be reached with current valid types
        var gatewayTypes = new[] { "NLG", "OTH", "NBG", "BNMH", "BNS" };
        var result = typeof(NetatmoLogicDataProvider)
            .GetMethod("BuildHomesDataQueryString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .Invoke(null, new object[] { gatewayTypes }) as string;
        var count = result.Split("gateway_types=").Length - 1;
        Assert.That(count, Is.EqualTo(5));
        Assert.That(result, Does.Contain("NLG"));
        Assert.That(result, Does.Contain("OTH"));
        Assert.That(result, Does.Contain("NBG"));
        Assert.That(result, Does.Contain("BNMH"));
        Assert.That(result, Does.Contain("BNS"));
    }

    [Test]
    public void GetApiBaseUrl_ReturnsDefaultOnMalformedUrl()
    {
        var badOptions = Options.Create(new NetatmoOptions { TokenUrl = "not-a-url" });
        var badProvider = new NetatmoLogicDataProvider(httpClientFactoryMock.Object, badOptions, memoryCache, loggerMock.Object);
        var result = typeof(NetatmoLogicDataProvider)
            .GetMethod("GetApiBaseUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(badProvider, null) as string;
        Assert.That(result, Is.EqualTo("https://api.netatmo.com"));
    }

    [Test]
    public void BuildModuleRoomMap_ReturnsCorrectMapping()
    {
        var home = new JsonHome
        {
            Body = new Body
            {
                Homes = new List<Home>
                {
                    new Home
                    {
                        Rooms = new List<Room> { new Room { Id = "room1", Name = "Room1" } },
                        Modules = new List<Module> { new Module { Id = "mod1", RoomId = "room1" } }
                    }
                }
            }
        };
        var method = typeof(NetatmoLogicDataProvider)
            .GetMethod("BuildModuleRoomMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method.Invoke(null, new object[] { home });
        Assert.That(result, Is.Not.Null);
        var dict = result as System.Collections.IDictionary;
        Assert.That(dict, Is.Not.Null);
        Assert.That(dict.Contains("mod1"), Is.True);
    }
}
