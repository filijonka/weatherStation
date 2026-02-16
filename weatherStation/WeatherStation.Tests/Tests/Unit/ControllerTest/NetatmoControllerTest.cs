using API.Auth.Netatmo.Interface;
using API.Auth.Netatmo.Services;
using API.Controllers.v1;
using API.Interface.Logic;
using API.Responses;
using Application.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Serilog;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherStation.Tests.Tests.Unit.ControllerTest;

/// <summary>
/// Tests for Netatmo controller.
/// </summary>
[TestFixture]
public class NetatmoControllerTest
{
    /// <summary>
    /// Login should redirect to the authorize URL.
    /// </summary>
    [Test]
    public void Test_Login_RedirectsToAuthorizeUrl()
    {
        // Arrange
        Mock<INetatmoOAuthClient> oauthClientMock = new Mock<INetatmoOAuthClient>();
        oauthClientMock
            .Setup(c => c.BuildAuthorizeUrl(It.Is<string>(s => !string.IsNullOrWhiteSpace(s) && s.Length == 32)))
            .Returns("https://example.invalid/oauth2/authorize?state=test");

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        Mock<ILogger> loggerMock = new Mock<ILogger>();

        NetatmoController controller = new NetatmoController(
            oauthClientMock.Object,
            tokenStoreMock.Object,
            logicDataProviderMock.Object,
            loggerMock.Object
        );

        // Act
        ActionResult result = controller.Login();

        // Assert
        Assert.That(result, Is.InstanceOf<RedirectResult>());
        RedirectResult redirect = result as RedirectResult;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect.Url, Is.EqualTo("https://example.invalid/oauth2/authorize?state=test"));
        oauthClientMock.Verify(c => c.BuildAuthorizeUrl(It.IsAny<string>()), Times.Once);
    }

    /// <summary>
    /// GetHomesDataAsync should return 401 when not authenticated.
    /// </summary>
    [Test]
    public async Task Test_GetHomesDataAsync_WhenNotAuthenticated_Returns401WithApiResponse()
    {
        // Arrange
        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((NetatmoTokenInfo)null);

        Mock<INetatmoOAuthClient> oauthClientMock = new Mock<INetatmoOAuthClient>();
        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        Mock<ILogger> loggerMock = new Mock<ILogger>();

        NetatmoController controller = new NetatmoController(
            oauthClientMock.Object,
            tokenStoreMock.Object,
            logicDataProviderMock.Object,
            loggerMock.Object
        );

        // Setup HttpContext
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Scheme = "https",
                    Host = new HostString("localhost:8080")
                }
            }
        };

        // Act
        ActionResult<ApiResponse<JsonHome>> result = await controller.GetHomesDataAsync(null, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<UnauthorizedObjectResult>());
        UnauthorizedObjectResult unauthorizedResult = result.Result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult, Is.Not.Null);
        Assert.That(unauthorizedResult.Value, Is.InstanceOf<ApiResponse<JsonHome>>());

        ApiResponse<JsonHome> response = unauthorizedResult.Value as ApiResponse<JsonHome>;
        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response.IsSuccess, Is.False);
            Assert.That(response.IsAuthenticated, Is.False);
            Assert.That(response.LoginUrl, Does.Contain("/api/v1/netatmo/auth/login"));
            Assert.That(response.Data, Is.Not.Null);
        });
        logicDataProviderMock.Verify(d => d.GetHomesDataAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// GetHomesDataAsync should return 502 when authenticated but payload is empty.
    /// </summary>
    [Test]
    public async Task Test_GetHomesDataAsync_WhenAuthenticatedButNoData_Returns502()
    {
        // Arrange
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "test-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenInfo);

        JsonHome emptyJson = new JsonHome();

        Mock<INetatmoOAuthClient> oauthClientMock = new Mock<INetatmoOAuthClient>();
        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        logicDataProviderMock.Setup(d => d.GetHomesDataAsync("test-token", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyJson);
        Mock<ILogger> loggerMock = new Mock<ILogger>();

        NetatmoController controller = new NetatmoController(
            oauthClientMock.Object,
            tokenStoreMock.Object,
            logicDataProviderMock.Object,
            loggerMock.Object
        );

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Scheme = "https",
                    Host = new HostString("localhost:8080")
                }
            }
        };

        // Act
        ActionResult<ApiResponse<JsonHome>> result = await controller.GetHomesDataAsync(null, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        ObjectResult objectResult = result.Result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult.StatusCode, Is.EqualTo(502));
        Assert.That(objectResult.Value, Is.InstanceOf<ApiResponse<JsonHome>>());
        ApiResponse<JsonHome> response = objectResult.Value as ApiResponse<JsonHome>;
        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response.IsSuccess, Is.False);
            Assert.That(response.Error, Is.EqualTo("No home data was returned. Please try again later."));
            Assert.That(response.Data, Is.Not.Null);
        });
    }

    /// <summary>
    /// GetHomesDataAsync should return JSON data when authenticated.
    /// </summary>
    [Test]
    public async Task Test_GetHomesDataAsync_WhenAuthenticated_ReturnsJsonData()
    {
        // Arrange
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "test-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenInfo);

        JsonHome jsonData = new JsonHome
        {
            Body = new Body
            {
                Homes = new System.Collections.Generic.List<Home> { new Home { Id = "home-1", Name = "Home" } }
            }
        };

        Mock<INetatmoOAuthClient> oauthClientMock = new Mock<INetatmoOAuthClient>();
        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        logicDataProviderMock.Setup(d => d.GetHomesDataAsync("test-token", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonData);
        Mock<ILogger> loggerMock = new Mock<ILogger>();

        NetatmoController controller = new NetatmoController(
            oauthClientMock.Object,
            tokenStoreMock.Object,
            logicDataProviderMock.Object,
            loggerMock.Object
        );

        // Setup HttpContext
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Scheme = "https",
                    Host = new HostString("localhost:8080")
                }
            }
        };

        // Act
        ActionResult<ApiResponse<JsonHome>> result = await controller.GetHomesDataAsync(null, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        OkObjectResult okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.Value, Is.InstanceOf<ApiResponse<JsonHome>>());
        ApiResponse<JsonHome> response = okResult.Value as ApiResponse<JsonHome>;
        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response.IsSuccess, Is.True);
            Assert.That(response.IsAuthenticated, Is.True);
            Assert.That(response.Data, Is.SameAs(jsonData));
            Assert.That(response.Error, Is.EqualTo(string.Empty));
        });
        logicDataProviderMock.Verify(d => d.GetHomesDataAsync("test-token", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetHomesDataAsync should parse and pass gatewayTypes parameter.
    /// </summary>
    [Test]
    public async Task Test_GetHomesDataAsync_WithGatewayTypes_ParsesAndPassesFilter()
    {
        // Arrange
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "test-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenInfo);

        JsonHome jsonData = new JsonHome
        {
            Body = new Body
            {
                Homes = new System.Collections.Generic.List<Home> { new Home { Id = "home-1", Name = "Home" } }
            }
        };

        Mock<INetatmoOAuthClient> oauthClientMock = new Mock<INetatmoOAuthClient>();
        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        logicDataProviderMock.Setup(d => d.GetHomesDataAsync(
            "test-token",
            It.Is<string[]>(arr => arr != null && arr.Length == 2 && arr[0] == "NLG" && arr[1] == "OTH"),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonData);
        Mock<ILogger> loggerMock = new Mock<ILogger>();

        NetatmoController controller = new NetatmoController(
            oauthClientMock.Object,
            tokenStoreMock.Object,
            logicDataProviderMock.Object,
            loggerMock.Object
        );

        // Setup HttpContext
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Scheme = "https",
                    Host = new HostString("localhost:8080")
                }
            }
        };

        // Act
        await controller.GetHomesDataAsync("NLG, OTH", CancellationToken.None);

        // Assert
        logicDataProviderMock.Verify(d => d.GetHomesDataAsync(
            "test-token",
            It.Is<string[]>(arr => arr != null && arr.Length == 2 && arr[0] == "NLG" && arr[1] == "OTH"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetHomesDataAsync should handle empty gatewayTypes gracefully.
    /// </summary>
    [Test]
    public async Task Test_GetHomesDataAsync_WithEmptyGatewayTypes_PassesNull()
    {
        // Arrange
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "test-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenInfo);

        JsonHome jsonData = new JsonHome
        {
            Body = new Body
            {
                Homes = new System.Collections.Generic.List<Home> { new Home { Id = "home-1", Name = "Home" } }
            }
        };

        Mock<INetatmoOAuthClient> oauthClientMock = new Mock<INetatmoOAuthClient>();
        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        logicDataProviderMock.Setup(d => d.GetHomesDataAsync("test-token", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonData);
        Mock<ILogger> loggerMock = new Mock<ILogger>();

        NetatmoController controller = new NetatmoController(
            oauthClientMock.Object,
            tokenStoreMock.Object,
            logicDataProviderMock.Object,
            loggerMock.Object
        );

        // Setup HttpContext
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Scheme = "https",
                    Host = new HostString("localhost:8080")
                }
            }
        };

        // Act
        await controller.GetHomesDataAsync("", CancellationToken.None);

        // Assert
        logicDataProviderMock.Verify(d => d.GetHomesDataAsync("test-token", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetHomesDataAsync should return 502 when data provider throws exception.
    /// </summary>
    [Test]
    public async Task Test_GetHomesDataAsync_WhenDataProviderThrows_Returns502()
    {
        // Arrange
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "test-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenInfo);

        Mock<INetatmoOAuthClient> oauthClientMock = new Mock<INetatmoOAuthClient>();
        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        logicDataProviderMock.Setup(d => d.GetHomesDataAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API error"));
        Mock<ILogger> loggerMock = new Mock<ILogger>();

        NetatmoController controller = new NetatmoController(
            oauthClientMock.Object,
            tokenStoreMock.Object,
            logicDataProviderMock.Object,
            loggerMock.Object
        );

        // Setup HttpContext
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Scheme = "https",
                    Host = new HostString("localhost:8080")
                }
            }
        };

        // Act
        ActionResult<ApiResponse<JsonHome>> result = await controller.GetHomesDataAsync(null, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        ObjectResult objectResult = result.Result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult.StatusCode, Is.EqualTo(502));
        Assert.That(objectResult.Value, Is.InstanceOf<ApiResponse<JsonHome>>());
        ApiResponse<JsonHome> response = objectResult.Value as ApiResponse<JsonHome>;
        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response.IsSuccess, Is.False);
            Assert.That(response.IsAuthenticated, Is.True);
            Assert.That(response.Error, Is.EqualTo("Failed to retrieve home data from Netatmo. Please try again later."));
        });
        loggerMock.Verify(
            x => x.Error(It.IsAny<Exception>(), It.Is<string>(s => s.Contains("Failed to fetch Netatmo homesdata")), It.IsAny<string>()),
            Times.Once);
    }

    /// <summary>
    /// CallbackAsync should return bad request when code is missing.
    /// </summary>
    [Test]
    public async Task Test_CallbackAsync_WhenCodeMissing_ReturnsBadRequest()
    {
        // Arrange
        Mock<INetatmoOAuthClient> oauthClientMock = new Mock<INetatmoOAuthClient>();
        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        Mock<ILogger> loggerMock = new Mock<ILogger>();

        NetatmoController controller = new NetatmoController(
            oauthClientMock.Object,
            tokenStoreMock.Object,
            logicDataProviderMock.Object,
            loggerMock.Object
        );

        // Act
        ActionResult result = await controller.CallbackAsync(" ", CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        BadRequestObjectResult badRequest = result as BadRequestObjectResult;
        Assert.That(badRequest, Is.Not.Null);
        Assert.That(badRequest.Value, Is.InstanceOf<ProblemDetails>());
        ProblemDetails details = badRequest.Value as ProblemDetails;
        Assert.That(details, Is.Not.Null);
        Assert.That(details.Detail, Is.EqualTo("The authorization code is missing or invalid."));
    }

    /// <summary>
    /// CallbackAsync should exchange code and persist tokens.
    /// </summary>
    [Test]
    public async Task Test_CallbackAsync_WhenSuccessful_SavesTokensAndReturnsJson()
    {
        // Arrange
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

        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        Mock<ILogger> loggerMock = new Mock<ILogger>();

        NetatmoController controller = new NetatmoController(
            oauthClientMock.Object,
            tokenStoreMock.Object,
            logicDataProviderMock.Object,
            loggerMock.Object
        );

        // Act
        ActionResult result = await controller.CallbackAsync("the-code", CancellationToken.None);

        // Assert
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

    /// <summary>
    /// CallbackAsync should return 502 when the OAuth exchange fails.
    /// </summary>
    [Test]
    public async Task Test_CallbackAsync_WhenExchangeFails_Returns502()
    {
        // Arrange
        Mock<INetatmoOAuthClient> oauthClientMock = new Mock<INetatmoOAuthClient>();
        oauthClientMock
            .Setup(c => c.ExchangeCodeAsync("the-code", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fail"));

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        Mock<ILogger> loggerMock = new Mock<ILogger>();

        NetatmoController controller = new NetatmoController(
            oauthClientMock.Object,
            tokenStoreMock.Object,
            logicDataProviderMock.Object,
            loggerMock.Object
        );

        // Act
        ActionResult result = await controller.CallbackAsync("the-code", CancellationToken.None);

        // Assert
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
