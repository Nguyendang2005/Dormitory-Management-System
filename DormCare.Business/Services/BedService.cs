using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DormCare.Business.DTOs;
using DormCare.DataAccess.Repositories;
using DormCare.Domain.Entities;

namespace DormCare.Business.Services
{
    public class BedStatsDto
    {
        public int TotalBedsCount { get; set; }
        public int AvailableBedsCount { get; set; }
        public int OccupiedBedsCount { get; set; }
        public int MaintenanceBedsCount { get; set; }
    }

    public class BedService
    {
        private readonly BedRepository _bedRepository;

        public event EventHandler? BedUpdated;

        public BedService(BedRepository bedRepository)
        {
            _bedRepository = bedRepository;
        }

        public async Task<IEnumerable<BedDto>> GetAllBedsAsync()
        {
            var beds = await _bedRepository.GetAllBedsWithDetailsAsync();
            return beds.Select(MapToDto);
        }

        public async Task<BedStatsDto> GetBedStatsAsync()
        {
            var beds = await _bedRepository.GetAllBedsWithDetailsAsync();
            var list = beds.ToList();

            return new BedStatsDto
            {
                TotalBedsCount = list.Count,
                AvailableBedsCount = list.Count(b => b.Status == "Available"),
                OccupiedBedsCount = list.Count(b => b.Status == "Occupied"),
                MaintenanceBedsCount = list.Count(b => b.Status == "Maintenance")
            };
        }

        public async Task<IEnumerable<BedDto>> SearchAndFilterBedsAsync(string? statusFilter, string? searchText)
        {
            var beds = await _bedRepository.GetAllBedsWithDetailsAsync();
            var query = beds.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All" && statusFilter != "Tất cả trạng thái")
            {
                query = query.Where(b => b.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string kw = searchText.Trim();
                query = query.Where(b =>
                    (!string.IsNullOrEmpty(b.BedCode) && b.BedCode.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(b.BedNumber) && b.BedNumber.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                    (b.Room != null && b.Room.RoomNumber.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                    (b.Room != null && b.Room.Building != null && b.Room.Building.BuildingName.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                    (b.RoomAssignments != null && b.RoomAssignments.Any(ra => ra.Status == "Active" && ra.Student != null && ra.Student.FullName.Contains(kw, StringComparison.OrdinalIgnoreCase))));
            }

            return query.Select(MapToDto);
        }

        public async Task<IEnumerable<BedDto>> GetBedsByRoomIdAsync(int roomId)
        {
            if (roomId == 0) return await GetAllBedsAsync();

            var beds = await _bedRepository.GetBedsByRoomIdAsync(roomId);
            return beds.Select(MapToDto);
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
            return ServiceResult<bool>.Success(true, $"Cập nhật trạng thái giường '{bed.BedCode}' thành '{newStatus}' thành công!");
        }

        private static BedDto MapToDto(Bed b)
        {
            var activeAssign = b.RoomAssignments?.FirstOrDefault(ra => ra.Status == "Active");
            string studentName = activeAssign?.Student?.FullName ?? (b.Status == "Occupied" ? "Sinh viên" : string.Empty);
            string studentCode = activeAssign?.Student?.StudentCode ?? string.Empty;

            return new BedDto
            {
                BedId = b.BedId,
                RoomId = b.RoomId,
                RoomNumber = b.Room?.RoomNumber ?? string.Empty,
                BuildingName = BuildingService.SanitizeBuildingName(b.Room?.Building?.BuildingName),
                BedNumber = b.BedNumber,
                BedCode = b.BedCode,
                Status = b.Status,
                StudentName = studentName,
                StudentCode = studentCode,
                StartDate = activeAssign?.StartDate,
                Description = b.Description ?? string.Empty
            };
        }
    }
}
