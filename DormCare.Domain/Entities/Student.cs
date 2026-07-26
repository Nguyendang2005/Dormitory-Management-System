using System;
using System.Collections.Generic;

namespace DormCare.Domain.Entities
{
    public class Student
    {
        public int StudentId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = "Male";
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Campus { get; set; } = string.Empty;
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? Address { get; set; }
        public string Status { get; set; } = "Active"; // Active, Inactive, Graduated, Suspended
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<RoomApplication> RoomApplications { get; set; } = new List<RoomApplication>();
        public ICollection<RoomAssignment> RoomAssignments { get; set; } = new List<RoomAssignment>();
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
    }
}
