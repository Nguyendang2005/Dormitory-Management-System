using System;

namespace DormCare.Domain.Entities
{
    public class MaintenanceRequest
    {
        public int RequestId { get; set; }
        public string RequestCode { get; set; } = string.Empty;

        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public int RoomId { get; set; }
        public Room Room { get; set; } = null!;

        public string Category { get; set; } = "Electricity"; // Electricity, Water, AirConditioner, Furniture
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical
        public string Status { get; set; } = "Submitted"; // Submitted, Assigned, InProgress, Resolved, Closed, Rejected

        public int? AssignedTo { get; set; }
        public User? Assignee { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AssignedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public string? ResolutionNote { get; set; }
        public int? StudentRating { get; set; }
        public string? StudentFeedback { get; set; }
    }
}
