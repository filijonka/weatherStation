using System;

namespace API.Exceptions;

/// <summary>
/// Handling bad request response from Netatmo API
/// </summary>
public sealed class NetatmoBadRequestException : Exception
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="code"></param>
    /// <param name="apiMessage"></param>
    public NetatmoBadRequestException(int code, string apiMessage)
        : base(apiMessage)
    {
        this.Code = code;
        this.ApiMessage = apiMessage;
    }

    /// <summary>
    /// The error code
    /// </summary>
    public int Code { get; }

    /// <summary>
    /// Message from API
    /// </summary>
    public string ApiMessage { get; }
}
