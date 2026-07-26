using System;

namespace DormCare.Business.DTOs
{
    public class StudentDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = "Chưa nhận phòng";
        public string BuildingName { get; set; } = "N/A";
        public string BedNumber { get; set; } = "N/A";
    }
}
