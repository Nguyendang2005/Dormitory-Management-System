using System;
using System.Text.RegularExpressions;
using DormCare.Domain.Entities;

namespace DormCare.Business.Validators
{
    public static class BuildingValidator
    {
        private static readonly Regex CodeRegex = new(@"^[a-zA-Z0-9-]+$", RegexOptions.Compiled);

        public static (bool IsValid, string Message) Validate(Building building)
        {
            // BuildingCode validation
            if (string.IsNullOrWhiteSpace(building.BuildingCode))
            {
                return (false, "Mã tòa nhà không được để trống.");
            }

            var trimmedCode = building.BuildingCode.Trim();
            if (trimmedCode.Length < 1 || trimmedCode.Length > 20 || !CodeRegex.IsMatch(trimmedCode))
            {
                return (false, "Mã tòa nhà từ 1 đến 20 ký tự và chỉ được chứa chữ cái, số hoặc dấu gạch ngang.");
            }

            // BuildingName validation
            if (string.IsNullOrWhiteSpace(building.BuildingName))
            {
                return (false, "Tên tòa nhà không được để trống.");
            }

            var trimmedName = building.BuildingName.Trim();
            if (trimmedName.Length < 2 || trimmedName.Length > 100)
            {
                return (false, "Tên tòa nhà phải từ 2 đến 100 ký tự.");
            }

            // Address validation
            if (string.IsNullOrWhiteSpace(building.Address))
            {
                return (false, "Địa chỉ không được để trống.");
            }

            var trimmedAddress = building.Address.Trim();
            if (trimmedAddress.Length > 255)
            {
                return (false, "Địa chỉ không được vượt quá 255 ký tự.");
            }

            // NumberOfFloors validation
            if (building.NumberOfFloors < 1 || building.NumberOfFloors > 100)
            {
                return (false, "Số tầng phải là số nguyên từ 1 đến 100.");
            }

            // Description validation
            if (!string.IsNullOrEmpty(building.Description) && building.Description.Length > 500)
            {
                return (false, "Mô tả không được vượt quá 500 ký tự.");
            }

            return (true, string.Empty);
        }
    }
}
