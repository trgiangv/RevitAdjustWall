using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RevitAdjustWall.Validation
{
    /// <summary>
    /// Provides input validation functionality
    /// Follows Single Responsibility Principle (SRP)
    /// </summary>
    public static class InputValidator
    {
        private static readonly Regex NumericRegex = new Regex(@"^[0-9]*\.?[0-9]+$", RegexOptions.Compiled);
        private const double MIN_GAP_DISTANCE = 0.1; // Minimum 0.1mm
        private const double MAX_GAP_DISTANCE = 10000.0; // Maximum 10 meters in mm

        /// <summary>
        /// Validates if the input string represents a valid numeric value
        /// </summary>
        /// <param name="input">The input string to validate</param>
        /// <returns>True if the input is a valid number, false otherwise</returns>
        public static bool IsValidNumeric(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            return NumericRegex.IsMatch(input.Trim());
        }

        /// <summary>
        /// Validates if the gap distance is within acceptable range
        /// </summary>
        /// <param name="gapDistance">The gap distance in millimeters</param>
        /// <returns>True if the gap distance is valid, false otherwise</returns>
        public static bool IsValidGapDistance(double gapDistance)
        {
            return gapDistance >= MIN_GAP_DISTANCE && 
                   gapDistance <= MAX_GAP_DISTANCE && 
                   !double.IsNaN(gapDistance) && 
                   !double.IsInfinity(gapDistance);
        }

        /// <summary>
        /// Tries to parse the input string as a valid gap distance
        /// </summary>
        /// <param name="input">The input string to parse</param>
        /// <param name="gapDistance">The parsed gap distance if successful</param>
        /// <returns>True if parsing was successful and value is valid, false otherwise</returns>
        public static bool TryParseGapDistance(string input, out double gapDistance)
        {
            gapDistance = 0.0;

            if (!IsValidNumeric(input))
                return false;

            if (!double.TryParse(input.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out gapDistance))
                return false;

            return IsValidGapDistance(gapDistance);
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

            if (double.TryParse(input.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                if (value < MIN_GAP_DISTANCE)
                    return $"Gap distance must be at least {MIN_GAP_DISTANCE} mm.";

                if (value > MAX_GAP_DISTANCE)
                    return $"Gap distance cannot exceed {MAX_GAP_DISTANCE} mm.";

                if (double.IsNaN(value) || double.IsInfinity(value))
                    return "Gap distance must be a finite number.";
            }

            return "Invalid gap distance value.";
        }

        /// <summary>
        /// Validates if a character is allowed for numeric input
        /// </summary>
        /// <param name="character">The character to validate</param>
        /// <param name="currentText">The current text in the input field</param>
        /// <returns>True if the character is allowed, false otherwise</returns>
        public static bool IsValidNumericCharacter(char character, string currentText)
        {
            // Allow digits
            if (char.IsDigit(character))
                return true;

            // Allow decimal point only if there isn't one already
            if (character == '.' || character == ',')
            {
                return !currentText.Contains(".") && !currentText.Contains(",");
            }

            // Allow control characters (backspace, delete, etc.)
            if (char.IsControl(character))
                return true;

            return false;
        }

        /// <summary>
        /// Sanitizes numeric input by removing invalid characters
        /// </summary>
        /// <param name="input">The input string to sanitize</param>
        /// <returns>Sanitized input string</returns>
        public static string SanitizeNumericInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Replace comma with dot for decimal separator
            input = input.Replace(',', '.');

            // Remove all non-numeric characters except decimal point
            var sanitized = Regex.Replace(input, @"[^0-9.]", "");

            // Ensure only one decimal point
            var parts = sanitized.Split('.');
            if (parts.Length > 2)
            {
                sanitized = parts[0] + "." + string.Join("", parts, 1, parts.Length - 1);
            }

            return sanitized;
        }
    }
}
