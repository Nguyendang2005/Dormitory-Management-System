using System;
using System.Collections.Generic;

namespace DormCare.Domain.Entities
{
    public class Room
    {
        public int RoomId { get; set; }
        public int BuildingId { get; set; }
        public Building Building { get; set; } = null!;

        public string RoomNumber { get; set; } = string.Empty;
        public int FloorNumber { get; set; }
        public string RoomType { get; set; } = "Standard"; // Standard, Premium, Accessible
        public int Capacity { get; set; } = 6;
        public decimal MonthlyRent { get; set; } = 1500000;
        public string GenderType { get; set; } = "Male"; // Male, Female, Mixed
        public string? Description { get; set; }
        public string Status { get; set; } = "Available"; // Available, Full, Maintenance, Inactive
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Bed> Beds { get; set; } = new List<Bed>();
        public ICollection<RoomApplication> RoomApplications { get; set; } = new List<RoomApplication>();
        public ICollection<RoomAssignment> RoomAssignments { get; set; } = new List<RoomAssignment>();
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
    }
}
