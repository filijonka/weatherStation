using API.Auth.Netatmo.Options;
using API.Auth.Netatmo.Services;

using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Serilog;
using System.Net;
using System.Text;
using System.Text.Json;
using ItExpr = Moq.Protected.ItExpr;

namespace WeatherStation.Tests.Tests.Unit.AuthTest.Netatmo;

/// <summary>
/// Tests for Netatmo OAuth client.
/// </summary>
[TestFixture]
public class OAuthClientTest : TestBase
{
    private Mock<HttpMessageHandler> mockHttpMessageHandler;
    private HttpClient httpClient;
    private NetatmoOptions options;

    /// <summary>
    /// Sets up test dependencies.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        this.mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        this.httpClient = new HttpClient(this.mockHttpMessageHandler.Object);
        this.options = new NetatmoOptions
        {
            ClientId = "test-client-id",
            ClientSecret = "test-secret",
            RedirectUri = "http://localhost/callback",
            Scopes = "read_station",
            AuthorizeUrl = "https://api.netatmo.com/oauth2/authorize",
            TokenUrl = "https://api.netatmo.com/oauth2/token"
        };
    }

    /// <summary>
    /// Tears down test dependencies.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        this.httpClient?.Dispose();
    }

    /// <summary>
    /// Build authorize URL should contain all parameters.
    /// </summary>
    [Test]
    public void Test_BuildAuthorizeUrl_ContainsAllParameters()
    {
        IOptions<NetatmoOptions> optionsWrapper = Options.Create(this.options);
        ILogger logger = new LoggerConfiguration().CreateLogger();
        NetatmoOAuthClient client = new NetatmoOAuthClient(this.httpClient, optionsWrapper, logger);

        string state = "test-state-123";
        string url = client.BuildAuthorizeUrl(state);

        Assert.That(url, Does.Contain("client_id=test-client-id"));
        Assert.That(url, Does.Contain("redirect_uri="));
        Assert.That(url, Does.Contain("scope=read_station"));
        Assert.That(url, Does.Contain("response_type=code"));
        Assert.That(url, Does.Contain($"state={state}"));
        Assert.That(url, Does.StartWith("https://api.netatmo.com/oauth2/authorize?"));
    }

    /// <summary>
    /// Build authorize URL should normalize scopes.
    /// </summary>
    [Test]
    public void Test_BuildAuthorizeUrl_NormalizesScopes()
    {
        this.options.Scopes = "read_station,read_thermostat";
        IOptions<NetatmoOptions> optionsWrapper = Options.Create(this.options);
        Mock<ILogger> logger = new Mock<ILogger>();
        logger.Setup(l => l.ForContext<NetatmoOAuthClient>()).Returns(logger.Object);
        NetatmoOAuthClient client = new NetatmoOAuthClient(this.httpClient, optionsWrapper, logger.Object);

        string url = client.BuildAuthorizeUrl("state");

        Assert.That(url, Does.Contain("scope=read_station%20read_thermostat"));
    }

    /// <summary>
    /// Exchange code should return token info.
    /// </summary>
    [Test]
    public async Task Test_ExchangeCode_ReturnsTokenInfo()
    {
        string responseJson = JsonSerializer.Serialize(new
        {
            access_token = "test-access-token",
            refresh_token = "test-refresh-token",
            scope = "read_station",
            token_type = "bearer",
            expires_in = 3600
        });

        HttpResponseMessage httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        this.mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        IOptions<NetatmoOptions> optionsWrapper = Options.Create(this.options);
        Mock<ILogger> logger = new Mock<ILogger>();
        logger.Setup(l => l.ForContext<NetatmoOAuthClient>()).Returns(logger.Object);

        NetatmoOAuthClient client = new NetatmoOAuthClient(this.httpClient, optionsWrapper, logger.Object);

        NetatmoTokenInfo result = await client.ExchangeCodeAsync("test-code", CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.AccessToken, Is.EqualTo("test-access-token"));
        Assert.That(result.RefreshToken, Is.EqualTo("test-refresh-token"));
        Assert.That(result.Scope, Is.EqualTo("read_station"));
        Assert.That(result.TokenType, Is.EqualTo("bearer"));
    }

    /// <summary>
    /// Exchange code should throw exception on bad request.
    /// </summary>
    [Test]
    public void Test_ExchangeCode_BadRequest_ThrowsException()
    {
        HttpResponseMessage httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent("{\"error\":\"invalid_grant\"}")
        };

        this.mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        IOptions<NetatmoOptions> optionsWrapper = Options.Create(this.options);
        ILogger logger = new LoggerConfiguration().CreateLogger();
        NetatmoOAuthClient client = new NetatmoOAuthClient(this.httpClient, optionsWrapper, logger);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await client.ExchangeCodeAsync("test-code", CancellationToken.None));
    }

    /// <summary>
    /// Exchange code should throw exception on invalid response.
    /// </summary>
    [Test]
    public void Test_ExchangeCode_InvalidResponse_ThrowsException()
    {
        HttpResponseMessage httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"invalid\":\"json\"}")
        };

        this.mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        IOptions<NetatmoOptions> optionsWrapper = Options.Create(this.options);
        Mock<ILogger> logger = new Mock<ILogger>();
        logger.Setup(l => l.ForContext<NetatmoOAuthClient>()).Returns(logger.Object);

        NetatmoOAuthClient client = new NetatmoOAuthClient(this.httpClient, optionsWrapper, logger.Object);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await client.ExchangeCodeAsync("test-code", CancellationToken.None));
    }

    /// <summary>
    /// Refresh should return new token info.
    /// </summary>
    [Test]
    public async Task Test_Refresh_ReturnsNewTokenInfo()
    {
        string responseJson = JsonSerializer.Serialize(new
        {
            access_token = "new-access-token",
            refresh_token = "new-refresh-token",
            scope = "read_station",
            token_type = "bearer",
            expires_in = 3600
        });

        HttpResponseMessage httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        this.mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        IOptions<NetatmoOptions> optionsWrapper = Options.Create(this.options);
        Mock<ILogger> logger = new Mock<ILogger>();
        logger.Setup(l => l.ForContext<NetatmoOAuthClient>()).Returns(logger.Object);

        NetatmoOAuthClient client = new NetatmoOAuthClient(this.httpClient, optionsWrapper, logger.Object);

        NetatmoTokenInfo result = await client.RefreshAsync("old-refresh-token", CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.AccessToken, Is.EqualTo("new-access-token"));
            Assert.That(result.RefreshToken, Is.EqualTo("new-refresh-token"));
        });
    }

    /// <summary>
    /// Refresh should throw exception on expired refresh token.
    /// </summary>
    [Test]
    public void Test_Refresh_ExpiredRefreshToken_ThrowsException()
    {
        HttpResponseMessage httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent("{\"error\":\"invalid_grant\"}")
        };

        this.mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        IOptions<NetatmoOptions> optionsWrapper = Options.Create(this.options);
        Mock<ILogger> logger = new Mock<ILogger>();
        logger.Setup(l => l.ForContext<NetatmoOAuthClient>()).Returns(logger.Object);

        NetatmoOAuthClient client = new NetatmoOAuthClient(this.httpClient, optionsWrapper, logger.Object);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await client.RefreshAsync("expired-token", CancellationToken.None));
    }
}
