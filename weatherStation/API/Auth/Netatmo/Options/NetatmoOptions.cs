using System;
using API.Options;

namespace API.Auth.Netatmo.Options;

/// <summary>
/// Configuration options for Netatmo OAuth.
/// </summary>
public class NetatmoOptions : ValidatedOptionsBase
{
    /// <summary>
    /// Netatmo application client id.
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Netatmo application client secret.
    /// </summary>
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>
    /// Redirect URI for the OAuth callback.
    /// </summary>
    public string RedirectUri { get; init; } = string.Empty;

    /// <summary>
    /// OAuth scopes to request.
    /// </summary>
    public string Scopes { get; set; } = "read_station";

    /// <summary>
    /// OAuth authorization endpoint.
    /// </summary>
    public string AuthorizeUrl { get; init; } = "https://api.netatmo.com/oauth2/authorize";

    /// <summary>
    /// OAuth token endpoint.
    /// </summary>
    public string TokenUrl { get; init; } = "https://api.netatmo.com/oauth2/token";

    /// <summary>
    /// Path to the token storage file.
    /// </summary>
    public string TokenFilePath { get; init; } = "./data/netatmo.tokens.json";

    /// <summary>
    /// Base URL of this API (e.g. https://localhost:8080) used to build the login link
    /// when returning status to Grafana. Defaults to http://localhost:8080.
    /// </summary>
    public string ApiBaseUrl { get; init; } = "http://localhost:8080";

    /// <summary>
    /// How often the Netatmo api will be called
    /// </summary>
    public TimeSpan Frequency { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// 
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(0);

    /// <inheritdoc />
    public override void Validate()
    {
        RequireNonEmpty(ClientId, nameof(ClientId));
        RequireNonEmpty(ClientSecret, nameof(ClientSecret));
        RequireNonEmpty(RedirectUri, nameof(RedirectUri));
        RequireNonEmpty(Scopes, nameof(Scopes));
        RequireNonEmpty(AuthorizeUrl, nameof(AuthorizeUrl));
        RequireNonEmpty(TokenUrl, nameof(TokenUrl));
        RequireNonEmpty(TokenFilePath, nameof(TokenFilePath));
    }
}
