using System;
using System.Collections.Generic;

namespace DormCare.Domain.Entities
{
    public class Bed
    {
        public int BedId { get; set; }
        public int RoomId { get; set; }
        public Room Room { get; set; } = null!;

        public string BedNumber { get; set; } = string.Empty;
        public string BedCode { get; set; } = string.Empty;
        public string Status { get; set; } = "Available"; // Available, Occupied, Reserved, Maintenance
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<RoomAssignment> RoomAssignments { get; set; } = new List<RoomAssignment>();
        public ICollection<RoomApplication> RoomApplications { get; set; } = new List<RoomApplication>();
    }
}
