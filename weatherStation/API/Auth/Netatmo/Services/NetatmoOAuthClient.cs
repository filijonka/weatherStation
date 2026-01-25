using API.Auth.Netatmo.Interface;
using API.Auth.Netatmo.Options;

using Microsoft.Extensions.Options;
using Serilog;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;
namespace API.Auth.Netatmo.Services;

/// <inheritdoc />
public sealed class NetatmoOAuthClient : INetatmoOAuthClient
{
    private readonly HttpClient httpClient;
    private readonly NetatmoOptions options;
    private readonly ILogger logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetatmoOAuthClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for Netatmo requests.</param>
    /// <param name="options">Netatmo OAuth configuration.</param>
    /// <param name="logger">Serilog logger.</param>
    public NetatmoOAuthClient(
        HttpClient httpClient,
        IOptions<NetatmoOptions> options,
        ILogger logger
    )
    {
        this.httpClient = httpClient;
        this.options = options.Value;
        this.logger = logger.ForContext<NetatmoOAuthClient>();
    }

    /// <inheritdoc />
    public string BuildAuthorizeUrl(string state)
    {
        string scopes = NormalizeScopes(this.options.Scopes);
        Dictionary<string, string> query = new Dictionary<string, string>
        {
            { "client_id", this.options.ClientId },
            { "redirect_uri", this.options.RedirectUri },
            { "scope", scopes },
            { "response_type", "code" },
            { "state", state }
        };

        string queryString = BuildQueryString(query);
        return string.Concat(this.options.AuthorizeUrl, "?", queryString);
    }

    /// <inheritdoc />
    public async Task<NetatmoTokenInfo> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        string scopes = NormalizeScopes(this.options.Scopes);
        Dictionary<string, string> formFields = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "client_id", this.options.ClientId },
            { "client_secret", this.options.ClientSecret },
            { "code", code },
            { "redirect_uri", this.options.RedirectUri },
            { "scope", scopes }
        };

        return await RequestTokenAsync(formFields, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<NetatmoTokenInfo> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        Dictionary<string, string> formFields = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "client_id", this.options.ClientId },
            { "client_secret", this.options.ClientSecret },
            { "refresh_token", refreshToken }
        };

        return await RequestTokenAsync(formFields, cancellationToken);
    }

    /// <summary>
    /// Sends a token request to Netatmo and returns the parsed token info.
    /// </summary>
    /// <param name="formFields">Form payload to send.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>Token information.</returns>
    private async Task<NetatmoTokenInfo> RequestTokenAsync(
        Dictionary<string, string> formFields,
        CancellationToken cancellationToken
    )
    {
        using FormUrlEncodedContent content = new FormUrlEncodedContent(formFields);
        using HttpResponseMessage response = await this.httpClient.PostAsync(this.options.TokenUrl, content, cancellationToken);

        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            this.logger.Warning("Netatmo token request failed: {StatusCode} {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException("Netatmo token request failed.");
        }

        JsonSerializerOptions serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        NetatmoTokenResponse tokenResponse = JsonSerializer.Deserialize<NetatmoTokenResponse>(responseBody, serializerOptions);
        if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Netatmo token response was empty.");
        }

        DateTime obtainedAtUtc = DateTime.UtcNow;
        DateTime expiresAtUtc = obtainedAtUtc.AddSeconds(tokenResponse.ExpiresIn);

        string scopeString = tokenResponse.Scope != null ? string.Join(" ", tokenResponse.Scope) : string.Empty;

        return new NetatmoTokenInfo
        {
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            Scope = scopeString,
            TokenType = tokenResponse.TokenType,
            ObtainedAtUtc = obtainedAtUtc,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    /// <summary>
    /// Normalizes scope formatting for Netatmo.
    /// </summary>
    /// <param name="scopes">Raw scope string.</param>
    /// <returns>Normalized scope string.</returns>
    private static string NormalizeScopes(string scopes)
    {
        string normalized = scopes.Replace(',', ' ').Trim();
        while (normalized.Contains("  ", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);
        }

        return normalized;
    }

    /// <summary>
    /// Builds a query string from provided parameters.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    /// <returns>Encoded query string.</returns>
    private static string BuildQueryString(Dictionary<string, string> query)
    {
        StringBuilder builder = new StringBuilder();
        bool isFirst = true;
        foreach (KeyValuePair<string, string> pair in query)
        {
            if (string.IsNullOrWhiteSpace(pair.Value))
            {
                continue;
            }

            if (!isFirst)
            {
                builder.Append('&');
            }

            builder.Append(Uri.EscapeDataString(pair.Key));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(pair.Value));
            isFirst = false;
        }

        return builder.ToString();
    }
}
