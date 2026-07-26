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
                OccupiedBeds = r.Beds.Count(b => b.Status == "Occupied"),
                AvailableBeds = r.Beds.Count(b => b.Status == "Available"),
                ReservedBeds = r.Beds.Count(b => b.Status == "Reserved"),
                MaintenanceBeds = r.Beds.Count(b => b.Status == "Maintenance"),
                TotalBedsCreated = r.Beds.Count
            });
        }

        public async Task<IEnumerable<RoomDto>> GetAvailableRoomsAsync()
        {
            var rooms = await _roomRepository.GetRoomsWithBuildingAndBedsAsync();
            var availableRooms = rooms
                .Where(r => r.Status == "Available" && r.Beds.Any(b => b.Status == "Available"))
                .Select(r => new RoomDto
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
                    OccupiedBeds = r.Beds.Count(b => b.Status == "Occupied"),
                    AvailableBeds = r.Beds.Count(b => b.Status == "Available"),
                    ReservedBeds = r.Beds.Count(b => b.Status == "Reserved"),
                    MaintenanceBeds = r.Beds.Count(b => b.Status == "Maintenance"),
                    TotalBedsCreated = r.Beds.Count
                });

            return availableRooms;
        }

        public async Task<RoomOccupancyDto> GetOccupancyStatsAsync()
        {
            var rooms = await _roomRepository.GetRoomsWithBuildingAndBedsAsync();
            var roomList = rooms.ToList();

            var totalBeds = roomList.Sum(r => r.Beds.Count);
            var occupiedBeds = roomList.Sum(r => r.Beds.Count(b => b.Status == "Occupied"));
            var availableBeds = roomList.Sum(r => r.Beds.Count(b => b.Status == "Available"));

            return new RoomOccupancyDto
            {
                TotalRooms = roomList.Count,
                TotalBeds = totalBeds,
                OccupiedBeds = occupiedBeds,
                AvailableBeds = availableBeds,
                OccupancyRate = totalBeds > 0 ? (double)occupiedBeds / totalBeds * 100 : 0
            };
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
                OccupiedBeds = r.Beds.Count(b => b.Status == "Occupied"),
                AvailableBeds = r.Beds.Count(b => b.Status == "Available"),
                ReservedBeds = r.Beds.Count(b => b.Status == "Reserved"),
                MaintenanceBeds = r.Beds.Count(b => b.Status == "Maintenance"),
                TotalBedsCreated = r.Beds.Count
            });
        }

        public async Task<ServiceResult<Room>> AddRoomAsync(Room room)
        {
            var building = await _buildingRepository.GetByIdAsync(room.BuildingId);
            if (building != null && (building.Status == "Inactive" || building.Status == "Maintenance"))
            {
                return ServiceResult<Room>.Failure($"Tòa nhà '{building.BuildingName}' đang ở trạng thái {building.Status}, không thể thêm phòng mới.");
            }

            await _roomRepository.AddAsync(room);
            await _roomRepository.SaveChangesAsync();
            RoomUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<Room>.Success(room, "Thêm phòng thành công!");
        }
    }
}
