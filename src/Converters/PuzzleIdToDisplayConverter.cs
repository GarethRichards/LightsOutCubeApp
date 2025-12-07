using System;
using System.Globalization;
using System.Windows.Data;

namespace LightsOutCube.Converters
{
    public class PuzzleIdToDisplayConverter : IValueConverter
    {
        // If value is 0 (the sentinel) show "Speed Run", otherwise "Puzzle N"
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int id)
            {
                return id == 0 ? "Speed Run" : $"Puzzle {id}";
            }
            return value?.ToString() ?? string.Empty;
        }

        // ConvertBack not used by the ComboBox item template, but implement defensively
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            if (string.IsNullOrEmpty(s)) return 0;
            if (s.Equals("Speed Run", StringComparison.OrdinalIgnoreCase)) return 0;
            if (s.StartsWith("Puzzle ", StringComparison.OrdinalIgnoreCase) && int.TryParse(s.Substring(7), out var n))
                return n;
            if (int.TryParse(s, out var v)) return v;
            return 0;
        }
    }
}