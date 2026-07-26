using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DormCare.Business.DTOs;
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

        public async Task<IEnumerable<BuildingDto>> GetAllBuildingsAsync()
        {
            var buildings = await _buildingRepository.GetBuildingsWithRoomsAsync();
            return buildings.Select(b => new BuildingDto
            {
                BuildingId = b.BuildingId,
                BuildingCode = b.BuildingCode,
                BuildingName = b.BuildingName,
                Address = b.Address,
                NumberOfFloors = b.NumberOfFloors,
                Description = b.Description ?? string.Empty,
                Status = b.Status,
                TotalRooms = b.Rooms.Count,
                TotalBeds = b.Rooms.Sum(r => r.Capacity),
                OccupiedBeds = b.Rooms.SelectMany(r => r.Beds).Count(bed => bed.Status == "Occupied")
            });
        }

        public async Task<ServiceResult<bool>> AddBuildingAsync(Building building)
        {
            if (string.IsNullOrWhiteSpace(building.BuildingCode) || string.IsNullOrWhiteSpace(building.BuildingName))
            {
                return ServiceResult<bool>.Failure("Mã tòa nhà và Tên tòa nhà không được để trống.");
            }

            await _buildingRepository.AddAsync(building);
            await _buildingRepository.SaveChangesAsync();

            BuildingUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<bool>.Success(true, "Thêm tòa nhà mới thành công!");
        }

        public async Task<ServiceResult<bool>> UpdateBuildingAsync(Building building)
        {
            await _buildingRepository.UpdateAsync(building);
            await _buildingRepository.SaveChangesAsync();

            BuildingUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<bool>.Success(true, "Cập nhật thông tin tòa nhà thành công!");
        }
    }
}
