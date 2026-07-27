using System;
using System.Collections.Generic;

namespace DormCare.DataAccess.Models;

public partial class MaintenanceRequest
{
    public int RequestId { get; set; }

    public string RequestCode { get; set; } = null!;

    public int StudentId { get; set; }

    public int RoomId { get; set; }

    public string Category { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int? AssignedTo { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public string? ResolutionNote { get; set; }

    public int? StudentRating { get; set; }

    public string? StudentFeedback { get; set; }

    public virtual User? AssignedToNavigation { get; set; }

    public virtual Room Room { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
