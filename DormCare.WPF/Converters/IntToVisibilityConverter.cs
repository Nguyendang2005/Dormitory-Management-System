using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace DormCare.WPF.Converters
{
    /// <summary>
    /// Converter: ẩn/hiện một panel dựa trên giá trị int.
    /// Dùng trong tab navigation — panel chỉ hiện khi giá trị binding == TargetValue.
    /// 
    /// Sử dụng như MarkupExtension để dùng trực tiếp trong XAML inline:
    ///   Visibility="{Binding ActiveTab, Converter={conv:IntToVisibilityConverter TargetValue=0}}"
    ///   Hoặc dùng trong Binding.Converter theo kiểu inline object.
    /// </summary>
    public class IntToVisibilityConverter : MarkupExtension, IValueConverter
    {
        public int TargetValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intVal && intVal == TargetValue)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        public override object ProvideValue(IServiceProvider serviceProvider)
            => this;
    }
}
