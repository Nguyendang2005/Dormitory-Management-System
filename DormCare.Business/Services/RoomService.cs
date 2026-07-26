using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DormCare.Business.DTOs;
using DormCare.DataAccess.Repositories;
using DormCare.Domain.Entities;

namespace DormCare.Business.Services
{
    public class RoomService
    {
        private readonly RoomRepository _roomRepository;
        private readonly BuildingRepository _buildingRepository;

        public event EventHandler? RoomUpdated;

        public RoomService(RoomRepository roomRepository, BuildingRepository buildingRepository)
        {
            _roomRepository = roomRepository;
            _buildingRepository = buildingRepository;
        }

        public async Task<IEnumerable<RoomDto>> GetAllRoomsAsync()
        {
            var rooms = await _roomRepository.GetRoomsWithBuildingAndBedsAsync();
            return rooms.Select(r => new RoomDto
            {
                RoomId = r.RoomId,
                BuildingId = r.BuildingId,
                BuildingName = r.Building != null ? r.Building.BuildingName : "N/A",
                RoomNumber = r.RoomNumber,
                FloorNumber = r.FloorNumber,
                RoomType = r.RoomType,
                Capacity = r.Capacity,
                MonthlyRent = r.MonthlyRent,
                GenderType = r.GenderType,
                Status = r.Status,
                Description = r.Description ?? string.Empty,
                OccupiedBeds = r.Beds.Count(b => b.Status == "Occupied")
            });
        }

        public async Task<IEnumerable<RoomDto>> SearchAndFilterRoomsAsync(int? buildingId, string? genderType, string? roomType, string? searchText)
        {
            var rooms = await _roomRepository.GetRoomsWithBuildingAndBedsAsync();
            var list = rooms.ToList();

            IEnumerable<Room> query = list;

            if (buildingId.HasValue && buildingId.Value > 0)
            {
                query = query.Where(r => r.BuildingId == buildingId.Value);
            }

            if (!string.IsNullOrWhiteSpace(genderType) && genderType != "All")
            {
                query = query.Where(r => r.GenderType.Equals(genderType, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(roomType) && roomType != "All")
            {
                query = query.Where(r => r.RoomType.Equals(roomType, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(r => r.RoomNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                         (r.Building != null && r.Building.BuildingName.Contains(searchText, StringComparison.OrdinalIgnoreCase)));
            }

            return query.Select(r => new RoomDto
            {
                RoomId = r.RoomId,
                BuildingId = r.BuildingId,
                BuildingName = r.Building != null ? r.Building.BuildingName : "N/A",
                RoomNumber = r.RoomNumber,
                FloorNumber = r.FloorNumber,
                RoomType = r.RoomType,
                Capacity = r.Capacity,
                MonthlyRent = r.MonthlyRent,
                GenderType = r.GenderType,
                Status = r.Status,
                Description = r.Description ?? string.Empty,
                OccupiedBeds = r.Beds.Count(b => b.Status == "Occupied")
            });
        }

        public async Task<RoomOccupancyDto> GetOccupancyStatsAsync()
        {
            var buildings = await _buildingRepository.GetBuildingsWithRoomsAsync();
            var allRooms = buildings.SelectMany(b => b.Rooms).ToList();
            var allBeds = allRooms.SelectMany(r => r.Beds).ToList();

            return new RoomOccupancyDto
            {
                TotalBuildings = buildings.Count(),
                TotalRooms = allRooms.Count,
                TotalBeds = allBeds.Count,
                OccupiedBeds = allBeds.Count(b => b.Status == "Occupied"),
                AvailableBeds = allBeds.Count(b => b.Status == "Available"),
                MaintenanceBeds = allBeds.Count(b => b.Status == "Maintenance")
            };
        }

        public async Task<ServiceResult<bool>> AddRoomAsync(Room room)
        {
            if (string.IsNullOrWhiteSpace(room.RoomNumber))
            {
                return ServiceResult<bool>.Failure("Số phòng không được để trống.");
            }

            await _roomRepository.AddAsync(room);
            await _roomRepository.SaveChangesAsync();

            RoomUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<bool>.Success(true, "Thêm phòng mới thành công!");
        }

        public async Task<ServiceResult<bool>> UpdateRoomAsync(Room room)
        {
            await _roomRepository.UpdateAsync(room);
            await _roomRepository.SaveChangesAsync();

            RoomUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<bool>.Success(true, "Cập nhật thông tin phòng thành công!");
        }
    }
}
