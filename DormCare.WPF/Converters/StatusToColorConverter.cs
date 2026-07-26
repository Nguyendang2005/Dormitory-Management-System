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
                "Available" or "Paid" or "Approved" or "Completed" or "Empty" => new SolidColorBrush(Color.FromRgb(46, 204, 113)), // Emerald Green
                "Pending" or "InProgress" => new SolidColorBrush(Color.FromRgb(241, 196, 15)), // Sunflower Yellow
                "Full" or "Unpaid" or "Rejected" or "Occupied" => new SolidColorBrush(Color.FromRgb(231, 76, 60)), // Alizarin Red
                "Overdue" or "Maintenance" or "Disabled" => new SolidColorBrush(Color.FromRgb(230, 126, 34)), // Orange
                _ => new SolidColorBrush(Color.FromRgb(149, 165, 166)) // Gray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
