using System;
using System.Text.Json.Serialization;

namespace API.Auth.Netatmo.Services;

/// <summary>
/// Represents stored Netatmo OAuth tokens.
/// </summary>
public sealed class NetatmoTokenInfo
{
    /// <summary>
    /// Access token string.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token string.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Granted OAuth scopes.
    /// </summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// Token type (for example, Bearer).
    /// </summary>
    public string TokenType { get; set; } = string.Empty;

    /// <summary>
    /// Time the token was obtained (UTC).
    /// </summary>
    public DateTime ObtainedAtUtc { get; set; }

    /// <summary>
    /// Time the token expires (UTC).
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }
}

/// <summary>
/// Raw token response payload from Netatmo.
/// </summary>
internal sealed class NetatmoTokenResponse
{
    /// <summary>
    /// Access token string.
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Refresh token string.
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>
    /// Granted OAuth scopes.
    /// </summary>
    [JsonPropertyName("scope")]
    public string Scope { get; init; } = string.Empty;

    /// <summary>
    /// Token type (for example, Bearer).
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = string.Empty;

    /// <summary>
    /// Token lifetime in seconds.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
}
