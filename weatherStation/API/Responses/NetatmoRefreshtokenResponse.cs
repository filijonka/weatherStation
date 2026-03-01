using Newtonsoft.Json;

namespace API.Responses;

/// <summary>
/// The reposne from netatmo for querying a refreshtoken
/// </summary>
public class NetatmoRefreshTokenResponse
{
    /// <summary>
    /// The new access token
    /// </summary>
    [JsonProperty("access_token")]
    public string AccessToken { get; set; }

    /// <summary>
    /// The new Refreshtoken
    /// </summary>
    [JsonProperty("refresh_token")]
    public string RefreshToken { get; set; }

    /// <summary>
    /// Number of seconds from now this will expires
    /// </summary>
    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("scope")]
    public string Scope { get; set; }

    /// <summary>
    /// TYpe of token
    /// </summary>
    [JsonProperty("token_type")]
    public string TokenType { get; set; }
}