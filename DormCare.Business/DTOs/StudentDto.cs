using System;

namespace DormCare.Business.DTOs
{
    public class StudentDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; } = new DateTime(2005, 1, 1);
        public string Major { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Gender { get; set; } = "Male";
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Campus { get; set; } = "FPT University Da Nang";
        public string? Address { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string Status { get; set; } = "Active";

        public int? RoomId { get; set; }
        public string RoomNumber { get; set; } = "Chưa nhận phòng";
        public string BuildingName { get; set; } = "N/A";
        public string BedNumber { get; set; } = "N/A";
        public DateTime? CheckInDate { get; set; }

        public bool HasRoom => RoomId.HasValue;

        public string RoomDisplay => RoomId.HasValue
            ? $"{BuildingName} — {RoomNumber}"
            : "Chưa nhận phòng";
    }
}
