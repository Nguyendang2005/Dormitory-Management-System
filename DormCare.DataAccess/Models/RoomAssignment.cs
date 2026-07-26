using System;
using System.Collections.Generic;

namespace DormCare.DataAccess.Models;

public partial class RoomAssignment
{
    public int AssignmentId { get; set; }

    public int StudentId { get; set; }

    public int RoomId { get; set; }

    public int BedId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string AssignmentType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int AssignedBy { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User AssignedByNavigation { get; set; } = null!;

    public virtual Bed Bed { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
