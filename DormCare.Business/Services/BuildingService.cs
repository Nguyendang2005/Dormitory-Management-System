using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DormCare.Business.DTOs;
using DormCare.Business.Validators;
using DormCare.DataAccess.Repositories;
using DormCare.Domain.Entities;

namespace DormCare.Business.Services
{
    public class BuildingService
    {
        private readonly BuildingRepository _buildingRepository;

        public event EventHandler? BuildingUpdated;

        public BuildingService(BuildingRepository buildingRepository)
        {
            _buildingRepository = buildingRepository;
        }

        public static string SanitizeBuildingName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            string clean = name;
            while (clean.Contains("(Đã cập nhật)"))
            {
                clean = clean.Replace("(Đã cập nhật)", "").Trim();
            }
            return clean;
        }

        public async Task<IEnumerable<BuildingDto>> GetAllBuildingsAsync()
        {
            var buildings = await _buildingRepository.GetBuildingsWithRoomsAsync();
            return buildings.Select(b =>
            {
                int totalBeds = b.Rooms.Sum(r => r.Beds.Count > 0 ? r.Beds.Count : r.Capacity);
                int occupiedBeds = b.Rooms.SelectMany(r => r.Beds).Count(bed => bed.Status == "Occupied");
                int maintenanceBeds = b.Rooms.SelectMany(r => r.Beds).Count(bed => bed.Status == "Maintenance");

                return new BuildingDto
                {
                    BuildingId = b.BuildingId,
                    BuildingCode = b.BuildingCode,
                    BuildingName = SanitizeBuildingName(b.BuildingName),
                    Address = b.Address,
                    NumberOfFloors = b.NumberOfFloors,
                    Description = b.Description ?? string.Empty,
                    Status = b.Status,
                    TotalRooms = b.Rooms.Count,
                    TotalBeds = totalBeds,
                    OccupiedBeds = occupiedBeds,
                    MaintenanceBeds = maintenanceBeds
                };
            });
        }

        public async Task<BuildingDetailDto?> GetBuildingDetailAsync(int buildingId)
        {
            var b = await _buildingRepository.GetBuildingDetailWithRoomsAsync(buildingId);
            if (b == null) return null;

            int totalBeds = b.Rooms.Sum(r => r.Beds.Count > 0 ? r.Beds.Count : r.Capacity);
            int occupiedBeds = b.Rooms.SelectMany(r => r.Beds).Count(bed => bed.Status == "Occupied");
            int maintenanceBeds = b.Rooms.SelectMany(r => r.Beds).Count(bed => bed.Status == "Maintenance");

            var detail = new BuildingDetailDto
            {
                BuildingId = b.BuildingId,
                BuildingCode = b.BuildingCode,
                BuildingName = b.BuildingName,
                Address = b.Address,
                NumberOfFloors = b.NumberOfFloors,
                Description = b.Description ?? string.Empty,
                Status = b.Status,
                TotalRooms = b.Rooms.Count,
                TotalBeds = totalBeds,
                OccupiedBeds = occupiedBeds,
                MaintenanceBeds = maintenanceBeds,
                Rooms = b.Rooms.Select(r => new BuildingRoomSummaryDto
                {
                    RoomId = r.RoomId,
                    RoomNumber = r.RoomNumber,
                    FloorNumber = r.FloorNumber,
                    Capacity = r.Capacity,
                    OccupiedBeds = r.Beds.Count(bed => bed.Status == "Occupied"),
                    Status = r.Status
                }).OrderBy(r => r.FloorNumber).ThenBy(r => r.RoomNumber).ToList()
            };

            return detail;
        }

        public async Task<ServiceResult<bool>> AddBuildingAsync(Building building)
        {
            // Centralized Validation
            var (isValid, validationMessage) = BuildingValidator.Validate(building);
            if (!isValid)
            {
                return ServiceResult<bool>.Failure(validationMessage);
            }

            building.BuildingCode = building.BuildingCode.Trim().ToUpper();
            building.BuildingName = building.BuildingName.Trim();
            building.Address = building.Address.Trim();
            building.Description = building.Description?.Trim();

            // Duplicate BuildingCode Check
            var existing = await _buildingRepository.GetBuildingsWithRoomsAsync();
            if (existing.Any(b => b.BuildingCode.Equals(building.BuildingCode, StringComparison.OrdinalIgnoreCase)))
            {
                return ServiceResult<bool>.Failure($"❌ Mã tòa nhà '{building.BuildingCode}' đã tồn tại.\nVui lòng sử dụng mã khác.");
            }

            try
            {
                await _buildingRepository.AddAsync(building);
                await _buildingRepository.SaveChangesAsync();

                BuildingUpdated?.Invoke(this, EventArgs.Empty);
                return ServiceResult<bool>.Success(true, $"✓ Tòa nhà '{building.BuildingName}' đã được thêm thành công.");
            }
            catch (DbUpdateException)
            {
                return ServiceResult<bool>.Failure($"Lỗi cơ sở dữ liệu: Mã tòa nhà '{building.BuildingCode}' đã tồn tại trong hệ thống.");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"Không thể thêm tòa nhà: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> UpdateBuildingAsync(Building building)
        {
            var entity = await _buildingRepository.GetByIdAsync(building.BuildingId);
            if (entity == null) return ServiceResult<bool>.Failure("Tòa nhà không tồn tại.");

            // Preserve read-only BuildingCode
            building.BuildingCode = entity.BuildingCode;

            // Validate inputs
            var (isValid, validationMessage) = BuildingValidator.Validate(building);
            if (!isValid)
            {
                return ServiceResult<bool>.Failure(validationMessage);
            }

            // Floor decrease business rule check
            int maxFloorWithRooms = await _buildingRepository.GetMaxFloorWithRoomsAsync(building.BuildingId);
            if (building.NumberOfFloors < maxFloorWithRooms)
            {
                return ServiceResult<bool>.Failure(
                    $"Không thể giảm số tầng xuống {building.NumberOfFloors}.\n" +
                    $"Tòa nhà hiện đang có phòng ở tầng {maxFloorWithRooms}.\n" +
                    $"Số tầng mới phải lớn hơn hoặc bằng tầng cao nhất đang có phòng.");
            }

            entity.BuildingName = SanitizeBuildingName(building.BuildingName);
            entity.Address = building.Address.Trim();
            entity.NumberOfFloors = building.NumberOfFloors;
            entity.Description = building.Description?.Trim();
            entity.Status = building.Status;
            entity.UpdatedAt = DateTime.UtcNow;

            await _buildingRepository.UpdateAsync(entity);
            await _buildingRepository.SaveChangesAsync();

            BuildingUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<bool>.Success(true, "Cập nhật thông tin tòa nhà thành công!");
        }

        public async Task<ServiceResult<BuildingDeleteResult>> DeleteBuildingAsync(int buildingId)
        {
            var building = await _buildingRepository.GetByIdAsync(buildingId);
            if (building == null)
            {
                return ServiceResult<BuildingDeleteResult>.Failure("Tòa nhà không tồn tại.");
            }

            var (totalRooms, totalBeds, residingStudents, _) = await _buildingRepository.GetBuildingUsageAsync(buildingId);

            if (totalRooms > 0 || totalBeds > 0 || residingStudents > 0)
            {
                var blockedResult = new BuildingDeleteResult
                {
                    CanDelete = false,
                    TotalRooms = totalRooms,
                    TotalBeds = totalBeds,
                    ResidingStudents = residingStudents,
                    Message = $"Không thể xóa tòa nhà '{building.BuildingName}'. Tòa nhà đang có dữ liệu liên quan."
                };
                return ServiceResult<BuildingDeleteResult>.Success(blockedResult, "Xóa tòa nhà bị chặn do có dữ liệu liên quan.");
            }

            await _buildingRepository.DeleteAsync(building);
            await _buildingRepository.SaveChangesAsync();

            BuildingUpdated?.Invoke(this, EventArgs.Empty);

            var successResult = new BuildingDeleteResult
            {
                CanDelete = true,
                Message = $"Tòa nhà '{building.BuildingName}' đã được xóa thành công."
            };
            return ServiceResult<BuildingDeleteResult>.Success(successResult, successResult.Message);
        }

        public async Task<ServiceResult<bool>> DeactivateBuildingAsync(int buildingId)
        {
            var entity = await _buildingRepository.GetByIdAsync(buildingId);
            if (entity == null) return ServiceResult<bool>.Failure("Tòa nhà không tồn tại.");

            entity.Status = "Inactive";
            entity.UpdatedAt = DateTime.UtcNow;

            await _buildingRepository.UpdateAsync(entity);
            await _buildingRepository.SaveChangesAsync();

            BuildingUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<bool>.Success(true, $"Đã chuyển tòa nhà '{entity.BuildingName}' sang trạng thái Inactive thành công.");
        }
    }
}
