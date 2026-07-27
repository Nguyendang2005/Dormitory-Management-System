using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DormCare.Business.Services;
using DormCare.DataAccess.Repositories;
using DormCare.Domain.Entities;

namespace DormCare.Business.Validators
{
    public class RoomValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public static RoomValidationResult Success() => new() { IsValid = true };
        public static RoomValidationResult Failure(string message) => new() { IsValid = false, ErrorMessage = message };
    }

    public class RoomValidator
    {
        private readonly BuildingRepository _buildingRepository;
        private readonly RoomRepository _roomRepository;

        public RoomValidator(BuildingRepository buildingRepository, RoomRepository roomRepository)
        {
            _buildingRepository = buildingRepository;
            _roomRepository = roomRepository;
        }

        public async Task<RoomValidationResult> ValidateAsync(Room room, bool isEdit = false, int currentOccupiedBeds = 0, string currentGenderType = "")
        {
            if (room == null)
                return RoomValidationResult.Failure("Thông tin phòng không hợp lệ.");

            // 1. Building validation
            if (room.BuildingId <= 0)
                return RoomValidationResult.Failure("Vui lòng chọn tòa nhà.");

            var building = await _buildingRepository.GetByIdAsync(room.BuildingId);
            if (building == null)
                return RoomValidationResult.Failure("Tòa nhà được chọn không tồn tại trong hệ thống.");

            if (!isEdit && building.Status != "Active")
            {
                return RoomValidationResult.Failure($"Không thể thêm phòng vào tòa nhà đang '{building.Status}'. Vui lòng chọn một tòa nhà đang Active.");
            }

            // 2. RoomNumber validation
            string roomNumber = room.RoomNumber?.Trim().ToUpper() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(roomNumber))
                return RoomValidationResult.Failure("Số phòng không được để trống.");

            if (roomNumber.Length < 2 || roomNumber.Length > 10)
                return RoomValidationResult.Failure("Số phòng phải từ 2 đến 10 ký tự.");

            if (!Regex.IsMatch(roomNumber, @"^[a-zA-Z0-9-]+$"))
                return RoomValidationResult.Failure("Số phòng chỉ được chứa chữ cái (A-Z, a-z), chữ số (0-9) và dấu '-'. không chứa ký tự đặc biệt hoặc khoảng trắng.");

            // Uniqueness check within the same building
            var allRooms = await _roomRepository.GetRoomsWithBuildingAndBedsAsync();
            bool isDuplicate = allRooms.Any(r => r.BuildingId == room.BuildingId &&
                                                  r.RoomNumber.Equals(roomNumber, StringComparison.OrdinalIgnoreCase) &&
                                                  r.RoomId != room.RoomId);
            if (isDuplicate)
            {
                return RoomValidationResult.Failure($"Số phòng '{roomNumber}' đã tồn tại trong tòa nhà '{building.BuildingName}'. Vui lòng nhập số phòng khác.");
            }

            // 3. FloorNumber validation
            if (room.FloorNumber < 1 || room.FloorNumber > building.NumberOfFloors)
            {
                return RoomValidationResult.Failure($"Số tầng phải nằm trong khoảng từ 1 đến {building.NumberOfFloors} (theo quy mô tòa nhà '{building.BuildingName}').");
            }

            // 4. RoomType validation
            string roomType = room.RoomType?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(roomType))
                return RoomValidationResult.Failure("Vui lòng chọn loại phòng.");

            // 5. Capacity validation
            if (room.Capacity < 1 || room.Capacity > 20)
                return RoomValidationResult.Failure("Sức chứa phòng phải nằm trong khoảng từ 1 đến 20 giường.");

            if (isEdit && room.Capacity < currentOccupiedBeds)
            {
                return RoomValidationResult.Failure($"Không thể giảm sức chứa xuống {room.Capacity}. Phòng hiện đang có {currentOccupiedBeds} sinh viên đang cư trú.");
            }

            // 6. MonthlyRent validation
            if (room.MonthlyRent <= 0)
                return RoomValidationResult.Failure("Giá thuê phòng phải lớn hơn 0 VNĐ.");

            if (room.MonthlyRent > 999999999m)
                return RoomValidationResult.Failure("Giá thuê phòng vượt quá hạn mức cho phép (Tối đa 999,999,999 VNĐ).");

            // 7. GenderType validation
            string genderType = room.GenderType?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(genderType))
                return RoomValidationResult.Failure("Vui lòng chọn đối tượng cư trú.");

            if (isEdit && currentOccupiedBeds > 0 && !currentGenderType.Equals(genderType, StringComparison.OrdinalIgnoreCase))
            {
                return RoomValidationResult.Failure($"Không thể thay đổi đối tượng cư trú ({currentGenderType} → {genderType}) khi phòng đang có {currentOccupiedBeds} sinh viên đang ở. Vui lòng check-out sinh viên trước.");
            }

            // 8. Description length check
            if (room.Description != null && room.Description.Length > 500)
                return RoomValidationResult.Failure("Mô tả phòng không được vượt quá 500 ký tự.");

            return RoomValidationResult.Success();
        }
    }
}
