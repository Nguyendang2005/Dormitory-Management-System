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

            // Đếm sinh viên đang ở từ active RoomAssignments
            int totalResidents = b.Rooms
                .SelectMany(r => r.Beds)
                .SelectMany(bed => bed.RoomAssignments)
                .Count(ra => ra.Status == "Active");

            // Build AllResidents (toàn bộ sinh viên trong tòa)
            var allResidents = b.Rooms
                .SelectMany(r => r.Beds.SelectMany(bed => bed.RoomAssignments
                    .Where(ra => ra.Status == "Active")
                    .Select(ra => new BuildingResidentDto
                    {
                        AssignmentId = ra.AssignmentId,
                        StudentId = ra.StudentId,
                        StudentCode = ra.Student?.StudentCode ?? string.Empty,
                        FullName = ra.Student?.FullName ?? string.Empty,
                        RoomNumber = r.RoomNumber,
                        BedCode = bed.BedCode,
                        FloorNumber = r.FloorNumber,
                        StartDate = ra.StartDate,
                        AssignedByName = ra.Manager?.Username ?? string.Empty
                    })))
                .OrderBy(x => x.FloorNumber).ThenBy(x => x.RoomNumber).ThenBy(x => x.BedCode)
                .ToList();

            var rooms = b.Rooms.Select(r =>
            {
                int rOccupied = r.Beds.Count(bed => bed.Status == "Occupied");
                int rMaintenance = r.Beds.Count(bed => bed.Status == "Maintenance");

                var residents = r.Beds.SelectMany(bed => bed.RoomAssignments
                    .Where(ra => ra.Status == "Active")
                    .Select(ra => new BuildingResidentDto
                    {
                        AssignmentId = ra.AssignmentId,
                        StudentId = ra.StudentId,
                        StudentCode = ra.Student?.StudentCode ?? string.Empty,
                        FullName = ra.Student?.FullName ?? string.Empty,
                        RoomNumber = r.RoomNumber,
                        BedCode = bed.BedCode,
                        FloorNumber = r.FloorNumber,
                        StartDate = ra.StartDate,
                        AssignedByName = ra.Manager?.Username ?? string.Empty
                    })).ToList();

                return new BuildingRoomSummaryDto
                {
                    RoomId = r.RoomId,
                    RoomNumber = r.RoomNumber,
                    FloorNumber = r.FloorNumber,
                    Capacity = r.Capacity,
                    OccupiedBeds = rOccupied,
                    MaintenanceBeds = rMaintenance,
                    Status = r.Status,
                    CurrentResidents = residents
                };
            })
            .OrderBy(r => r.FloorNumber).ThenBy(r => r.RoomNumber)
            .ToList();

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
                TotalResidents = totalResidents,
                Rooms = rooms,
                AllResidents = allResidents
            };

            return detail;
        }

        /// <summary>Lấy danh sách sinh viên đang cư trú trong tòa nhà (active).</summary>
        public async Task<List<BuildingResidentDto>> GetBuildingResidentsAsync(int buildingId)
        {
            var assignments = await _buildingRepository.GetBuildingActiveResidentsAsync(buildingId);
            return assignments.Select(ra => new BuildingResidentDto
            {
                AssignmentId = ra.AssignmentId,
                StudentId = ra.StudentId,
                StudentCode = ra.Student?.StudentCode ?? string.Empty,
                FullName = ra.Student?.FullName ?? string.Empty,
                RoomNumber = ra.Room?.RoomNumber ?? string.Empty,
                BedCode = ra.Bed?.BedCode ?? string.Empty,
                FloorNumber = ra.Room?.FloorNumber ?? 0,
                StartDate = ra.StartDate,
                AssignedByName = ra.Manager?.Username ?? string.Empty
            }).ToList();
        }

        public async Task<ServiceResult<bool>> AddBuildingAsync(Building building)
        {
            var (isValid, validationMessage) = BuildingValidator.Validate(building);
            if (!isValid)
            {
                return ServiceResult<bool>.Failure(validationMessage);
            }

            building.BuildingCode = building.BuildingCode.Trim().ToUpper();
            building.BuildingName = building.BuildingName.Trim();
            building.Address = building.Address.Trim();
            building.Description = building.Description?.Trim();

            var existing = await _buildingRepository.GetBuildingsWithRoomsAsync();
            if (existing.Any(b => b.BuildingCode.Equals(building.BuildingCode, StringComparison.OrdinalIgnoreCase)))
            {
                return ServiceResult<bool>.Failure($"❌ Mã tòa nhà '{building.BuildingCode}' đã tồn tại.\nVui lòng sử dụng mã khác.");
            }

            if (existing.Any(b => b.BuildingName.Equals(building.BuildingName, StringComparison.OrdinalIgnoreCase)))
            {
                return ServiceResult<bool>.Failure($"❌ Tên tòa nhà '{building.BuildingName}' đã tồn tại trong hệ thống.\nVui lòng sử dụng tên khác.");
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

            building.BuildingCode = entity.BuildingCode;

            var (isValid, validationMessage) = BuildingValidator.Validate(building);
            if (!isValid)
            {
                return ServiceResult<bool>.Failure(validationMessage);
            }

            var allBuildings = await _buildingRepository.GetBuildingsWithRoomsAsync();
            string sanitizedNewName = SanitizeBuildingName(building.BuildingName);

            if (allBuildings.Any(b => b.BuildingId != building.BuildingId && b.BuildingName.Equals(sanitizedNewName, StringComparison.OrdinalIgnoreCase)))
            {
                return ServiceResult<bool>.Failure($"❌ Tên tòa nhà '{sanitizedNewName}' đã trùng với một tòa nhà khác.");
            }

            int maxFloorWithRooms = await _buildingRepository.GetMaxFloorWithRoomsAsync(building.BuildingId);
            if (building.NumberOfFloors < maxFloorWithRooms)
            {
                return ServiceResult<bool>.Failure(
                    $"Không thể giảm số tầng xuống {building.NumberOfFloors}.\n" +
                    $"Tòa nhà hiện đang có phòng ở tầng {maxFloorWithRooms}.\n" +
                    $"Số tầng mới phải lớn hơn hoặc bằng tầng cao nhất đang có phòng.");
            }

            // Status transition check
            if (building.Status != "Active")
            {
                var (_, _, residingStudents, _) = await _buildingRepository.GetBuildingUsageAsync(building.BuildingId);
                if (residingStudents > 0)
                {
                    return ServiceResult<bool>.Failure(
                        $"❌ Không thể chuyển tòa nhà sang trạng thái '{building.Status}' vì đang có {residingStudents} sinh viên đang cư trú.\n" +
                        $"Vui lòng di chuyển hoặc trả phòng cho sinh viên trước khi đổi trạng thái tòa nhà.");
                }
            }

            entity.BuildingName = sanitizedNewName;
            entity.Address = building.Address.Trim();
            entity.NumberOfFloors = building.NumberOfFloors;
            entity.Description = building.Description?.Trim();
            entity.Status = building.Status;
            entity.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _buildingRepository.UpdateAsync(entity);
                await _buildingRepository.SaveChangesAsync();

                BuildingUpdated?.Invoke(this, EventArgs.Empty);
                return ServiceResult<bool>.Success(true, "Cập nhật thông tin tòa nhà thành công!");
            }
            catch (DbUpdateException ex)
            {
                return ServiceResult<bool>.Failure($"Lỗi cơ sở dữ liệu khi cập nhật tòa nhà: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"Không thể cập nhật tòa nhà: {ex.Message}");
            }
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

            try
            {
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
            catch (DbUpdateException)
            {
                return ServiceResult<BuildingDeleteResult>.Failure($"Không thể xóa tòa nhà '{building.BuildingName}' do ràng buộc khóa ngoại (FK_Rooms_Buildings) trong CSDL SQL Server.");
            }
        }

        public async Task<ServiceResult<bool>> DeactivateBuildingAsync(int buildingId)
        {
            var entity = await _buildingRepository.GetByIdAsync(buildingId);
            if (entity == null) return ServiceResult<bool>.Failure("Tòa nhà không tồn tại.");

            var (_, _, residingStudents, _) = await _buildingRepository.GetBuildingUsageAsync(buildingId);
            if (residingStudents > 0)
            {
                return ServiceResult<bool>.Failure($"❌ Không thể chuyển tòa nhà '{entity.BuildingName}' sang trạng thái Inactive vì đang có {residingStudents} sinh viên đang cư trú.\nVui lòng chuyển phòng hoặc check-out toàn bộ sinh viên trước khi vô hiệu hóa.");
            }

            entity.Status = "Inactive";
            entity.UpdatedAt = DateTime.UtcNow;

            await _buildingRepository.UpdateAsync(entity);
            await _buildingRepository.SaveChangesAsync();

            BuildingUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<bool>.Success(true, $"Đã chuyển tòa nhà '{entity.BuildingName}' sang trạng thái Inactive thành công.");
        }
    }
}
