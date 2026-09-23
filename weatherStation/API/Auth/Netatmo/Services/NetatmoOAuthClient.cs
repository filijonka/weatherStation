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
using System.Linq;
using API.Responses;

namespace API.Auth.Netatmo.Services;

/// <inheritdoc />
public sealed class NetatmoOAuthClient : INetatmoOAuthClient
{
    private readonly HttpClient httpClient;
    private readonly IOptions<NetatmoOptions> options;
    private readonly INetatmoTokenStore netatmoTokenStore;
    private readonly ILogger logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetatmoOAuthClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for Netatmo requests.</param>
    /// <param name="options">Netatmo OAuth configuration.</param>
    /// <param name="netatmoTokenStore"></param>
    /// <param name="logger">Serilog logger.</param>
    public NetatmoOAuthClient(
        HttpClient httpClient,
        IOptions<NetatmoOptions> options,
        INetatmoTokenStore netatmoTokenStore,
        ILogger logger
    )
    {
        this.httpClient = httpClient;
        this.options = options;
        this.netatmoTokenStore = netatmoTokenStore;
        this.logger = logger.ForContext<NetatmoOAuthClient>();
    }

    /// <inheritdoc />
    public async Task<NetatmoAuthStatusResponse> Login(CancellationToken cancellationToken)
    {
        NetatmoTokenInfo tokenInfo = await netatmoTokenStore.LoadAsync(cancellationToken);
        if (tokenInfo is null)
        {
            string state = Guid.NewGuid().ToString("N");
            string authorizeUrl = this.BuildAuthorizeUrl(state);
            return new NetatmoAuthStatusResponse
            {
                Authenticated = false,
                Token = "",
                Status = "Login required",
                LoginUrl = authorizeUrl,
                ExpiresAtUtc = null,
                StatusCode = 401,
                Message = "Netatmo authentication required. Please log in first."
            };
        }

        if (tokenInfo.ExpiresAtUtc > DateTime.UtcNow)
        {
            return new NetatmoAuthStatusResponse
            {
                Authenticated = true,
                Token = tokenInfo.AccessToken,
                Status = "Connected",
                LoginUrl = "",
                ExpiresAtUtc = tokenInfo.ExpiresAtUtc,
                StatusCode = 200,
                Message = ""
            };
        }

        Dictionary<string, string> formFields = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "client_id", this.options.Value.ClientId },
            { "client_secret", this.options.Value.ClientSecret },
            { "refresh_token", tokenInfo.RefreshToken }
        };
        try
        {
            tokenInfo = await RequestTokenAsync(formFields, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            this.logger.Error(ex, "REquest of token throw an exception");
            string state = Guid.NewGuid().ToString("N");
            string authorizeUrl = this.BuildAuthorizeUrl(state);
            return new NetatmoAuthStatusResponse
            {
                Authenticated = false,
                Token = "",
                Status = "Login required",
                LoginUrl = authorizeUrl,
                ExpiresAtUtc = null,
                StatusCode = 401,
                Message = "Netatmo authentication required. Please log in first."
            };
        }

        await this.netatmoTokenStore.SaveAsync(tokenInfo, cancellationToken);
        return new NetatmoAuthStatusResponse
        {
            Authenticated = true,
            Token = tokenInfo.AccessToken,
            Status = "Connected",
            LoginUrl = "",
            ExpiresAtUtc = tokenInfo.ExpiresAtUtc,
            StatusCode = 200,
            Message = ""
        };
    }

    /// <inheritdoc />
    public async Task<NetatmoTokenInfo> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        string scopes = NormalizeScopes(this.options.Value.Scopes);
        Dictionary<string, string> formFields = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "client_id", this.options.Value.ClientId },
            { "client_secret", this.options.Value.ClientSecret },
            { "code", code },
            { "redirect_uri", this.options.Value.RedirectUri },
            { "scope", scopes }
        };

        return await RequestTokenAsync(formFields, cancellationToken);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="formFields"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private  async Task<NetatmoTokenInfo> RequestTokenAsync(
        Dictionary<string, string> formFields,
        CancellationToken cancellationToken
    )
    {
        using FormUrlEncodedContent content = new FormUrlEncodedContent(formFields);
        using HttpResponseMessage response = await this.httpClient.PostAsync(this.options.Value.TokenUrl, content, cancellationToken);

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

    private string BuildAuthorizeUrl(string state)
    {
        string scopes = NormalizeScopes(this.options.Value.Scopes);
        Dictionary<string, string> query = new Dictionary<string, string>
        {
            { "client_id", this.options.Value.ClientId },
            { "redirect_uri", this.options.Value.RedirectUri },
            { "scope", scopes },
            { "response_type", "code" },
            { "state", state }
        };

        string queryString = BuildQueryString(query);
        return string.Concat(this.options.Value.AuthorizeUrl, "?", queryString);
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
        foreach (KeyValuePair<string, string> pair in query.Where(pair => !string.IsNullOrWhiteSpace(pair.Value)))
        {
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
