using System.Globalization;
using System.Text.RegularExpressions;
using RevitAdjustWall.Extensions;

namespace RevitAdjustWall.Validation;

/// <summary>
/// Provides input validation functionality
/// Follows Single Responsibility Principle (SRP)
/// </summary>
public static class InputValidator
{
    private static readonly Regex NumericRegex = new Regex(@"^[0-9]*\.?[0-9]+$", RegexOptions.Compiled);
    private static readonly double MinGapDistanceMm = 1.0; // Minimum 1mm
    private static readonly double MaxGapDistanceMm = 1000.0; // Maximum 1000mm

    /// <summary>
    /// Validates if the input string represents a valid numeric value
    /// </summary>
    /// <param name="input">The input string to validate</param>
    /// <returns>True if the input is a valid number, false otherwise</returns>
    private static bool IsValidNumeric(string input)
    {
        return !string.IsNullOrWhiteSpace(input) && NumericRegex.IsMatch(input.Trim());
    }

    /// <summary>
    /// Validates if the gap distance is within acceptable range
    /// </summary>
    /// <param name="gapDistanceMm">The gap distance in millimeters</param>
    /// <returns>True if the gap distance is valid, false otherwise</returns>
    private static bool IsValidGapDistance(double gapDistanceMm)
    {
        return gapDistanceMm >= MinGapDistanceMm &&
               gapDistanceMm <= MaxGapDistanceMm &&
               !double.IsNaN(gapDistanceMm) &&
               !double.IsInfinity(gapDistanceMm);
    }

    /// <summary>
    /// Tries to parse the input string as a valid gap distance
    /// </summary>
    /// <param name="input">The input string to parse (in millimeters)</param>
    /// <param name="gapDistanceInFeet">The parsed gap distance in Revit internal units (feet)</param>
    /// <returns>True if parsing was successful and value is valid, false otherwise</returns>
    public static bool TryParseGapDistance(string input, out double gapDistanceInFeet)
    {
        gapDistanceInFeet = 0.0;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        var trimmedInput = input.Trim();

        // Handle comma as decimal separator by replacing with dot
        var normalizedInput = trimmedInput.Replace(',', '.');

        // Check if the normalized input is valid numeric format
        if (!IsValidNumeric(normalizedInput))
            return false;

        if (!double.TryParse(normalizedInput, NumberStyles.Float, CultureInfo.InvariantCulture, out var gapDistanceMm))
            return false;

        // Validate the millimeter value first
        if (!IsValidGapDistance(gapDistanceMm))
        {
            gapDistanceInFeet = 0.0;
            return false;
        }

        // Convert to Revit internal units (feet)
        gapDistanceInFeet = gapDistanceMm.FromMillimeters();
        return true;
    }

    /// <summary>
    /// Gets the validation error message for invalid gap distance
    /// </summary>
    /// <param name="input">The input that failed validation</param>
    /// <returns>Descriptive error message</returns>
    public static string GetGapDistanceValidationError(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "Gap distance cannot be empty.";

        if (!IsValidNumeric(input))
            return "Gap distance must be a valid number.";

        if (!double.TryParse(input.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            return "Invalid gap distance value.";

        if (value < MinGapDistanceMm)
            return $"Gap distance must be at least {MinGapDistanceMm} mm.";

        if (value > MaxGapDistanceMm)
            return $"Gap distance cannot exceed {MaxGapDistanceMm} mm.";

        if (double.IsNaN(value) || double.IsInfinity(value))
            return "Gap distance must be a finite number.";

        return "Invalid gap distance value.";
    }
}