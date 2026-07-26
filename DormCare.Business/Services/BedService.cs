using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DormCare.Business.DTOs;
using DormCare.DataAccess.Repositories;
using DormCare.Domain.Entities;

namespace DormCare.Business.Services
{
    public class BedService
    {
        private readonly BedRepository _bedRepository;

        public event EventHandler? BedUpdated;

        public BedService(BedRepository bedRepository)
        {
            _bedRepository = bedRepository;
        }

        public async Task<IEnumerable<BedDto>> GetBedsByRoomIdAsync(int roomId)
        {
            var beds = await _bedRepository.GetBedsByRoomIdAsync(roomId);
            return beds.Select(b => new BedDto
            {
                BedId = b.BedId,
                RoomId = b.RoomId,
                RoomNumber = b.Room?.RoomNumber ?? string.Empty,
                BuildingName = b.Room?.Building?.BuildingName ?? string.Empty,
                BedNumber = b.BedNumber,
                BedCode = b.BedCode,
                Status = b.Status,
                Description = b.Description ?? string.Empty
            });
        }

        public async Task<ServiceResult<bool>> UpdateBedStatusAsync(int bedId, string newStatus)
        {
            var bed = await _bedRepository.GetByIdAsync(bedId);
            if (bed == null) return ServiceResult<bool>.Failure("Không tìm thấy giường.");

            bed.Status = newStatus;
            bed.UpdatedAt = DateTime.UtcNow;

            await _bedRepository.UpdateAsync(bed);
            await _bedRepository.SaveChangesAsync();

            BedUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<bool>.Success(true, "Cập nhật trạng thái giường thành công!");
        }
    }
}
