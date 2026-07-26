using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DormCare.WPF.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasText = value is string str && !string.IsNullOrWhiteSpace(str);
            if (Invert) hasText = !hasText;

            return hasText ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
