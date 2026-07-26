using System;
using System.Globalization;
using System.Windows.Data;

namespace DormCare.WPF.Converters
{
    public class StatusToVietnameseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            string status = value.ToString() ?? "";
            return status switch
            {
                "Available" => "Còn chỗ",
                "Occupied" => "Đang sử dụng",
                "Maintenance" => "Bảo trì",
                "Inactive" => "Ngừng hoạt động",
                "Full" => "Đã đầy",
                "Reserved" => "Đã đặt chỗ",
                "Active" => "Còn hoạt động",
                "Pending" => "Chờ duyệt",
                "Approved" => "Đã duyệt",
                "Rejected" => "Từ chối",
                "Unpaid" => "Chưa thanh toán",
                "Paid" => "Đã thanh toán",
                "Overdue" => "Quá hạn",
                "Standard" => "Tiêu chuẩn",
                "Premium" => "Cao cấp",
                "Accessible" => "Hỗ trợ",
                "Male" => "Nam",
                "Female" => "Nữ",
                "Mixed" => "Hỗn hợp",
                _ => status
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
