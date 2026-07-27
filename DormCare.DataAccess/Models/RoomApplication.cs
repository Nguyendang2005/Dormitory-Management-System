using System;
using System.Collections.Generic;

namespace DormCare.DataAccess.Models;

public partial class RoomApplication
{
    public int ApplicationId { get; set; }

    public string ApplicationCode { get; set; } = null!;

    public int StudentId { get; set; }

    public int RoomId { get; set; }

    public int? PreferredBedId { get; set; }

    public string? Reason { get; set; }

    public DateTime ApplicationDate { get; set; }

    public int? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Bed? PreferredBed { get; set; }

    public virtual User? ReviewedByNavigation { get; set; }

    public virtual Room Room { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
