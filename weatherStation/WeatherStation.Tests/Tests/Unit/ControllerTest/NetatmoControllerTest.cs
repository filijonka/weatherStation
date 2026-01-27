using API.Auth.Netatmo.Interface;
using API.Auth.Netatmo.Services;
using API.Controllers.v1;
using API.Interface.Logic;
using API.Responses;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Serilog;
using System;
using System.Text.Json;
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
    /// GetHomesDataAsync should return auth status when not authenticated.
    /// </summary>
    [Test]
    public async Task Test_GetHomesDataAsync_WhenNotAuthenticated_ReturnsAuthStatus()
    {
        // Arrange
        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((NetatmoTokenInfo?)null);

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
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
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
        ActionResult<NetatmoAuthStatusResponse> result = await controller.GetHomesDataAsync(null, CancellationToken.None);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value.Authenticated, Is.False);
        Assert.That(result.Value.StatusCode, Is.EqualTo(401));
        logicDataProviderMock.Verify(d => d.GetHomesDataAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Never);
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

        JsonElement jsonData = JsonDocument.Parse("{\"homes\": []}").RootElement;

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
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
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
        ActionResult<NetatmoAuthStatusResponse> result = await controller.GetHomesDataAsync(null, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<JsonResult>());
        JsonResult jsonResult = result.Result as JsonResult;
        Assert.That(jsonResult, Is.Not.Null);
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

        JsonElement jsonData = JsonDocument.Parse("{\"homes\": []}").RootElement;

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
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
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

        JsonElement jsonData = JsonDocument.Parse("{\"homes\": []}").RootElement;

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
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
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
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
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
        ActionResult<NetatmoAuthStatusResponse> result = await controller.GetHomesDataAsync(null, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        ObjectResult objectResult = result.Result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult.StatusCode, Is.EqualTo(502));
        Assert.That(objectResult.Value, Is.InstanceOf<ProblemDetails>());
        ProblemDetails problemDetails = objectResult.Value as ProblemDetails;
        Assert.That(problemDetails.Title, Is.EqualTo("Netatmo API Error"));
        loggerMock.Verify(
            x => x.Error(It.IsAny<Exception>(), It.Is<string>(s => s.Contains("Failed to fetch Netatmo homesdata")), It.IsAny<string>()),
            Times.Once);
    }
}
