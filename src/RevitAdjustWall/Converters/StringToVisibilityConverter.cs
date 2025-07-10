using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RevitAdjustWall.Converters;

/// <summary>
/// Converter that converts string values to Visibility
/// Empty or null strings become Collapsed, non-empty strings become Visible
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            return string.IsNullOrEmpty(stringValue) ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("StringToVisibilityConverter does not support ConvertBack");
    }
}