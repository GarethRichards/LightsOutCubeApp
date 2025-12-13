using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace LightsOutCube.Converters
{
    public class MinValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var doubles = values?.Where(v => v != null).Select(v => System.Convert.ToDouble(v)).ToArray();
                if (doubles == null || doubles.Length == 0) return Binding.DoNothing;
                var min = doubles.Min();
                return min;
            }
            catch { return Binding.DoNothing; }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
