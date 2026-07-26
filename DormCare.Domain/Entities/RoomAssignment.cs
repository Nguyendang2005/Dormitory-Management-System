using System;

namespace DormCare.Domain.Entities
{
    public class RoomAssignment
    {
        public int AssignmentId { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public int RoomId { get; set; }
        public Room Room { get; set; } = null!;

        public int BedId { get; set; }
        public Bed Bed { get; set; } = null!;

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string AssignmentType { get; set; } = "InitialAssignment"; // InitialAssignment, RoomTransfer, Replacement
        public string Status { get; set; } = "Active"; // Active, Ended

        public int AssignedBy { get; set; }
        public User Manager { get; set; } = null!;

        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
