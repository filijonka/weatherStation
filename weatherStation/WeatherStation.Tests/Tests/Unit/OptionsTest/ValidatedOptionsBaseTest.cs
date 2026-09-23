using System;
using API.Options;

namespace WeatherStation.Tests.Tests.Unit.OptionsTest;

/// <summary>
/// Tests for ValidatedOptionsBase.
/// </summary>
[TestFixture]
public class ValidatedOptionsBaseTest
{
    /// <summary>
    /// Validate via RequireNonEmpty when value is empty should throw.
    /// </summary>
    [Test]
    public void Test_Validate_WhenRequiredValueEmpty_ThrowsInvalidOperationException()
    {
        TestOptions options = new TestOptions { Value = "" };

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => options.Validate());

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Does.Contain("Value"));
        Assert.That(ex.Message, Does.Contain("not configured"));
    }

    [Test]
    public void Test_Validate_WhenRequiredValueWhitespace_ThrowsInvalidOperationException()
    {
        TestOptions options = new TestOptions { Value = "   " };

        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Test]
    public void Test_Validate_WhenRequiredValueNonEmpty_DoesNotThrow()
    {
        TestOptions options = new TestOptions { Value = "valid" };

        Assert.DoesNotThrow(() => options.Validate());
    }

    private sealed class TestOptions : ValidatedOptionsBase
    {
        public string Value { get; init; } = string.Empty;

        public override void Validate() => RequireNonEmpty(Value, nameof(Value));
    }
}
