using API.Auth.Netatmo.Interface;
using API.Auth.Netatmo.Services;
using API.Controllers.v1;

using Microsoft.AspNetCore.Mvc;
using Moq;
using Serilog;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherStation.Tests.Tests.Unit.ControllerTest;

[TestFixture]
public class NetatmoAuthControllerTest
{
    [Test]
    public void Test_Login_RedirectsToAuthorizeUrl()
    {
        Mock<INetatmoOAuthClient> oauthClientMock = new Mock<INetatmoOAuthClient>();
        oauthClientMock
            .Setup(c => c.BuildAuthorizeUrl(It.Is<string>(s => !string.IsNullOrWhiteSpace(s) && s.Length == 32)))
            .Returns("https://example.invalid/oauth2/authorize?state=test");

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        Mock<ILogger> loggerMock = new Mock<ILogger>();

        NetatmoAuthController controller = new NetatmoAuthController(
            oauthClientMock.Object,
            tokenStoreMock.Object,
            loggerMock.Object
        );

        ActionResult result = controller.Login();

        Assert.That(result, Is.InstanceOf<RedirectResult>());
        RedirectResult redirect = result as RedirectResult;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect.Url, Is.EqualTo("https://example.invalid/oauth2/authorize?state=test"));
        oauthClientMock.Verify(c => c.BuildAuthorizeUrl(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task Test_CallbackAsync_WhenCodeMissing_ReturnsBadRequest()
    {
        Mock<INetatmoOAuthClient> oauthClientMock = new Mock<INetatmoOAuthClient>();
        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        Mock<ILogger> loggerMock = new Mock<ILogger>();

        NetatmoAuthController controller = new NetatmoAuthController(
            oauthClientMock.Object,
            tokenStoreMock.Object,
            loggerMock.Object
        );

        ActionResult result = await controller.CallbackAsync(" ", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        BadRequestObjectResult badRequest = result as BadRequestObjectResult;
        Assert.That(badRequest, Is.Not.Null);
        Assert.That(badRequest.Value, Is.InstanceOf<ProblemDetails>());
        ProblemDetails details = badRequest.Value as ProblemDetails;
        Assert.That(details, Is.Not.Null);
        Assert.That(details.Detail, Is.EqualTo("The authorization code is missing or invalid."));
    }

    [Test]
    public async Task Test_CallbackAsync_WhenSuccessful_SavesTokensAndReturnsJson()
    {
        DateTime expiresAtUtc = new DateTime(2026, 2, 11, 12, 0, 0, DateTimeKind.Utc);
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "access",
            RefreshToken = "refresh",
            Scope = "read_station",
            TokenType = "bearer",
            ObtainedAtUtc = new DateTime(2026, 2, 11, 11, 0, 0, DateTimeKind.Utc),
            ExpiresAtUtc = expiresAtUtc
        };

        Mock<INetatmoOAuthClient> oauthClientMock = new Mock<INetatmoOAuthClient>();
        oauthClientMock
            .Setup(c => c.ExchangeCodeAsync("the-code", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenInfo);

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock
            .Setup(s => s.SaveAsync(tokenInfo, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<ILogger> loggerMock = new Mock<ILogger>();

        NetatmoAuthController controller = new NetatmoAuthController(
            oauthClientMock.Object,
            tokenStoreMock.Object,
            loggerMock.Object
        );

        ActionResult result = await controller.CallbackAsync("the-code", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        JsonResult jsonResult = result as JsonResult;
        Assert.That(jsonResult, Is.Not.Null);

        object value = jsonResult.Value;
        Assert.That(value, Is.Not.Null);

        PropertyInfo scopeProperty = value.GetType().GetProperty("scope", BindingFlags.Instance | BindingFlags.Public);
        PropertyInfo expiresAtProperty = value.GetType().GetProperty("expiresAtUtc", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(scopeProperty, Is.Not.Null);
        Assert.That(expiresAtProperty, Is.Not.Null);
        Assert.That(scopeProperty.GetValue(value), Is.EqualTo("read_station"));
        Assert.That(expiresAtProperty.GetValue(value), Is.EqualTo(expiresAtUtc));

        oauthClientMock.Verify(c => c.ExchangeCodeAsync("the-code", It.IsAny<CancellationToken>()), Times.Once);
        tokenStoreMock.Verify(s => s.SaveAsync(tokenInfo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Test_CallbackAsync_WhenExchangeFails_Returns502()
    {
        Mock<INetatmoOAuthClient> oauthClientMock = new Mock<INetatmoOAuthClient>();
        oauthClientMock
            .Setup(c => c.ExchangeCodeAsync("the-code", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fail"));

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        Mock<ILogger> loggerMock = new Mock<ILogger>();

        NetatmoAuthController controller = new NetatmoAuthController(
            oauthClientMock.Object,
            tokenStoreMock.Object,
            loggerMock.Object
        );

        ActionResult result = await controller.CallbackAsync("the-code", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        ObjectResult objectResult = result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult.StatusCode, Is.EqualTo(502));
        Assert.That(objectResult.Value, Is.InstanceOf<ProblemDetails>());
        ProblemDetails details = objectResult.Value as ProblemDetails;
        Assert.That(details, Is.Not.Null);
        Assert.That(details.Title, Is.EqualTo("Sign-in Failed"));

        loggerMock.Verify(
            x => x.Error(It.IsAny<Exception>(), It.Is<string>(s => s.Contains("Netatmo callback failed")), It.IsAny<string>()),
            Times.Once);
    }
}
