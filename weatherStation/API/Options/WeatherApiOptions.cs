namespace API.Options;

/// <summary>
/// Configuration for the external weather API.
/// </summary>
public class WeatherApiOptions : ValidatedOptionsBase
{
    /// <summary>
    /// Base URL for the external API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API key or token value.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Relative path for readings endpoint.
    /// </summary>
    public string ReadingsPath { get; set; } = "readings";

    /// <summary>
    /// Header name used for the API key.
    /// </summary>
    public string ApiKeyHeaderName { get; set; } = "X-Api-Key";

    /// <inheritdoc />
    public override void Validate()
    {
        RequireNonEmpty(BaseUrl, nameof(BaseUrl));
        RequireNonEmpty(ApiKey, nameof(ApiKey));
    }
}
