namespace Persistance.Influx;

/// <summary>
/// Configuration options for InfluxDB.
/// </summary>
public class InfluxOptions
{
    /// <summary>
    /// InfluxDB base URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// InfluxDB access token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// InfluxDB organization name.
    /// </summary>
    public string Org { get; set; } = string.Empty;

    /// <summary>
    /// InfluxDB bucket name.
    /// </summary>
    public string Bucket { get; set; } = string.Empty;
}
