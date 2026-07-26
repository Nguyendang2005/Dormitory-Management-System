using System;
using System.Collections.Generic;

namespace DormCare.DataAccess.Models;

public partial class Building
{
    public int BuildingId { get; set; }

    public string BuildingCode { get; set; } = null!;

    public string BuildingName { get; set; } = null!;

    public string Address { get; set; } = null!;

    public int NumberOfFloors { get; set; }

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
