using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DormCare.Business.DTOs;
using DormCare.Business.Validators;
using DormCare.DataAccess.Repositories;
using DormCare.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DormCare.Business.Services
{
    public class RoomOccupancyStats
    {
        public int TotalRoomsCount { get; set; }
        public int TotalBedsCount { get; set; }
        public int AvailableRoomsCount { get; set; }
    }

    public class RoomService
    {
        private readonly RoomRepository _roomRepository;
        private readonly BuildingRepository _buildingRepository;
        private readonly RoomValidator _roomValidator;

        public event EventHandler? RoomUpdated;

        public RoomService(RoomRepository roomRepository, BuildingRepository buildingRepository)
        {
            _roomRepository = roomRepository;
            _buildingRepository = buildingRepository;
            _roomValidator = new RoomValidator(buildingRepository, roomRepository);
        }

        public async Task<IEnumerable<RoomDto>> GetAllRoomsAsync()
        {
            var rooms = await _roomRepository.GetRoomsWithBuildingAndBedsAsync();
            return rooms.Select(MapToDto);
        }

        public async Task<IEnumerable<RoomDto>> GetAvailableRoomsAsync()
        {
            var rooms = await _roomRepository.GetRoomsWithBuildingAndBedsAsync();
            return rooms
                .Where(r => r.Status == "Available" && (r.Beds.Count == 0 || r.Beds.Any(b => b.Status == "Available")))
                .Select(MapToDto);
        }

        public async Task<RoomOccupancyStats> GetOccupancyStatsAsync()
        {
            var rooms = await _roomRepository.GetRoomsWithBuildingAndBedsAsync();
            var list = rooms.ToList();

            int totalRooms = list.Count;
            int totalBeds = list.Sum(r => r.Beds.Count > 0 ? r.Beds.Count : r.Capacity);
            int availableRooms = list.Count(r =>
            {
                int occupied = r.Beds.Count(b => b.Status == "Occupied");
                int maintenance = r.Beds.Count(b => b.Status == "Maintenance");
                int total = r.Beds.Count > 0 ? r.Beds.Count : r.Capacity;
                int available = total - occupied - maintenance;
                return available > 0 && r.Status != "Inactive";
            });

            return new RoomOccupancyStats
            {
                TotalRoomsCount = totalRooms,
                TotalBedsCount = totalBeds,
                AvailableRoomsCount = availableRooms
            };
        }

        public async Task<IEnumerable<RoomDto>> SearchAndFilterRoomsAsync(int? buildingId, string? genderType, string? roomType, string? searchText)
        {
            var rooms = await _roomRepository.GetRoomsWithBuildingAndBedsAsync();
            var query = rooms.AsEnumerable();

            if (buildingId.HasValue && buildingId.Value > 0)
            {
                query = query.Where(r => r.BuildingId == buildingId.Value);
            }

            if (!string.IsNullOrWhiteSpace(genderType) && genderType != "Tất cả" && genderType != "All")
            {
                query = query.Where(r => r.GenderType.Equals(genderType, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(roomType) && roomType != "Tất cả loại" && roomType != "All")
            {
                query = query.Where(r => r.RoomType.Equals(roomType, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string kw = searchText.Trim();
                query = query.Where(r =>
                    r.RoomNumber.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    (r.Building != null && (r.Building.BuildingName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                                            r.Building.BuildingCode.Contains(kw, StringComparison.OrdinalIgnoreCase))));
            }

            return query.Select(MapToDto);
        }

        public async Task<RoomDetailDto?> GetRoomDetailAsync(int roomId)
        {
            var room = await _roomRepository.GetRoomWithDetailsAsync(roomId);
            if (room == null) return null;

            int occupied = room.Beds.Count(b => b.Status == "Occupied");
            int maintenance = room.Beds.Count(b => b.Status == "Maintenance");
            int total = room.Beds.Count > 0 ? room.Beds.Count : room.Capacity;
            int available = Math.Max(0, total - occupied - maintenance);

            var bedsDto = room.Beds.OrderBy(b => b.BedNumber).Select(b =>
            {
                var activeAssign = b.RoomAssignments.FirstOrDefault(ra => ra.Status == "Active");
                return new BedDetailDto
                {
                    BedId = b.BedId,
                    BedNumber = b.BedNumber,
                    BedCode = !string.IsNullOrWhiteSpace(b.BedCode) ? b.BedCode : $"{room.RoomNumber}-{b.BedNumber}",
                    Status = b.Status,
                    StudentName = activeAssign?.Student?.FullName ?? "-",
                    StudentCode = activeAssign?.Student?.StudentCode ?? "-",
                    StartDate = activeAssign?.StartDate,
                    AssignedByName = activeAssign?.Manager?.Username ?? string.Empty
                };
            }).ToList();

            return new RoomDetailDto
            {
                RoomId = room.RoomId,
                BuildingId = room.BuildingId,
                BuildingCode = room.Building?.BuildingCode ?? string.Empty,
                BuildingName = BuildingService.SanitizeBuildingName(room.Building?.BuildingName),
                RoomNumber = room.RoomNumber,
                FloorNumber = room.FloorNumber,
                RoomType = room.RoomType,
                Capacity = room.Capacity,
                MonthlyRent = room.MonthlyRent,
                GenderType = room.GenderType,
                Status = room.Status,
                Description = room.Description ?? string.Empty,
                OccupiedBeds = occupied,
                AvailableBeds = available,
                MaintenanceBeds = maintenance,
                Beds = bedsDto
            };
        }

        public async Task<ServiceResult<Room>> AddRoomAsync(Room room)
        {
            var valResult = await _roomValidator.ValidateAsync(room, isEdit: false);
            if (!valResult.IsValid)
            {
                return ServiceResult<Room>.Failure(valResult.ErrorMessage);
            }

            // Standardize fields
            room.RoomNumber = room.RoomNumber.Trim().ToUpper();
            room.RoomType = room.RoomType.Trim();
            room.GenderType = room.GenderType.Trim();
            room.Description = room.Description?.Trim();
            room.Status = "Available";

            var dbContext = _roomRepository.DbContext;
            var strategy = dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await dbContext.Database.BeginTransactionAsync();
                try
                {
                    await _roomRepository.AddAsync(room);
                    await _roomRepository.SaveChangesAsync();

                    // Automatically generate Capacity number of Beds inside the transaction
                    for (int i = 1; i <= room.Capacity; i++)
                    {
                        var bed = new Bed
                        {
                            RoomId = room.RoomId,
                            BedNumber = $"B{i}",
                            BedCode = $"{room.RoomNumber}-B{i}",
                            Status = "Available",
                            Description = $"Giường số {i} thuộc phòng {room.RoomNumber}"
                        };
                        await dbContext.Set<Bed>().AddAsync(bed);
                    }

                    await dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    RoomUpdated?.Invoke(this, EventArgs.Empty);
                    return ServiceResult<Room>.Success(room, $"Tạo phòng '{room.RoomNumber}' và {room.Capacity} giường thành công!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    string detail = ex.InnerException?.Message ?? ex.Message;
                    return ServiceResult<Room>.Failure($"Lỗi khi tạo phòng và giường vào cơ sở dữ liệu: {detail}");
                }
            });
        }

        public async Task<ServiceResult<bool>> UpdateRoomAsync(Room room)
        {
            var existingRoom = await _roomRepository.GetRoomWithDetailsAsync(room.RoomId);
            if (existingRoom == null)
                return ServiceResult<bool>.Failure("Phòng không tồn tại.");

            int occupiedCount = existingRoom.Beds.Count(b => b.Status == "Occupied");

            var valResult = await _roomValidator.ValidateAsync(room, isEdit: true, currentOccupiedBeds: occupiedCount, currentGenderType: existingRoom.GenderType);
            if (!valResult.IsValid)
            {
                return ServiceResult<bool>.Failure(valResult.ErrorMessage);
            }

            // Update basic fields
            existingRoom.RoomType = room.RoomType.Trim();
            existingRoom.FloorNumber = room.FloorNumber;
            existingRoom.MonthlyRent = room.MonthlyRent;
            existingRoom.GenderType = room.GenderType.Trim();
            existingRoom.Status = room.Status;
            existingRoom.Description = room.Description?.Trim();

            // Capacity change handling
            int oldCapacity = existingRoom.Capacity;
            int newCapacity = room.Capacity;

            if (newCapacity != oldCapacity)
            {
                var dbContext = _roomRepository.DbContext;

                if (newCapacity > oldCapacity)
                {
                    // Add new available beds
                    for (int i = oldCapacity + 1; i <= newCapacity; i++)
                    {
                        var bed = new Bed
                        {
                            RoomId = existingRoom.RoomId,
                            BedNumber = $"B{i}",
                            BedCode = $"{existingRoom.RoomNumber}-B{i}",
                            Status = "Available",
                            Description = $"Giường số {i} thuộc phòng {existingRoom.RoomNumber}"
                        };
                        await dbContext.Set<Bed>().AddAsync(bed);
                    }
                }
                else if (newCapacity < oldCapacity)
                {
                    // Remove unassigned available beds from the tail
                    int bedsToRemove = oldCapacity - newCapacity;
                    var availableBedsToRemove = existingRoom.Beds
                        .Where(b => b.Status == "Available")
                        .OrderByDescending(b => b.BedId)
                        .Take(bedsToRemove)
                        .ToList();

                    foreach (var b in availableBedsToRemove)
                    {
                        dbContext.Set<Bed>().Remove(b);
                    }
                }

                existingRoom.Capacity = newCapacity;
            }

            existingRoom.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _roomRepository.UpdateAsync(existingRoom);
                await _roomRepository.SaveChangesAsync();

                RoomUpdated?.Invoke(this, EventArgs.Empty);
                return ServiceResult<bool>.Success(true, $"Cập nhật phòng '{existingRoom.RoomNumber}' thành công!");
            }
            catch (DbUpdateException ex)
            {
                return ServiceResult<bool>.Failure($"Lỗi cơ sở dữ liệu khi cập nhật phòng: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"Không thể cập nhật phòng: {ex.Message}");
            }
        }

        public async Task<RoomDeleteResult> CheckRoomDeleteDependencyAsync(int roomId)
        {
            var room = await _roomRepository.GetRoomWithDetailsAsync(roomId);
            if (room == null)
            {
                return new RoomDeleteResult
                {
                    CanDelete = false,
                    Message = "Phòng không tồn tại."
                };
            }

            int totalBeds = room.Beds.Count;
            int occupiedBeds = room.Beds.Count(b => b.Status == "Occupied");
            int activeAssignments = room.RoomAssignments.Count(ra => ra.Status == "Active");

            bool canDelete = (totalBeds == 0 && activeAssignments == 0 && occupiedBeds == 0);

            return new RoomDeleteResult
            {
                CanDelete = canDelete,
                TotalBeds = totalBeds,
                OccupiedBeds = occupiedBeds,
                ResidingStudents = occupiedBeds,
                ActiveAssignments = activeAssignments,
                Message = canDelete
                    ? $"Có thể xóa phòng '{room.RoomNumber}'."
                    : $"Không thể xóa phòng '{room.RoomNumber}' vì đang có {totalBeds} giường và {occupiedBeds} sinh viên đang ở."
            };
        }

        public async Task<ServiceResult<bool>> DeactivateRoomAsync(int roomId)
        {
            var room = await _roomRepository.GetRoomWithDetailsAsync(roomId);
            if (room == null) return ServiceResult<bool>.Failure("Phòng không tồn tại.");

            int occupiedCount = room.Beds.Count(b => b.Status == "Occupied");
            if (occupiedCount > 0)
            {
                return ServiceResult<bool>.Failure($"❌ Không thể chuyển phòng '{room.RoomNumber}' sang Inactive vì đang có {occupiedCount} sinh viên đang cư trú.\nVui lòng check-out sinh viên trước.");
            }

            room.Status = "Inactive";
            room.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _roomRepository.UpdateAsync(room);
                await _roomRepository.SaveChangesAsync();

                RoomUpdated?.Invoke(this, EventArgs.Empty);
                return ServiceResult<bool>.Success(true, $"Đã chuyển trạng thái phòng '{room.RoomNumber}' sang Inactive.");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"Không thể vô hiệu hóa phòng: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> DeleteRoomAsync(int roomId)
        {
            var checkResult = await CheckRoomDeleteDependencyAsync(roomId);
            if (!checkResult.CanDelete)
            {
                return ServiceResult<bool>.Failure(checkResult.Message);
            }

            var room = await _roomRepository.GetByIdAsync(roomId);
            if (room == null) return ServiceResult<bool>.Failure("Phòng không tồn tại.");

            try
            {
                await _roomRepository.DeleteAsync(room);
                await _roomRepository.SaveChangesAsync();

                RoomUpdated?.Invoke(this, EventArgs.Empty);
                return ServiceResult<bool>.Success(true, $"Xóa phòng '{room.RoomNumber}' thành công!");
            }
            catch (DbUpdateException)
            {
                return ServiceResult<bool>.Failure($"Không thể xóa phòng '{room.RoomNumber}' do ràng buộc dữ liệu liên quan trong CSDL SQL Server.");
            }
        }

        private static RoomDto MapToDto(Room r)
        {
            int occupied = r.Beds.Count(b => b.Status == "Occupied");
            int reserved = r.Beds.Count(b => b.Status == "Reserved");
            int maintenance = r.Beds.Count(b => b.Status == "Maintenance");
            int capacity = r.Capacity;
            int totalBeds = r.Beds.Count > 0 ? r.Beds.Count : capacity;
            int available = Math.Max(0, totalBeds - occupied - maintenance - reserved);

            return new RoomDto
            {
                RoomId = r.RoomId,
                BuildingId = r.BuildingId,
                BuildingCode = r.Building?.BuildingCode ?? string.Empty,
                BuildingName = BuildingService.SanitizeBuildingName(r.Building?.BuildingName),
                RoomNumber = r.RoomNumber,
                FloorNumber = r.FloorNumber,
                RoomType = r.RoomType,
                Capacity = capacity,
                MonthlyRent = r.MonthlyRent,
                GenderType = r.GenderType,
                Status = r.Status,
                Description = r.Description ?? string.Empty,
                OccupiedBeds = occupied,
                AvailableBeds = available,
                ReservedBeds = reserved,
                MaintenanceBeds = maintenance,
                TotalBedsCreated = r.Beds.Count
            };
        }

        /// <summary>Lấy danh sách sinh viên đang cư trú trong phòng (active assignments).</summary>
        public async Task<List<RoomResidentDto>> GetRoomResidentsAsync(int roomId)
        {
            var assignments = await _roomRepository.GetRoomAllAssignmentsAsync(roomId);
            return assignments
                .Where(ra => ra.Status == "Active")
                .Select(ra => new RoomResidentDto
                {
                    AssignmentId = ra.AssignmentId,
                    StudentId = ra.StudentId,
                    StudentCode = ra.Student?.StudentCode ?? string.Empty,
                    FullName = ra.Student?.FullName ?? string.Empty,
                    BedCode = ra.Bed?.BedCode ?? string.Empty,
                    BedNumber = ra.Bed?.BedNumber ?? string.Empty,
                    StartDate = ra.StartDate,
                    AssignmentType = ra.AssignmentType,
                    AssignedByName = ra.Manager?.Username ?? string.Empty,
                    Status = ra.Status
                })
                .OrderBy(r => r.BedCode)
                .ToList();
        }

        /// <summary>Lấy lịch sử cư trú đầy đủ của phòng (cả Active lẫn Ended).</summary>
        public async Task<List<RoomHistoryEntryDto>> GetRoomHistoryAsync(int roomId)
        {
            var assignments = await _roomRepository.GetRoomAllAssignmentsAsync(roomId);
            return assignments
                .Select(ra => new RoomHistoryEntryDto
                {
                    AssignmentId = ra.AssignmentId,
                    StudentCode = ra.Student?.StudentCode ?? string.Empty,
                    FullName = ra.Student?.FullName ?? string.Empty,
                    BedCode = ra.Bed?.BedCode ?? string.Empty,
                    StartDate = ra.StartDate,
                    EndDate = ra.EndDate,
                    Status = ra.Status,
                    Note = ra.Note ?? string.Empty,
                    AssignedByName = ra.Manager?.Username ?? string.Empty
                })
                .ToList();
        }
    }
}
