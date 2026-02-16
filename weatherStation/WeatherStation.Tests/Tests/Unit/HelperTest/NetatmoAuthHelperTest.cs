using API.Auth.Netatmo.Interface;
using API.Auth.Netatmo.Services;
using API.Helpers;
using API.Responses;

using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherStation.Tests.Tests.Unit.HelperTest;

/// <summary>
/// Tests for NetatmoAuthHelper.
/// </summary>
[TestFixture]
public class NetatmoAuthHelperTest
{
    /// <summary>
    /// GetAuthStatusAsync should return authenticated status when token is valid.
    /// </summary>
    [Test]
    public async Task Test_GetAuthStatusAsync_WhenTokenValid_ReturnsAuthenticated()
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

        string baseUrl = "https://localhost:8080";

        // Act
        NetatmoAuthStatusResponse result = await NetatmoAuthHelper.GetAuthStatusAsync(
            tokenStoreMock.Object,
            baseUrl,
            CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Authenticated, Is.True);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Assert.That(result.Status, Is.EqualTo("Connected"));
        Assert.That(result.Token, Is.EqualTo("test-token"));
        Assert.That(result.LoginUrl, Is.Empty);
        Assert.That(result.ExpiresAtUtc, Is.Not.Null);
        Assert.That(result.Message, Is.Empty);
    }

    /// <summary>
    /// GetAuthStatusAsync should return unauthenticated status when token is expired.
    /// </summary>
    [Test]
    public async Task Test_GetAuthStatusAsync_WhenTokenExpired_ReturnsUnauthenticated()
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

        string baseUrl = "https://localhost:8080";

        // Act
        NetatmoAuthStatusResponse result = await NetatmoAuthHelper.GetAuthStatusAsync(
            tokenStoreMock.Object,
            baseUrl,
            CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Authenticated, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(401));
        Assert.That(result.Status, Is.EqualTo("Login required"));
        Assert.That(result.Token, Is.EqualTo(""));
        Assert.That(result.LoginUrl, Is.EqualTo("https://localhost:8080/api/v1/netatmo/auth/login"));
        Assert.That(result.ExpiresAtUtc, Is.Null);
        Assert.That(result.Message, Is.EqualTo("Netatmo authentication required. Please log in first."));
    }

    /// <summary>
    /// GetAuthStatusAsync should return unauthenticated status when token is null.
    /// </summary>
    [Test]
    public async Task Test_GetAuthStatusAsync_WhenTokenNull_ReturnsUnauthenticated()
    {
        // Arrange
        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((NetatmoTokenInfo)null);

        string baseUrl = "https://localhost:8080";

        // Act
        NetatmoAuthStatusResponse result = await NetatmoAuthHelper.GetAuthStatusAsync(
            tokenStoreMock.Object,
            baseUrl,
            CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Authenticated, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(401));
        Assert.That(result.Status, Is.EqualTo("Login required"));
        Assert.That(result.Token, Is.EqualTo(""));
        Assert.That(result.LoginUrl, Is.EqualTo("https://localhost:8080/api/v1/netatmo/auth/login"));
        Assert.That(result.ExpiresAtUtc, Is.Null);
        Assert.That(result.Message, Is.EqualTo("Netatmo authentication required. Please log in first."));
    }

    /// <summary>
    /// GetAuthStatusAsync should handle baseUrl with trailing slash.
    /// </summary>
    [Test]
    public async Task Test_GetAuthStatusAsync_WithTrailingSlash_BuildsCorrectLoginUrl()
    {
        // Arrange
        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((NetatmoTokenInfo)null);

        string baseUrl = "https://localhost:8080";

        // Act
        NetatmoAuthStatusResponse result = await NetatmoAuthHelper.GetAuthStatusAsync(
            tokenStoreMock.Object,
            baseUrl,
            CancellationToken.None);

        // Assert
        Assert.That(result.LoginUrl, Is.EqualTo("https://localhost:8080/api/v1/netatmo/auth/login"));
    }

    /// <summary>
    /// GetAuthStatusAsync should handle token that expires exactly at UtcNow.
    /// </summary>
    [Test]
    public async Task Test_GetAuthStatusAsync_WhenTokenExpiresAtUtcNow_ReturnsUnauthenticated()
    {
        // Arrange
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "expiring-token",
            ExpiresAtUtc = DateTime.UtcNow // Expires exactly now
        };

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenInfo);

        string baseUrl = "https://localhost:8080";

        // Act
        NetatmoAuthStatusResponse result = await NetatmoAuthHelper.GetAuthStatusAsync(
            tokenStoreMock.Object,
            baseUrl,
            CancellationToken.None);

        // Assert
        Assert.That(result.Authenticated, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(401));
    }
}
