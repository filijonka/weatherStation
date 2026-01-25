using API.Auth.Netatmo.Interface;
using API.Auth.Netatmo.Options;
using API.Auth.Netatmo.Services;
using API.Controllers.v1;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using It = Moq.It;

namespace WeatherStation.Tests.Tests.Unit.ControllerTest;

/// <summary>
/// Tests for Netatmo controller.
/// </summary>
[TestFixture]
public class NetatmoControllerTest
{
    [Test]
    public void Test_Login_RedirectsToAuthorizeUrl()
    {
        const string expectedUrl = "https://api.netatmo.com/oauth2/authorize?client_id=test&redirect_uri=callback&scope=read_station&response_type=code&state=teststate";
        Mock<INetatmoOAuthClient> oauthClient = new ServiceTestMockBuilder<INetatmoOAuthClient>.Builder()
            .Setup(c => c.BuildAuthorizeUrl(It.IsAny<string>()), expectedUrl)
            .Build();
        Mock<INetatmoTokenStore> tokenStore = new ServiceTestMockBuilder<INetatmoTokenStore>.Builder()
            .Build();

        NetatmoController controller = new NetatmoController(oauthClient.Object, tokenStore.Object, Options.Create(new NetatmoOptions()), new Mock<ILogger>().Object);

        ActionResult result = controller.Login();

        Assert.That(result, Is.InstanceOf<RedirectResult>());
        RedirectResult redirectResult = result as RedirectResult;
        Assert.That(redirectResult, Is.Not.Null);
        Assert.That(redirectResult.Url, Is.EqualTo(expectedUrl));
    }

    [Test]
    public async Task Test_Callback_ExchangesCodeAndSavesTokens()
    {
        const string code = "testcode";
        DateTime expiresAt = DateTime.UtcNow.AddHours(3);
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "access",
            RefreshToken = "refresh",
            Scope = "read_station",
            TokenType = "bearer",
            ObtainedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAt
        };

        Mock<INetatmoOAuthClient> oauthClient = new ServiceTestMockBuilder<INetatmoOAuthClient>.Builder()
            .Setup(c => c.ExchangeCodeAsync(code, It.IsAny<CancellationToken>()), Task.FromResult(tokenInfo))
            .Build();
        Mock<INetatmoTokenStore> tokenStore = new ServiceTestMockBuilder<INetatmoTokenStore>.Builder()
            .SetupVoid(s => s.SaveAsync(tokenInfo, It.IsAny<CancellationToken>()))
            .Build();

        NetatmoController controller = new NetatmoController(oauthClient.Object, tokenStore.Object, Options.Create(new NetatmoOptions()), new Mock<ILogger>().Object);

        ActionResult<NetatmoController.NetatmoCallbackResponse> result = await controller.CallbackAsync(code, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        OkObjectResult okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        NetatmoController.NetatmoCallbackResponse response = okResult.Value as NetatmoController.NetatmoCallbackResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Scope, Is.EqualTo("read_station"));
        Assert.That(response.ExpiresAtUtc, Is.EqualTo(expiresAt));
        tokenStore.Verify(s => s.SaveAsync(tokenInfo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Test_Callback_MissingCode_ReturnsBadRequest()
    {
        Mock<INetatmoOAuthClient> oauthClient = new ServiceTestMockBuilder<INetatmoOAuthClient>.Builder()
            .Build();
        Mock<INetatmoTokenStore> tokenStore = new ServiceTestMockBuilder<INetatmoTokenStore>.Builder()
            .Build();

        NetatmoController controller = new NetatmoController(oauthClient.Object, tokenStore.Object, Options.Create(new NetatmoOptions()), new Mock<ILogger>().Object);

        ActionResult<NetatmoController.NetatmoCallbackResponse> result = await controller.CallbackAsync(string.Empty, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        BadRequestObjectResult badRequest = result.Result as BadRequestObjectResult;
        Assert.That(badRequest, Is.Not.Null);
        Assert.That(badRequest.Value, Is.InstanceOf<ProblemDetails>());
        ProblemDetails problem = badRequest.Value as ProblemDetails;
        Assert.That(problem, Is.Not.Null);
        Assert.That(problem.Detail, Does.Contain("authorization code"));
    }

    [Test]
    public async Task Test_Callback_TokenExchangeFails_Returns502()
    {
        const string code = "testcode";
        Mock<INetatmoOAuthClient> oauthClient = new ServiceTestMockBuilder<INetatmoOAuthClient>.Builder()
            .SetupException(c => c.ExchangeCodeAsync(
                code, 
                It.IsAny<CancellationToken>()), 
                new InvalidOperationException("Token exchange failed")
            )
            .Build();
        Mock<INetatmoTokenStore> tokenStore = new ServiceTestMockBuilder<INetatmoTokenStore>.Builder()
            .Build();

        NetatmoController controller = new NetatmoController(oauthClient.Object, tokenStore.Object, Options.Create(new NetatmoOptions()), new Mock<ILogger>().Object);

        ActionResult<NetatmoController.NetatmoCallbackResponse> result = await controller.CallbackAsync(code, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        ObjectResult objectResult = result.Result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult.StatusCode, Is.EqualTo(502));
        Assert.That(objectResult.Value, Is.InstanceOf<ProblemDetails>());
        ProblemDetails problem = objectResult.Value as ProblemDetails;
        Assert.That(problem, Is.Not.Null);
        Assert.That(problem.Title, Is.EqualTo("Sign-in Failed"));
    }

    [Test]
    public async Task Test_Status_NotAuthenticated_ReturnsLoginUrl()
    {
        Mock<INetatmoOAuthClient> oauthClient = new ServiceTestMockBuilder<INetatmoOAuthClient>.Builder().Build();
        Mock<INetatmoTokenStore> tokenStore = new ServiceTestMockBuilder<INetatmoTokenStore>.Builder()
            .Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()), Task.FromResult<NetatmoTokenInfo>(null!))
            .Build();
        NetatmoOptions options = new NetatmoOptions { ApiBaseUrl = "http://localhost:8080" };

        NetatmoController controller = new NetatmoController(oauthClient.Object, tokenStore.Object, Options.Create(options), new Mock<ILogger>().Object);

        ActionResult<NetatmoController.NetatmoStatusResponse> result = await controller.StatusAsync(CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        NetatmoController.NetatmoStatusResponse status = (result.Result as OkObjectResult)!.Value as NetatmoController.NetatmoStatusResponse;
        Assert.That(status, Is.Not.Null);
        Assert.That(status.Authenticated, Is.False);
        Assert.That(status.Status, Is.EqualTo("Login required"));
        Assert.That(status.LoginUrl, Is.EqualTo("http://localhost:8080/api/v1/netatmo/login"));
        Assert.That(status.ExpiresAtUtc, Is.Null);
    }

    [Test]
    public async Task Test_Status_Authenticated_ReturnsConnected()
    {
        DateTime expiresAt = DateTime.UtcNow.AddHours(1);
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "a",
            RefreshToken = "r",
            Scope = "read_station",
            TokenType = "bearer",
            ObtainedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAt
        };
        Mock<INetatmoOAuthClient> oauthClient = new ServiceTestMockBuilder<INetatmoOAuthClient>.Builder().Build();
        Mock<INetatmoTokenStore> tokenStore = new ServiceTestMockBuilder<INetatmoTokenStore>.Builder()
            .Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()), Task.FromResult(tokenInfo))
            .Build();

        NetatmoController controller = new NetatmoController(oauthClient.Object, tokenStore.Object, Options.Create(new NetatmoOptions()), new Mock<ILogger>().Object);

        ActionResult<NetatmoController.NetatmoStatusResponse> result = await controller.StatusAsync(CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        NetatmoController.NetatmoStatusResponse status = (result.Result as OkObjectResult)!.Value as NetatmoController.NetatmoStatusResponse;
        Assert.That(status, Is.Not.Null);
        Assert.That(status.Authenticated, Is.True);
        Assert.That(status.Status, Is.EqualTo("Connected"));
        Assert.That(status.LoginUrl, Is.Null);
        Assert.That(status.ExpiresAtUtc, Is.EqualTo(expiresAt));
    }
}
