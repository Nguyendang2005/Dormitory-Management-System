using System;
using System.Collections.Generic;

namespace DormCare.Domain.Entities
{
    public class Building
    {
        public int BuildingId { get; set; }
        public string BuildingCode { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int NumberOfFloors { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = "Active"; // Active, Inactive, Maintenance
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
