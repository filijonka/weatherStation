namespace Application.Models;

/// <summary>
/// Our own response class to always send the same response object
/// </summary>
/// <typeparam name="T"></typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Successful or not
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// If error a mesage
    /// </summary>
    public string Error { get; init; }

    /// <summary>
    /// if we are authenticated
    /// </summary>
    public bool IsAuthenticated { get; init; }

    /// <summary>
    /// Login url
    /// </summary>
    public string LoginUrl { get; init; }
    
    /// <summary>
    /// Data we respond with
    /// </summary>
    public T Data { get; init; }
}
