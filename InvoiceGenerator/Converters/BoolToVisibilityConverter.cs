using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace InvoiceGenerator.Converters
{
    /// <summary>
    /// Converts a <see cref="bool"/> to <see cref="Visibility"/>.
    /// <c>true</c> → <see cref="Visibility.Visible"/>, <c>false</c> → <see cref="Visibility.Collapsed"/>.
    /// Pass "Inverse" as the converter parameter to invert.
    /// </summary>
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var flag = value is bool b && b;
            var invert = "Inverse".Equals(parameter as string, StringComparison.OrdinalIgnoreCase);
            if (invert) flag = !flag;
            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isVisible = value is Visibility v && v == Visibility.Visible;
            var invert = "Inverse".Equals(parameter as string, StringComparison.OrdinalIgnoreCase);
            return invert ? !isVisible : isVisible;
        }
    }
}
