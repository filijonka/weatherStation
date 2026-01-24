using System;

namespace API.Options;

/// <summary>
/// Base class for options that validate their own required members.
/// </summary>
public abstract class ValidatedOptionsBase
{
    /// <summary>
    /// Validates that all required options are set. Throws if validation fails.
    /// </summary>
    public abstract void Validate();

    /// <summary>
    /// Ensures the value is non-null and non-whitespace. Throws if invalid.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="propertyName">The property name for the error message.</param>
    /// <exception cref="InvalidOperationException">Thrown when the value is null or whitespace.</exception>
    protected static void RequireNonEmpty(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{propertyName} is not configured.");
        }
    }
}
