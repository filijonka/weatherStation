using API.Auth.Netatmo.Interface;
using API.Auth.Netatmo.Options;

using Microsoft.Extensions.Options;
using Serilog;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System;
namespace API.Auth.Netatmo.Services;

/// <inheritdoc />
public sealed class NetatmoTokenStore : INetatmoTokenStore
{
    private readonly NetatmoOptions options;
    private readonly ILogger logger;
    private readonly string filePath;
    private readonly SemaphoreSlim semaphore;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetatmoTokenStore"/> class.
    /// </summary>
    /// <param name="options">Netatmo options.</param>
    /// <param name="logger">Serilog logger.</param>
    public NetatmoTokenStore(IOptions<NetatmoOptions> options, ILogger logger)
    {
        this.options = options.Value;
        this.logger = logger.ForContext<NetatmoTokenStore>();
        this.filePath = ResolvePath(this.options.TokenFilePath);
        this.semaphore = new SemaphoreSlim(1, 1);
    }

    /// <inheritdoc />
    public async Task SaveAsync(NetatmoTokenInfo tokenInfo, CancellationToken cancellationToken)
    {
        await this.semaphore.WaitAsync(cancellationToken);
        try
        {
            string directory = Path.GetDirectoryName(this.filePath) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            JsonSerializerOptions serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(tokenInfo, serializerOptions);
            await File.WriteAllTextAsync(this.filePath, json, cancellationToken);
            this.logger.Information("Netatmo tokens stored at {Path}", this.filePath);
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<NetatmoTokenInfo> LoadAsync(CancellationToken cancellationToken)
    {
        await this.semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(this.filePath))
            {
                return null;
            }

            string json = await File.ReadAllTextAsync(this.filePath, cancellationToken);
            NetatmoTokenInfo tokenInfo = JsonSerializer.Deserialize<NetatmoTokenInfo>(json);
            return tokenInfo;
        }
        catch (Exception ex)
        {
            this.logger.Warning(ex, "Failed to read Netatmo token file");
            return null;
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    /// <summary>
    /// Resolves a relative token path against the current directory.
    /// </summary>
    /// <param name="configuredPath">Configured file path.</param>
    /// <returns>Absolute file path.</returns>
    private static string ResolvePath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        string baseDirectory = Directory.GetCurrentDirectory();
        return Path.Combine(baseDirectory, configuredPath);
    }
}
