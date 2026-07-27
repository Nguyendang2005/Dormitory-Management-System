using System;
using System.Text.RegularExpressions;
using DormCare.Domain.Entities;

namespace DormCare.Business.Validators
{
    public static class BuildingValidator
    {
        private static readonly Regex CodeRegex = new(@"^[a-zA-Z0-9][a-zA-Z0-9_-]*$", RegexOptions.Compiled);
        private static readonly Regex HasLetterRegex = new(@"\p{L}", RegexOptions.Compiled);

        public static (bool IsValid, string Message) Validate(Building building)
        {
            if (building == null)
            {
                return (false, "Thông tin tòa nhà không được để trống.");
            }

            // 1. BuildingCode validation
            if (string.IsNullOrWhiteSpace(building.BuildingCode))
            {
                return (false, "Mã tòa nhà không được để trống.");
            }

            var trimmedCode = building.BuildingCode.Trim();
            if (trimmedCode.Length < 1 || trimmedCode.Length > 20)
            {
                return (false, "Mã tòa nhà phải từ 1 đến 20 ký tự.");
            }

            if (!CodeRegex.IsMatch(trimmedCode))
            {
                return (false, "Mã tòa nhà phải bắt đầu bằng chữ cái hoặc chữ số, chỉ được chứa chữ cái, chữ số, dấu gạch ngang '-' hoặc gạch dưới '_'. Không chứa khoảng trắng hoặc ký tự đặc biệt.");
            }

            // 2. BuildingName validation
            if (string.IsNullOrWhiteSpace(building.BuildingName))
            {
                return (false, "Tên tòa nhà không được để trống.");
            }

            var trimmedName = building.BuildingName.Trim();
            if (trimmedName.Length < 2 || trimmedName.Length > 100)
            {
                return (false, "Tên tòa nhà phải từ 2 đến 100 ký tự.");
            }

            if (!HasLetterRegex.IsMatch(trimmedName))
            {
                return (false, "Tên tòa nhà phải chứa ít nhất một chữ cái hợp lệ (không được chỉ gồm toàn chữ số hoặc ký tự đặc biệt).");
            }

            // 3. Address validation
            if (string.IsNullOrWhiteSpace(building.Address))
            {
                return (false, "Địa chỉ tòa nhà không được để trống.");
            }

            var trimmedAddress = building.Address.Trim();
            if (trimmedAddress.Length < 5 || trimmedAddress.Length > 255)
            {
                return (false, "Địa chỉ tòa nhà phải từ 5 đến 255 ký tự.");
            }

            if (!HasLetterRegex.IsMatch(trimmedAddress))
            {
                return (false, "Địa chỉ tòa nhà phải chứa nội dung chữ cái rõ ràng.");
            }

            // 4. NumberOfFloors validation
            if (building.NumberOfFloors < 1 || building.NumberOfFloors > 100)
            {
                return (false, "Số tầng tòa nhà phải là số nguyên từ 1 đến 100.");
            }

            // 5. Status validation
            string status = building.Status?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(status))
            {
                return (false, "Trạng thái tòa nhà không được để trống.");
            }

            if (status != "Active" && status != "Inactive" && status != "Maintenance")
            {
                return (false, "Trạng thái tòa nhà chỉ chấp nhận một trong các giá trị: Active, Inactive, Maintenance.");
            }

            // 6. Description validation
            if (!string.IsNullOrEmpty(building.Description) && building.Description.Trim().Length > 500)
            {
                return (false, "Mô tả tòa nhà không được vượt quá 500 ký tự.");
            }

            return (true, string.Empty);
        }
    }
}
