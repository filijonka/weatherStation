using API.Auth.Netatmo.Interface;
using API.Auth.Netatmo.Options;
using API.Auth.Netatmo.Services;
using API.Controllers.v1;
using API.Responses;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherStation.Tests.Tests.Unit.ControllerTest;

/// <summary>
/// Tests for health controller.
/// </summary>
[TestFixture]
public class HealthControllerTest
{
    [Test]
    public void Test_GetHealth_ReturnsHealthy()
    {
        HealthController controller = new HealthController(
            new Mock<INetatmoTokenStore>().Object,
            new Mock<IOptions<NetatmoOptions>>().Object
        );

        ActionResult result = controller.GetHealth();
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        OkObjectResult okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
    }

    [Test]
    public async Task Test_GetNetatmoStatusAsync_WhenAuthenticated_Returns200()
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

        NetatmoOptions options = new NetatmoOptions();
        Mock<IOptions<NetatmoOptions>> optionsMock = new Mock<IOptions<NetatmoOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);

        HealthController controller = new HealthController(
            tokenStoreMock.Object,
            optionsMock.Object
        );

        // Setup HttpContext for Request.Scheme and Request.Host
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
        ActionResult<NetatmoAuthStatusResponse> result = await controller.GetNetatmoStatusAsync(CancellationToken.None);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Authenticated, Is.True);
            Assert.That(result.Value.StatusCode, Is.EqualTo(200));
            Assert.That(result.Value.Status, Is.EqualTo("Connected"));
            Assert.That(result.Value.Token, Is.EqualTo("test-token"));
            Assert.That(result.Value.LoginUrl, Is.Empty);
        });
    }

    [Test]
    public async Task Test_GetNetatmoStatusAsync_WhenTokenExpired_Returns401()
    {
        // Arrange
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "expired-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(-1) // Expired
        };

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenInfo);

        NetatmoOptions options = new NetatmoOptions();
        Mock<IOptions<NetatmoOptions>> optionsMock = new Mock<IOptions<NetatmoOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);

        HealthController controller = new HealthController(
            tokenStoreMock.Object,
            optionsMock.Object
        );

        // Setup HttpContext
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Scheme = "http",
                    Host = new HostString("localhost:8080")
                }
            }
        };

        // Act
        ActionResult<NetatmoAuthStatusResponse> result = await controller.GetNetatmoStatusAsync(CancellationToken.None);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Authenticated, Is.False);
            Assert.That(result.Value.StatusCode, Is.EqualTo(401));
            Assert.That(result.Value.Status, Is.EqualTo("Login required"));
            Assert.That(result.Value.LoginUrl, Is.EqualTo("http://localhost:8080/api/v1/netatmo/auth/login"));
            Assert.That(result.Value.Message, Is.Not.Empty);
        });
    }

    /// <summary>
    /// GetNetatmoStatusAsync should return unauthenticated status when token is null.
    /// </summary>
    [Test]
    public async Task Test_GetNetatmoStatusAsync_WhenTokenNull_Returns401()
    {
        // Arrange
        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((NetatmoTokenInfo)null);

        NetatmoOptions options = new NetatmoOptions();
        Mock<IOptions<NetatmoOptions>> optionsMock = new Mock<IOptions<NetatmoOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);

        HealthController controller = new HealthController(
            tokenStoreMock.Object,
            optionsMock.Object
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
        ActionResult<NetatmoAuthStatusResponse> result = await controller.GetNetatmoStatusAsync(CancellationToken.None);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value.Authenticated, Is.False);
        Assert.That(result.Value.StatusCode, Is.EqualTo(401));
        Assert.That(result.Value.LoginUrl, Is.EqualTo("http://localhost:8080/api/v1/netatmo/auth/login"));
    }

    /// <summary>
    /// GetNetatmoStatusAsync should use ApiBaseUrl from options when provided.
    /// </summary>
    [Test]
    public async Task Test_GetNetatmoStatusAsync_WhenApiBaseUrlProvided_UsesIt()
    {
        // Arrange
        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((NetatmoTokenInfo)null);

        NetatmoOptions options = new NetatmoOptions
        {
            ApiBaseUrl = "https://api.example.com"
        };
        Mock<IOptions<NetatmoOptions>> optionsMock = new Mock<IOptions<NetatmoOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);

        HealthController controller = new HealthController(
            tokenStoreMock.Object,
            optionsMock.Object
        );

        // Setup HttpContext (should not be used when ApiBaseUrl is set)
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Scheme = "http",
                    Host = new HostString("localhost:8080")
                }
            }
        };

        // Act
        ActionResult<NetatmoAuthStatusResponse> result = await controller.GetNetatmoStatusAsync(CancellationToken.None);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value.LoginUrl, Is.EqualTo("https://api.example.com/api/v1/netatmo/auth/login"));
    }

    [Test]
    public async Task Test_GetNetatmoStatusAsync_WhenApiBaseUrlProvided_ButEmpty()
    {
        // Arrange
        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((NetatmoTokenInfo)null);

        NetatmoOptions options = new NetatmoOptions
        {
            ApiBaseUrl = ""
        };
        Mock<IOptions<NetatmoOptions>> optionsMock = new Mock<IOptions<NetatmoOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);

        HealthController controller = new HealthController(
            tokenStoreMock.Object,
            optionsMock.Object
        );

        // Setup HttpContext (should not be used when ApiBaseUrl is set)
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Scheme = "http",
                    Host = new HostString("localhost:8080")
                }
            }
        };

        // Act
        ActionResult<NetatmoAuthStatusResponse> result = await controller.GetNetatmoStatusAsync(CancellationToken.None);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value.LoginUrl, Is.EqualTo("http://localhost:8080/api/v1/netatmo/auth/login"));
    }
}
