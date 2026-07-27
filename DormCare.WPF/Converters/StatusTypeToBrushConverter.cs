using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DormCare.WPF.ViewModels;

namespace DormCare.WPF.Converters
{
    /// <summary>Chuyển StatusType thành màu nền cho banner thông báo inline.</summary>
    public class StatusTypeToBgBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is StatusType st ? st switch
            {
                StatusType.Error   => new SolidColorBrush(Color.FromRgb(0xFE, 0xE2, 0xE2)), // #FEE2E2
                StatusType.Success => new SolidColorBrush(Color.FromRgb(0xDC, 0xFC, 0xE7)), // #DCFCE7
                StatusType.Warning => new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xC7)), // #FEF3C7
                StatusType.Info    => new SolidColorBrush(Color.FromRgb(0xE0, 0xF2, 0xFE)), // #E0F2FE
                _                  => new SolidColorBrush(Colors.Transparent)
            } : new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Chuyển StatusType thành màu border cho banner thông báo inline.</summary>
    public class StatusTypeToBorderBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is StatusType st ? st switch
            {
                StatusType.Error   => new SolidColorBrush(Color.FromRgb(0xFC, 0xA5, 0xA5)), // #FCA5A5
                StatusType.Success => new SolidColorBrush(Color.FromRgb(0x86, 0xEF, 0xAC)), // #86EFAC
                StatusType.Warning => new SolidColorBrush(Color.FromRgb(0xFD, 0xD8, 0x35)), // #FDD835
                StatusType.Info    => new SolidColorBrush(Color.FromRgb(0x7D, 0xD3, 0xFC)), // #7DD3FC
                _                  => new SolidColorBrush(Colors.Transparent)
            } : new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Chuyển StatusType thành màu chữ cho banner thông báo inline.</summary>
    public class StatusTypeToFgBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is StatusType st ? st switch
            {
                StatusType.Error   => new SolidColorBrush(Color.FromRgb(0xB9, 0x1C, 0x1C)), // #B91C1C
                StatusType.Success => new SolidColorBrush(Color.FromRgb(0x15, 0x80, 0x3D)), // #15803D
                StatusType.Warning => new SolidColorBrush(Color.FromRgb(0x92, 0x40, 0x08)), // #924008
                StatusType.Info    => new SolidColorBrush(Color.FromRgb(0x07, 0x5D, 0x8A)), // #075D8A
                _                  => new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A))
            } : new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
