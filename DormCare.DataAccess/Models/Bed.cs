using System;
using System.Collections.Generic;

namespace DormCare.DataAccess.Models;

public partial class Bed
{
    public int BedId { get; set; }

    public int RoomId { get; set; }

    public string BedNumber { get; set; } = null!;

    public string BedCode { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Room Room { get; set; } = null!;

    public virtual ICollection<RoomApplication> RoomApplications { get; set; } = new List<RoomApplication>();

    public virtual ICollection<RoomAssignment> RoomAssignments { get; set; } = new List<RoomAssignment>();
}
