using API.Auth.Netatmo.Options;
using API.Exceptions;
using API.Logic;

using Microsoft.Extensions.Options;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Application.Models;
using Microsoft.Extensions.Caching.Memory;
using Moq.Protected;
using Newtonsoft.Json;

namespace WeatherStation.Tests.Tests.Unit.LogicTest;

[TestFixture]
public class NetatmoLogicDataProviderDeserializationTest
{
    [Test]
    public async Task Test_GetHomesDataAsync_Deserializes_RealFixture()
    {
        string fixturePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "response_homesdata.json");
        string responseBody = await File.ReadAllTextAsync(fixturePath, CancellationToken.None);

        using HttpClient httpClient = new HttpClient(new StubHttpMessageHandler(responseBody));
        httpClient.BaseAddress = new Uri("https://api.netatmo.com");

        Mock<IHttpClientFactory> httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        NetatmoOptions netatmoOptions = new NetatmoOptions
        {
            TokenUrl = "https://api.netatmo.com/oauth2/token"
        };

        IOptions<NetatmoOptions> optionsWrapper = Options.Create(netatmoOptions);
        Mock<ILogger> mockLogger = new Mock<ILogger>();
        Mock<IMemoryCache> mockMemoryCache = new Mock<IMemoryCache>();

        NetatmoLogicDataProvider provider = new NetatmoLogicDataProvider(
            httpClientFactoryMock.Object,
            optionsWrapper,
            mockMemoryCache.Object,
            mockLogger.Object
        );

        JsonHome result = await provider.GetHomesDataAsync("access-token", null, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("ok"));
            Assert.That(result.Body, Is.Not.Null);
        });
        Assert.That(result.Body.Homes, Is.Not.Null);
        Assert.That(result.Body.Homes.Count, Is.GreaterThanOrEqualTo(1));

        Home home = result.Body.Homes[0];
        Assert.Multiple(() =>
        {
            Assert.That(home.Id, Is.Not.Empty);
            Assert.That(home.Rooms, Is.Not.Null);
            Assert.That(home.Modules, Is.Not.Null);
        });

        Room roomWithModules = home.Rooms.Find(r => r.ModuleIds != null && r.ModuleIds.Count > 0);
        Assert.That(roomWithModules, Is.Not.Null);

        Module moduleWithBridge = home.Modules.Find(m => !string.IsNullOrWhiteSpace(m.Bridge));
        Assert.That(moduleWithBridge, Is.Not.Null);
    }

    [Test]
    public async Task Test_GetModuleDataAsync_Deserializes_RealFixture()
    {
        string fixturePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "response_stationdata.json");
        string responseBody = await File.ReadAllTextAsync(fixturePath, CancellationToken.None);

        using HttpClient httpClient = new HttpClient(new StubHttpMessageHandler(responseBody));
        httpClient.BaseAddress = new Uri("https://api.netatmo.com");

        Mock<IHttpClientFactory> httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        NetatmoOptions netatmoOptions = new NetatmoOptions
        {
            TokenUrl = "https://api.netatmo.com/oauth2/token"
        };

        IOptions<NetatmoOptions> optionsWrapper = Options.Create(netatmoOptions);
        Mock<ILogger> mockLogger = new Mock<ILogger>();
        Mock<IMemoryCache> mockMemoryCache = new Mock<IMemoryCache>();

        NetatmoLogicDataProvider provider = new NetatmoLogicDataProvider(
            httpClientFactoryMock.Object,
            optionsWrapper,
            mockMemoryCache.Object,
            mockLogger.Object
        );


        JsonStationData result = await provider.GetModuleDataAsync(
            accessToken: "access-token",
            moduleId: "AA:BB:CC:00:00:10",
            cancellationToken: CancellationToken.None
        );

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("ok"));
            Assert.That(result.Body, Is.Not.Null);
        });

        Assert.That(result.Body.Devices, Is.Not.Null);
        Assert.That(result.Body.Devices.Count, Is.GreaterThanOrEqualTo(1));

        Device device = result.Body.Devices[0];
        Assert.That(device.Id, Is.Not.Empty);
        Assert.That(device.Modules, Is.Not.Null);
        Assert.That(device.Modules.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(device.Modules[0].Id, Is.Not.Empty);
    }

    [Test]
    public async Task Test_GetModuleDataAsync_BuildsDeviceIdQueryAndSetsBearer()
    {
        HttpRequestMessage capturedRequest = null;

        string okJson = JsonConvert.SerializeObject(new
        {
            status = "ok",
            body = new
            {
                devices = new[]
                {
                    new
                    {
                        _id = "AA:BB:CC:00:00:10",
                        dashboard_data = new { time_utc = 1 },
                        modules = Array.Empty<object>()
                    }
                }
            },
            time_exec = 0.1,
            time_server = 123
        });

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(okJson)
            });

        HttpClient httpClient = new HttpClient(mockHandler.Object);

        Mock<IHttpClientFactory> httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        IOptions<NetatmoOptions> options = Options.Create(new NetatmoOptions
        {
            TokenUrl = "https://api.netatmo.com/oauth2/token"
        });

        Mock<ILogger> mockLogger = new Mock<ILogger>();
        Mock<IMemoryCache> mockMemoryCache = new Mock<IMemoryCache>();

        NetatmoLogicDataProvider provider = new NetatmoLogicDataProvider(
            httpClientFactoryMock.Object,
            options,
            mockMemoryCache.Object,
            mockLogger.Object
        );


        _ = await provider.GetModuleDataAsync(
            accessToken: "access-token",
            moduleId: "AA:BB:CC:00:00:10",
            cancellationToken: CancellationToken.None
        );

        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.RequestUri, Is.Not.Null);

        string url = capturedRequest.RequestUri!.ToString();
        Assert.That(url, Does.Contain("/api/getstationsdata?device_id=AA%3ABB%3ACC%3A00%3A00%3A10"));

        Assert.That(capturedRequest.Headers.Authorization, Is.Not.Null);
        Assert.That(capturedRequest.Headers.Authorization!.Scheme, Is.EqualTo("Bearer"));
        Assert.That(capturedRequest.Headers.Authorization!.Parameter, Is.EqualTo("access-token"));

        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Test]
    public void Test_GetModuleDataAsync_WhenBadRequest_ThrowsNetatmoBadRequestException_WithEnvelope()
    {
        const string badJson = "{\"error\":{\"code\":3,\"message\":\"Invalid argument\"}}";

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(badJson)
            });

        HttpClient httpClient = new HttpClient(mockHandler.Object);

        Mock<IHttpClientFactory> httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        IOptions<NetatmoOptions> options = Options.Create(new NetatmoOptions
        {
            TokenUrl = "https://api.netatmo.com/oauth2/token"
        });

        ILogger logger = new LoggerConfiguration().CreateLogger();
        Mock<IMemoryCache> mockMemoryCache = new Mock<IMemoryCache>();

        NetatmoLogicDataProvider provider = new NetatmoLogicDataProvider(
            httpClientFactoryMock.Object,
            options,
            mockMemoryCache.Object,
            logger
        );

        NetatmoBadRequestException ex = Assert.ThrowsAsync<NetatmoBadRequestException>(async () =>
            await provider.GetModuleDataAsync("access-token", "AA:BB:CC:00:00:10", CancellationToken.None));
        
        Assert.That(ex, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(ex.Code, Is.EqualTo(3));
            Assert.That(ex.ApiMessage, Is.EqualTo("Invalid argument"));
        });
    }

    [Test]
    public void Test_GetModuleDataAsync_WhenBadRequestBodyMalformed_ThrowsNetatmoBadRequestException_DefaultMessage()
    {
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("not-json")
            });

        HttpClient httpClient = new HttpClient(mockHandler.Object);

        Mock<IHttpClientFactory> httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        IOptions<NetatmoOptions> options = Options.Create(new NetatmoOptions
        {
            TokenUrl = "https://api.netatmo.com/oauth2/token"
        });

        ILogger logger = new LoggerConfiguration().CreateLogger();
        Mock<IMemoryCache> mockMemoryCache = new Mock<IMemoryCache>();

        NetatmoLogicDataProvider provider = new NetatmoLogicDataProvider(
            httpClientFactoryMock.Object,
            options,
            mockMemoryCache.Object,
            logger
        );


        NetatmoBadRequestException ex = Assert.ThrowsAsync<NetatmoBadRequestException>(async () =>
            await provider.GetModuleDataAsync("access-token", "AA:BB:CC:00:00:10", CancellationToken.None));

        Assert.That(ex, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(ex.Code, Is.EqualTo(-1));
            Assert.That(ex.ApiMessage, Is.EqualTo("Netatmo request was rejected."));
        });
    }

    [Test]
    public void Test_GetModuleDataAsync_WhenNonSuccessNon400_ThrowsHttpRequestException()
    {
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("server-error")
            });

        HttpClient httpClient = new HttpClient(mockHandler.Object);

        Mock<IHttpClientFactory> httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        IOptions<NetatmoOptions> options = Options.Create(new NetatmoOptions
        {
            TokenUrl = "https://api.netatmo.com/oauth2/token"
        });

        ILogger logger = new LoggerConfiguration().CreateLogger();
        Mock<IMemoryCache> mockMemoryCache = new Mock<IMemoryCache>();

        NetatmoLogicDataProvider provider = new NetatmoLogicDataProvider(
            httpClientFactoryMock.Object,
            options,
            mockMemoryCache.Object,
            logger
        );


        HttpRequestException ex = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await provider.GetModuleDataAsync("access-token", "AA:BB:CC:00:00:10", CancellationToken.None));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Message, Does.Contain("InternalServerError"));
    }

    [Test]
    public void Test_GetModuleDataAsync_WhenBodyNullLiteral_ThrowsInvalidOperationException()
    {
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("null")
            });

        HttpClient httpClient = new HttpClient(mockHandler.Object);

        Mock<IHttpClientFactory> httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        IOptions<NetatmoOptions> options = Options.Create(new NetatmoOptions
        {
            TokenUrl = "https://api.netatmo.com/oauth2/token"
        });

        Mock<ILogger> mockLogger = new Mock<ILogger>();
        Mock<IMemoryCache> mockMemoryCache = new Mock<IMemoryCache>();

        NetatmoLogicDataProvider provider = new NetatmoLogicDataProvider(
            httpClientFactoryMock.Object,
            options,
            mockMemoryCache.Object,
            mockLogger.Object
        );

        _ = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await provider.GetModuleDataAsync("access-token", "AA:BB:CC:00:00:10", CancellationToken.None));
    }
   
    [Test]
    [TestCaseSource(nameof(GatewayCorrectCases))]
    public async Task Test_GetHomesDataAsync_BuildsGatewayTypesQuery_CorrectCases(string[] gateways)
    {
        // Arrange: capture outbound request
        HttpRequestMessage capturedRequest = null;

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();

        string okJson = JsonConvert.SerializeObject(new
        {
            body = new { homes = new[] { new { id = "1", name = "Home" } } },
            status = "ok",
            time_exec = 0.1,
            time_server = 123
        });

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(okJson)
            });

        HttpClient httpClient = new(mockHandler.Object);

        Mock<IHttpClientFactory> mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        IOptions<NetatmoOptions> options = Options.Create(new NetatmoOptions
        {
            ApiBaseUrl = "https://api.netatmo.com"
        });

        Mock<ILogger> mockLogger = new Mock<ILogger>();
        Mock<IMemoryCache> mockMemoryCache = new Mock<IMemoryCache>();

        NetatmoLogicDataProvider provider = new NetatmoLogicDataProvider(
            mockHttpClientFactory.Object,
            options,
            mockMemoryCache.Object,
            mockLogger.Object
        );


        // Act
        _ = await provider.GetHomesDataAsync(
            accessToken: "access-token",
            gatewayTypes: gateways,
            cancellationToken: CancellationToken.None
        );

        // Assert: indirect test of BuildHomesDataQueryString via the outgoing URL
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.RequestUri, Is.Not.Null);

        string url = capturedRequest.RequestUri!.ToString();

        foreach (string gateway in gateways)
        {
            Assert.That(url, Does.Contain($"gateway_types={gateway}"));
        }

        if (gateways.Length == 0)
        {
            Assert.That(url, Does.Not.Contain("gateway"));
        }

        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Test]
    public async Task Test_GetHomesDataAsync_BuildsGatewayTypesQuery_ContainsEmpty()
    {
        // Arrange: capture outbound request
        HttpRequestMessage capturedRequest = null;

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();

        string okJson = JsonConvert.SerializeObject(new
        {
            body = new { homes = new[] { new { id = "1", name = "Home" } } },
            status = "ok",
            time_exec = 0.1,
            time_server = 123
        });

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(okJson)
            });

        HttpClient httpClient = new(mockHandler.Object);

        Mock<IHttpClientFactory> mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        IOptions<NetatmoOptions> options = Options.Create(new NetatmoOptions
        {
            ApiBaseUrl = "https://api.netatmo.com"
        });

        Mock<ILogger> mockLogger = new Mock<ILogger>();
        Mock<IMemoryCache> mockMemoryCache = new Mock<IMemoryCache>();

        NetatmoLogicDataProvider provider = new NetatmoLogicDataProvider(
            mockHttpClientFactory.Object,
            options,
            mockMemoryCache.Object,
            mockLogger.Object
        );

        // Act
        _ = await provider.GetHomesDataAsync(
            accessToken: "access-token",
            gatewayTypes: new string[] {"NLG", ""},
            cancellationToken: CancellationToken.None
        );

        // Assert: indirect test of BuildHomesDataQueryString via the outgoing URL
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.RequestUri, Is.Not.Null);

        string url = capturedRequest.RequestUri!.ToString();

        
        Assert.That(url, Does.Contain($"gateway_types=NLG"));
        
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Test]
    [TestCaseSource(nameof(GatewayInCorrectCases))]
    public async Task Test_GetHomesDataAsync_BuildsGatewayTypesQuery_InCorrectCases(string[] gateways)
    {
        // Arrange: capture outbound request
        HttpRequestMessage capturedRequest = null;

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();

        string okJson = JsonConvert.SerializeObject(new
        {
            body = new { homes = new[] { new { id = "1", name = "Home" } } },
            status = "ok",
            time_exec = 0.1,
            time_server = 123
        });

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(okJson)
            });

        HttpClient httpClient = new(mockHandler.Object);

        Mock<IHttpClientFactory> mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        IOptions<NetatmoOptions> options = Options.Create(new NetatmoOptions
        {
            ApiBaseUrl = "https://api.netatmo.com"
        });

        Mock<ILogger> mockLogger = new Mock<ILogger>();
        Mock<IMemoryCache> mockMemoryCache = new Mock<IMemoryCache>();

        NetatmoLogicDataProvider provider = new NetatmoLogicDataProvider(
            mockHttpClientFactory.Object,
            options,
            mockMemoryCache.Object,
            mockLogger.Object
        );

        // Act
        _ = await provider.GetHomesDataAsync(
            accessToken: "access-token",
            gatewayTypes: gateways,
            cancellationToken: CancellationToken.None
        );

        // Assert: indirect test of BuildHomesDataQueryString via the outgoing URL
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.RequestUri, Is.Not.Null);

        string url = capturedRequest.RequestUri!.ToString();

        foreach (string gateway in gateways)
        {
            Assert.That(url, Does.Not.Contain($"gateway_types={gateway}"));
        }

        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string responseBody;

        public StubHttpMessageHandler(string responseBody)
        {
            this.responseBody = responseBody;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(this.responseBody)
            };

            return Task.FromResult(response);
        }
    }
    private static IEnumerable<TestCaseData> GatewayCorrectCases()
    {
        yield return new TestCaseData((object) new string[]{"NLG"});
        yield return new TestCaseData((object) new string[]{"NLG", "OTH"});
        yield return new TestCaseData((object) Array.Empty<string>());
    }

    private static IEnumerable<TestCaseData> GatewayInCorrectCases()
    {
        yield return new TestCaseData((object) new string[]{"INVALID"});
        yield return new TestCaseData((object) new string[]{"read_station&x=y"});
        yield return new TestCaseData((object) new string[]{"x=1234567891011234"});
    }

}
