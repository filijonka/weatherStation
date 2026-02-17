using Newtonsoft.Json;

namespace Application.Models;

/// <summary>
/// Netatmo's error response
/// </summary>
public sealed class NetatmoError
{
    /// <summary>
    /// the error body
    /// </summary>
    [JsonProperty("Error")]
    public Error ErrorType { get; set; }
}

    
/// <summary>
/// The error information
/// </summary>
public sealed class Error
{
    /// <summary>
    /// the code
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// The message
    /// </summary>
    public string Message { get; set; }
}
