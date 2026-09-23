using System.Collections.Generic;

namespace Persistence.Influx;

/// <summary>
/// Configuration options for InfluxDB.
/// </summary>
public class InfluxOptions
{
    /// <summary>
    /// InfluxDB base URL.
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// InfluxDB access token.
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// InfluxDB organization name.
    /// </summary>
    public string Org { get; init; } = string.Empty;

    /// <summary>
    /// InfluxDB bucket name.
    /// </summary>
    public string Bucket { get; init; } = string.Empty;

    /// <summary>
    /// The different tables/measurment
    /// </summary>
    public Dictionary<string, string> Tables { get; init; } = new Dictionary<string, string>();
}
