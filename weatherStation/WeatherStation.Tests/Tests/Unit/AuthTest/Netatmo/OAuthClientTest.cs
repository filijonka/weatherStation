using API.Auth.Netatmo.Options;
using API.Auth.Netatmo.Services;
using ItExpr = Moq.Protected.ItExpr;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Serilog;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using API.Auth.Netatmo.Interface;
using API.Responses;
using WeatherStation.Tests.Tests;

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
    /// Exchange code should return token info.
    /// </summary>
    [Test]
    public async Task Test_ExchangeCode_ReturnsTokenInfo()
    {
        string responseJson = JsonSerializer.Serialize(new
        {
            access_token = "test-access-token",
            refresh_token = "test-refresh-token",
            scope = new[] { "read_station" },
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
        Mock<INetatmoTokenStore> netatmoTokenStore = new Mock<INetatmoTokenStore>();

        NetatmoOAuthClient client =
            new NetatmoOAuthClient(this.httpClient, optionsWrapper, netatmoTokenStore.Object, logger.Object);

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
        Mock<INetatmoTokenStore> netatmoTokenStore = new Mock<INetatmoTokenStore>();

        NetatmoOAuthClient client =
            new NetatmoOAuthClient(this.httpClient, optionsWrapper, netatmoTokenStore.Object, logger);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await client.ExchangeCodeAsync("test-code", CancellationToken.None));
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
        Mock<INetatmoTokenStore> netatmoTokenStore = new Mock<INetatmoTokenStore>();

        NetatmoOAuthClient client =
            new NetatmoOAuthClient(this.httpClient, optionsWrapper, netatmoTokenStore.Object, logger.Object);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await client.ExchangeCodeAsync("test-code", CancellationToken.None));
    }

    [Test]
    public async Task Login_NoToken_ReturnsLoginUrlAndUnauthenticated()
    {
        Mock<INetatmoTokenStore> tokenStore = new Mock<INetatmoTokenStore>();
        tokenStore.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync((NetatmoTokenInfo)null);
        Mock<ILogger> logger = new Mock<ILogger>();
        logger.Setup(l => l.ForContext<NetatmoOAuthClient>()).Returns(logger.Object);
        NetatmoOAuthClient client = new NetatmoOAuthClient(this.httpClient, Options.Create(this.options),
            tokenStore.Object, logger.Object);
        NetatmoAuthStatusResponse result = await client.Login(CancellationToken.None);
        Assert.That(result.Authenticated, Is.False);
        Assert.That(result.LoginUrl, Does.Contain("authorize"));
        Assert.That(result.Status, Is.EqualTo("Login required"));
        Assert.That(result.StatusCode, Is.EqualTo(401));
    }

    [Test]
    public async Task Login_ValidToken_ReturnsAuthenticated()
    {
        DateTime now = DateTime.UtcNow;
        NetatmoTokenInfo token = new NetatmoTokenInfo
        {
            AccessToken = "valid-token",
            RefreshToken = "refresh-token",
            ExpiresAtUtc = now.AddMinutes(10)
        };
        Mock<INetatmoTokenStore> tokenStore = new Mock<INetatmoTokenStore>();
        tokenStore.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(token);
        Mock<ILogger> logger = new Mock<ILogger>();
        logger.Setup(l => l.ForContext<NetatmoOAuthClient>()).Returns(logger.Object);
        NetatmoOAuthClient client = new NetatmoOAuthClient(this.httpClient, Options.Create(this.options),
            tokenStore.Object, logger.Object);
        NetatmoAuthStatusResponse result = await client.Login(CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(result.Authenticated, Is.True);
            Assert.That(result.Token, Is.EqualTo("valid-token"));
            Assert.That(result.Status, Is.EqualTo("Connected"));
            Assert.That(result.StatusCode, Is.EqualTo(200));
        });
    }

    [Test]
    public async Task Login_ExpiredToken_RefreshSucceeds_ReturnsAuthenticatedAndPersistsToken()
    {
        NetatmoTokenInfo expiredToken = new NetatmoTokenInfo
        {
            AccessToken = "expired-token",
            RefreshToken = "refresh-token",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-10)
        };
        Mock<INetatmoTokenStore> tokenStore = new Mock<INetatmoTokenStore>();
        tokenStore.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expiredToken);
        tokenStore.Setup(x => x.SaveAsync(It.IsAny<NetatmoTokenInfo>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask).Verifiable();
        Mock<ILogger> logger = new Mock<ILogger>();
        logger.Setup(l => l.ForContext<NetatmoOAuthClient>()).Returns(logger.Object);
        string responseJson = JsonSerializer.Serialize(new
        {
            access_token = "new-token",
            refresh_token = "new-refresh-token",
            scope = new[] { "read_station" },
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
        NetatmoOAuthClient client = new NetatmoOAuthClient(this.httpClient, Options.Create(this.options),
            tokenStore.Object, logger.Object);
        NetatmoAuthStatusResponse result = await client.Login(CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(result.Authenticated, Is.True);
            Assert.That(result.Token, Is.EqualTo("new-token"));
            Assert.That(result.Status, Is.EqualTo("Connected"));
            Assert.That(result.StatusCode, Is.EqualTo(200));
        });
        tokenStore.Verify(
            x => x.SaveAsync(It.Is<NetatmoTokenInfo>(t => t.AccessToken == "new-token"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Login_ExpiredToken_RefreshFails_ReturnsLoginUrlAndUnauthenticated()
    {
        NetatmoTokenInfo expiredToken = new NetatmoTokenInfo
        {
            AccessToken = "expired-token",
            RefreshToken = "refresh-token",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-10)
        };
        Mock<INetatmoTokenStore> tokenStore = new Mock<INetatmoTokenStore>();
        tokenStore.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expiredToken);
        Mock<ILogger> logger = new Mock<ILogger>();
        logger.Setup(l => l.ForContext<NetatmoOAuthClient>()).Returns(logger.Object);
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
        NetatmoOAuthClient client = new NetatmoOAuthClient(this.httpClient, Options.Create(this.options),
            tokenStore.Object, logger.Object);
        NetatmoAuthStatusResponse result = await client.Login(CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(result.Authenticated, Is.False);
            Assert.That(result.LoginUrl, Does.Contain("authorize"));
            Assert.That(result.Status, Is.EqualTo("Login required"));
            Assert.That(result.StatusCode, Is.EqualTo(401));
        });
    }

    [Test]
    public async Task Login_ExpiredToken_RefreshReturnsNull_ReturnsLoginUrlAndUnauthenticated()
    {
        NetatmoTokenInfo expiredToken = new NetatmoTokenInfo
        {
            AccessToken = "expired-token",
            RefreshToken = "refresh-token",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-10)
        };
        Mock<INetatmoTokenStore> tokenStore = new Mock<INetatmoTokenStore>();
        tokenStore.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expiredToken);
        Mock<ILogger> logger = new Mock<ILogger>();
        logger.Setup(l => l.ForContext<NetatmoOAuthClient>()).Returns(logger.Object);
        this.mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"invalid\":\"json\"}")
            });
        NetatmoOAuthClient client = new NetatmoOAuthClient(this.httpClient, Options.Create(this.options),
            tokenStore.Object, logger.Object);
        NetatmoAuthStatusResponse result = await client.Login(CancellationToken.None);
        Assert.That(result.Authenticated, Is.False);
        Assert.That(result.LoginUrl, Does.Contain("authorize"));
        Assert.That(result.Status, Is.EqualTo("Login required"));
        Assert.That(result.StatusCode, Is.EqualTo(401));
    }
}