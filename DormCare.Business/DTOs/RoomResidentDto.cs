using System;
using System.Collections.Generic;

namespace DormCare.Business.DTOs
{
    // DTO cho từng sinh viên đang cư trú (dùng trong Room Detail)
    public class RoomResidentDto
    {
        public int AssignmentId { get; set; }
        public int StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string BedCode { get; set; } = string.Empty;
        public string BedNumber { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public string AssignmentType { get; set; } = "InitialAssignment"; // InitialAssignment, RoomTransfer, Replacement
        public string AssignedByName { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";

        public string AssignmentTypeDisplay => AssignmentType switch
        {
            "InitialAssignment" => "Phân phòng ban đầu",
            "RoomTransfer"      => "Chuyển phòng",
            "Replacement"       => "Thay thế",
            _                   => AssignmentType
        };

        public string StartDateDisplay => StartDate.ToString("dd/MM/yyyy");
        public int DaysStaying => (DateTime.Today - StartDate.Date).Days;
    }

    // DTO cho lịch sử cư trú (cả Active lẫn Ended)
    public class RoomHistoryEntryDto
    {
        public int AssignmentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string BedCode { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = "Active"; // Active, Ended
        public string Note { get; set; } = string.Empty;
        public string AssignedByName { get; set; } = string.Empty;

        public string StartDateDisplay => StartDate.ToString("dd/MM/yyyy");
        public string EndDateDisplay => EndDate.HasValue ? EndDate.Value.ToString("dd/MM/yyyy") : "—";
        public int DurationDays => EndDate.HasValue
            ? (EndDate.Value.Date - StartDate.Date).Days
            : (DateTime.Today - StartDate.Date).Days;
        public string DurationDisplay => DurationDays == 1 ? "1 ngày" : $"{DurationDays} ngày";
        public string StatusDisplay => Status == "Active" ? "Đang cư trú" : "Đã kết thúc";
    }
}
