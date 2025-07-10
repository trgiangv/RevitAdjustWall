using NUnit.Framework;
using RevitAdjustWall.Validation;

namespace RevitAdjustWall.Tests.Validation;

[TestFixture]
public class InputValidatorTests
{
    [TestCase("10", true)]
    [TestCase("10.5", true)]
    [TestCase("0.1", true)]
    [TestCase("10000", true)]
    [TestCase("123.456", true)]
    [TestCase("1,5", true)] // Comma as decimal separator
    [TestCase("", false)]
    [TestCase("abc", false)]
    [TestCase("-5", false)]
    [TestCase("0", false)]
    [TestCase("0.05", false)] // Below minimum
    [TestCase("10001", false)] // Above maximum
    [TestCase("10.5.5", false)] // Multiple decimal points
    [TestCase("10a", false)] // Mixed alphanumeric
    [TestCase("  ", false)] // Whitespace only
    [TestCase("10 ", true)] // Trailing whitespace (should be trimmed)
    [TestCase(" 10", true)] // Leading whitespace (should be trimmed)
    [TestCase("10.0000", true)] // Many decimal places
    [TestCase("1e5", false)] // Scientific notation
    [TestCase("∞", false)] // Special characters
    [TestCase("10..5", false)] // Double decimal points
    public void TryParseGapDistance_VariousInputs_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = InputValidator.TryParseGapDistance(input, out double gapDistance);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
        if (expected)
        {
            Assert.That(gapDistance, Is.GreaterThan(0));
        }
    }

    [TestCase("10", 10.0)]
    [TestCase("10.5", 10.5)]
    [TestCase("0.1", 0.1)]
    [TestCase("10000", 10000.0)]
    [TestCase("123.456", 123.456)]
    [TestCase("1,5", 1.5)] // Comma converted to decimal
    [TestCase(" 10 ", 10.0)] // Whitespace trimmed
    [TestCase("10.0000", 10.0)] // Trailing zeros
    public void TryParseGapDistance_ValidInputs_ReturnsCorrectValue(string input, double expected)
    {
        // Act
        var result = InputValidator.TryParseGapDistance(input, out double gapDistance);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(gapDistance, Is.EqualTo(expected).Within(0.001));
    }

    [TestCase("")]
    [TestCase("abc")]
    [TestCase("-5")]
    [TestCase("0")]
    [TestCase("0.05")]
    [TestCase("10001")]
    [TestCase("10.5.5")]
    [TestCase("10a")]
    [TestCase("  ")]
    [TestCase("1e5")]
    [TestCase("∞")]
    [TestCase("10..5")]
    public void TryParseGapDistance_InvalidInputs_ReturnsFalse(string input)
    {
        // Act
        var result = InputValidator.TryParseGapDistance(input, out double gapDistance);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(gapDistance, Is.EqualTo(0.0));
    }

    [TestCase("")]
    [TestCase("abc")]
    [TestCase("-5")]
    [TestCase("0")]
    [TestCase("0.05")]
    [TestCase("10001")]
    public void GetGapDistanceValidationError_InvalidInputs_ReturnsAppropriateMessage(string input)
    {
        // Act
        var message = InputValidator.GetGapDistanceValidationError(input);

        // Assert
        Assert.That(message, Is.Not.Null);
        Assert.That(message, Is.Not.Empty);
    }

    [Test]
    public void IsValidNumeric_ValidInputs_ReturnsTrue()
    {
        // Arrange
        var validInputs = new[] { "10", "10.5", "0.1", "10000", "123.456" };

        foreach (var input in validInputs)
        {
            // Act
            var result = InputValidator.IsValidNumeric(input);

            // Assert
            Assert.That(result, Is.True, $"Input '{input}' should be valid");
        }
    }

    [Test]
    public void IsValidNumeric_InvalidInputs_ReturnsFalse()
    {
        // Arrange
        var invalidInputs = new[] { "", "abc", "10a", "10.5.5", "  ", "1e5", "∞" };

        foreach (var input in invalidInputs)
        {
            // Act
            var result = InputValidator.IsValidNumeric(input);

            // Assert
            Assert.That(result, Is.False, $"Input '{input}' should be invalid");
        }
    }
}