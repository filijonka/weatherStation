namespace API.Auth.Netatmo.Options;

/// <summary>
/// Configuration options for Netatmo OAuth.
/// </summary>
public class NetatmoOptions
{
    /// <summary>
    /// Netatmo application client id.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Netatmo application client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Redirect URI for the OAuth callback.
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// OAuth scopes to request.
    /// </summary>
    public string Scopes { get; set; } = "read_station";

    /// <summary>
    /// OAuth authorization endpoint.
    /// </summary>
    public string AuthorizeUrl { get; set; } = "https://api.netatmo.com/oauth2/authorize";

    /// <summary>
    /// OAuth token endpoint.
    /// </summary>
    public string TokenUrl { get; set; } = "https://api.netatmo.com/oauth2/token";

    /// <summary>
    /// Path to the token storage file.
    /// </summary>
    public string TokenFilePath { get; set; } = "netatmo.tokens.json";
}
