using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LightsOutCube.Converters
{
    /// <summary>
    /// Converts an integer collection count to Visibility. Returns Visible when count == 0, otherwise Collapsed.
    /// Intended for "No items" text blocks.
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i)
            {
                return i == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            // fallback: if null or unexpected type, hide
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
