using API.Auth.Netatmo.Interface;
using API.Auth.Netatmo.Services;
using API.Controllers.v1;

using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Threading;
using WeatherStation.Tests.Tests;
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

        NetatmoController controller = new NetatmoController(oauthClient.Object, tokenStore.Object);

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

        NetatmoController controller = new NetatmoController(oauthClient.Object, tokenStore.Object);

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
    public void Test_Callback_MissingCode_ThrowsException()
    {
        Mock<INetatmoOAuthClient> oauthClient = new ServiceTestMockBuilder<INetatmoOAuthClient>.Builder()
            .SetupException(c => c.ExchangeCodeAsync(null, It.IsAny<CancellationToken>()), new ArgumentNullException("code"))
            .Build();
        Mock<INetatmoTokenStore> tokenStore = new ServiceTestMockBuilder<INetatmoTokenStore>.Builder()
            .Build();

        NetatmoController controller = new NetatmoController(oauthClient.Object, tokenStore.Object);

        Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.CallbackAsync(null, CancellationToken.None));
    }

    [Test]
    public void Test_Callback_TokenExchangeFails_ReturnsError()
    {
        const string code = "testcode";
        Mock<INetatmoOAuthClient> oauthClient = new ServiceTestMockBuilder<INetatmoOAuthClient>.Builder()
            .SetupException(c => c.ExchangeCodeAsync(code, It.IsAny<CancellationToken>()), new InvalidOperationException("Token exchange failed"))
            .Build();
        Mock<INetatmoTokenStore> tokenStore = new ServiceTestMockBuilder<INetatmoTokenStore>.Builder()
            .Build();

        NetatmoController controller = new NetatmoController(oauthClient.Object, tokenStore.Object);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await controller.CallbackAsync(code, CancellationToken.None));
    }
}
