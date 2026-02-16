namespace Application.Models;

public class ApiResponse<T>
{
    public bool IsSuccess { get; init; }

    public string Error { get; init; }

    public bool IsAuthenticated { get; init; }

    public string LoginUrl { get; init; }
    
    public T Data { get; init; }
}
