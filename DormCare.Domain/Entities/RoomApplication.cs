using System;

namespace DormCare.Domain.Entities
{
    public class RoomApplication
    {
        public int ApplicationId { get; set; }
        public string ApplicationCode { get; set; } = string.Empty;

        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public int RoomId { get; set; }
        public Room Room { get; set; } = null!;

        public int? PreferredBedId { get; set; }
        public Bed? PreferredBed { get; set; }

        public string? Reason { get; set; }
        public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;

        public int? ReviewedBy { get; set; }
        public User? Reviewer { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNote { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Cancelled
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
