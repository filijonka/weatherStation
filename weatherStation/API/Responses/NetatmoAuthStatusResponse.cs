using System;

namespace API.Responses;

/// <summary>
/// Unified response model for Netatmo authentication status.
/// </summary>
public class NetatmoAuthStatusResponse
{
    /// <summary>
    /// Gets or sets whether the user is authenticated.
    /// </summary>
    public bool Authenticated { get; set; }
    
    /// <summary>
    /// Gets or sets the access token (null if not authenticated).
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// Gets or sets the authentication status message.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the login URL (empty if authenticated).
    /// </summary>
    public string LoginUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token expiry time (UTC).
    /// </summary>
    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the response message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
