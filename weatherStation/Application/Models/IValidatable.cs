namespace Application.Models;

/// <summary>
/// Validate interface
/// </summary>
public interface IValidatable
{
    /// <summary>
    /// Checks if a object is valid
    /// </summary>
    /// <returns>If object is valid</returns>
    bool IsValid();
}
