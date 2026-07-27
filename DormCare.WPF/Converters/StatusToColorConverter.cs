using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DormCare.WPF.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value?.ToString() ?? string.Empty;

            return status switch
            {
                "Active" or "Available" or "Paid" or "Approved" or "Completed" or "Empty" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E")), // Vibrant Emerald Green
                "Pending" or "InProgress" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EAB308")), // Gold Yellow
                "Full" or "Unpaid" or "Rejected" or "Occupied" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")), // Crimson Red
                "Overdue" or "Maintenance" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")), // Amber Orange
                "Inactive" or "Disabled" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")), // Muted Slate Gray
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"))
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
