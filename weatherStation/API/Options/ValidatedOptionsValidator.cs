using System;
using Microsoft.Extensions.Options;

namespace API.Options;

/// <summary>
/// Generic validator that calls Validate() on options inheriting from ValidatedOptionsBase.
/// </summary>
/// <typeparam name="TOptions">The options type; must inherit ValidatedOptionsBase.</typeparam>
public sealed class ValidatedOptionsValidator<TOptions> : IValidateOptions<TOptions>
    where TOptions : ValidatedOptionsBase
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string name, TOptions options)
    {
        try
        {
            options.Validate();
            return ValidateOptionsResult.Success;
        }
        catch (Exception ex)
        {
            return ValidateOptionsResult.Fail(ex.Message);
        }
    }
}
