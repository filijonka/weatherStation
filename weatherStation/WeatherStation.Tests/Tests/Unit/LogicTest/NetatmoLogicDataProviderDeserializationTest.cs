using API.Auth.Netatmo.Options;
using API.Logic;

using Microsoft.Extensions.Options;
using Moq;
using Serilog;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherStation.Tests.Tests.Unit.LogicTest;

[TestFixture]
public class NetatmoLogicDataProviderDeserializationTest
{
    [Test]
    public async Task Test_GetHomesDataAsync_Deserializes_RealFixture()
    {
        string fixturePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "response_homesdata.json");
        string responseBody = await File.ReadAllTextAsync(fixturePath, CancellationToken.None);

        using HttpClient httpClient = new HttpClient(new StubHttpMessageHandler(responseBody))
        {
            BaseAddress = new Uri("https://api.netatmo.com")
        };

        Mock<IHttpClientFactory> httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        NetatmoOptions netatmoOptions = new NetatmoOptions
        {
            TokenUrl = "https://api.netatmo.com/oauth2/token"
        };

        IOptions<NetatmoOptions> optionsWrapper = Options.Create(netatmoOptions);
        ILogger logger = new LoggerConfiguration().CreateLogger();

        NetatmoLogicDataProvider provider = new NetatmoLogicDataProvider(
            httpClientFactoryMock.Object,
            optionsWrapper,
            logger
        );

        var result = await provider.GetHomesDataAsync("access-token", null, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Status, Is.EqualTo("ok"));
        Assert.That(result.Body, Is.Not.Null);
        Assert.That(result.Body.Homes, Is.Not.Null);
        Assert.That(result.Body.Homes.Count, Is.GreaterThanOrEqualTo(1));

        var home = result.Body.Homes[0];
        Assert.That(home.Id, Is.Not.Empty);
        Assert.That(home.Rooms, Is.Not.Null);
        Assert.That(home.Modules, Is.Not.Null);

        var roomWithModules = home.Rooms.Find(r => r.ModuleIds != null && r.ModuleIds.Count > 0);
        Assert.That(roomWithModules, Is.Not.Null);

        var moduleWithBridge = home.Modules.Find(m => !string.IsNullOrWhiteSpace(m.Bridge));
        Assert.That(moduleWithBridge, Is.Not.Null);
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
}
