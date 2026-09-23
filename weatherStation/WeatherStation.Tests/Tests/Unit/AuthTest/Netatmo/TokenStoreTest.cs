using API.Auth.Netatmo.Options;
using API.Auth.Netatmo.Services;

using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;

namespace WeatherStation.Tests.Tests.Unit.AuthTest.Netatmo;

/// <summary>
/// Tests for Netatmo token storage.
/// </summary>
[TestFixture]
public class TokenStoreTest
{
    /// <summary>
    /// Save and load should persist token info.
    /// </summary>
    [Test]
    public async Task Test_SaveAndLoad_PersistsToken()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "weatherstation-tests", Guid.NewGuid().ToString("N"));
        string tokenPath = Path.Combine(tempDirectory, "netatmo.tokens.json");

        NetatmoOptions options = new NetatmoOptions
        {
            TokenFilePath = tokenPath
        };

        IOptions<NetatmoOptions> optionsWrapper = Options.Create(options);
        ILogger logger = new LoggerConfiguration().CreateLogger();
        NetatmoTokenStore store = new NetatmoTokenStore(optionsWrapper, logger);

        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "access",
            RefreshToken = "refresh",
            Scope = "read_station",
            TokenType = "bearer",
            ObtainedAtUtc = new DateTime(2026, 1, 20, 10, 0, 0, DateTimeKind.Utc),
            ExpiresAtUtc = new DateTime(2026, 1, 20, 13, 0, 0, DateTimeKind.Utc)
        };

        await store.SaveAsync(tokenInfo, CancellationToken.None);
        NetatmoTokenInfo loaded = await store.LoadAsync(CancellationToken.None);

        Assert.That(loaded, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(loaded.AccessToken, Is.EqualTo("access"));
            Assert.That(loaded.RefreshToken, Is.EqualTo("refresh"));
            Assert.That(loaded.Scope, Is.EqualTo("read_station"));
            Assert.That(loaded.TokenType, Is.EqualTo("bearer"));
            Assert.That(loaded.ObtainedAtUtc, Is.EqualTo(new DateTime(2026, 1, 20, 10, 0, 0, DateTimeKind.Utc)));
            Assert.That(loaded.ExpiresAtUtc, Is.EqualTo(new DateTime(2026, 1, 20, 13, 0, 0, DateTimeKind.Utc)));
        });

        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Test]
    public void Test_SaveAndLoad_PersistsToken_InvalidPath()
    {
        NetatmoOptions options = new NetatmoOptions
        {
            TokenFilePath = "/"
        };

        IOptions<NetatmoOptions> optionsWrapper = Options.Create(options);
        ILogger logger = new LoggerConfiguration().CreateLogger();
        NetatmoTokenStore store = new NetatmoTokenStore(optionsWrapper, logger);

        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "access",
            RefreshToken = "refresh",
            Scope = "read_station",
            TokenType = "bearer",
            ObtainedAtUtc = new DateTime(2026, 1, 20, 10, 0, 0, DateTimeKind.Utc),
            ExpiresAtUtc = new DateTime(2026, 1, 20, 13, 0, 0, DateTimeKind.Utc)
        };

        InvalidOperationException ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await store.SaveAsync(tokenInfo, CancellationToken.None));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Message, Is.EqualTo("Failed to store Netatmo token at /"));
    }

    [Test]
    public void Test_SaveAndLoad_PersistsToken_NoPath()
    {
        NetatmoOptions options = new NetatmoOptions
        {
            TokenFilePath = ""
        };

        IOptions<NetatmoOptions> optionsWrapper = Options.Create(options);
        ILogger logger = new LoggerConfiguration().CreateLogger();
        NetatmoTokenStore store = new NetatmoTokenStore(optionsWrapper, logger);

        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "access",
            RefreshToken = "refresh",
            Scope = "read_station",
            TokenType = "bearer",
            ObtainedAtUtc = new DateTime(2026, 1, 20, 10, 0, 0, DateTimeKind.Utc),
            ExpiresAtUtc = new DateTime(2026, 1, 20, 13, 0, 0, DateTimeKind.Utc)
        };

        InvalidOperationException ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await store.SaveAsync(tokenInfo, CancellationToken.None));

    }

    /// <summary>
    /// Load should return null when file not found.
    /// </summary>
    [Test]
    public async Task Test_Load_FileNotFound_ReturnsNull()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "weatherstation-tests", Guid.NewGuid().ToString("N"));
        string tokenPath = Path.Combine(tempDirectory, "nonexistent.tokens.json");

        NetatmoOptions options = new NetatmoOptions
        {
            TokenFilePath = tokenPath
        };

        IOptions<NetatmoOptions> optionsWrapper = Options.Create(options);
        ILogger logger = new LoggerConfiguration().CreateLogger();
        NetatmoTokenStore store = new NetatmoTokenStore(optionsWrapper, logger);

        NetatmoTokenInfo loaded = await store.LoadAsync(CancellationToken.None);

        Assert.That(loaded, Is.Null);
    }

    /// <summary>
    /// Load should return null when JSON is invalid.
    /// </summary>
    [Test]
    public async Task Test_Load_InvalidJson_ReturnsNull()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "weatherstation-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        string tokenPath = Path.Combine(tempDirectory, "netatmo.tokens.json");

        await File.WriteAllTextAsync(tokenPath, "invalid json {");

        NetatmoOptions options = new NetatmoOptions
        {
            TokenFilePath = tokenPath
        };

        IOptions<NetatmoOptions> optionsWrapper = Options.Create(options);
        ILogger logger = new LoggerConfiguration().CreateLogger();
        NetatmoTokenStore store = new NetatmoTokenStore(optionsWrapper, logger);

        NetatmoTokenInfo loaded = await store.LoadAsync(CancellationToken.None);

        Assert.That(loaded, Is.Null);

        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    /// <summary>
    /// Save should create directory if it doesn't exist.
    /// </summary>
    [Test]
    public async Task Test_Save_CreatesDirectory()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "weatherstation-tests", Guid.NewGuid().ToString("N"));
        string tokenPath = Path.Combine(tempDirectory, "subdir", "netatmo.tokens.json");

        NetatmoOptions options = new NetatmoOptions
        {
            TokenFilePath = tokenPath
        };

        IOptions<NetatmoOptions> optionsWrapper = Options.Create(options);
        ILogger logger = new LoggerConfiguration().CreateLogger();
        NetatmoTokenStore store = new NetatmoTokenStore(optionsWrapper, logger);

        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "access",
            RefreshToken = "refresh",
            Scope = "read_station",
            TokenType = "bearer",
            ObtainedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(3)
        };

        await store.SaveAsync(tokenInfo, CancellationToken.None);

        Assert.That(File.Exists(tokenPath), Is.True);

        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, true);
        }
    }
}
