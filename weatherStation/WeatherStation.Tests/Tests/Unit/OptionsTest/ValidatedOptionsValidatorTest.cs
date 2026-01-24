using System.Linq;
using API.Options;
using Microsoft.Extensions.Options;

namespace WeatherStation.Tests.Tests.Unit.OptionsTest;

/// <summary>
/// Tests for ValidatedOptionsValidator.
/// </summary>
[TestFixture]
public class ValidatedOptionsValidatorTest
{
    [Test]
    public void Test_Validate_WhenOptionsValid_ReturnsSuccess()
    {
        ValidatedOptionsValidator<TestOptions> validator = new ValidatedOptionsValidator<TestOptions>();
        TestOptions options = new TestOptions { Value = "ok" };

        ValidateOptionsResult result = validator.Validate(null, options);

        Assert.That(result.Succeeded, Is.True);
    }

    /// <summary>
    /// Validate when options.Validate throws should return Fail.
    /// </summary>
    [Test]
    public void Test_Validate_WhenOptionsInvalid_ReturnsFail()
    {
        ValidatedOptionsValidator<TestOptions> validator = new ValidatedOptionsValidator<TestOptions>();
        TestOptions options = new TestOptions { Value = "" };

        ValidateOptionsResult result = validator.Validate(null, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failures, Is.Not.Null);
            Assert.That(result.Failures.First(), Does.Contain("Value"));
        });
    }

    private sealed class TestOptions : ValidatedOptionsBase
    {
        public string Value { get; init; } = string.Empty;

        public override void Validate() => RequireNonEmpty(Value, nameof(Value));
    }
}
