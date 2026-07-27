using System;
using System.Collections.Generic;

namespace DormCare.DataAccess.Models;

public partial class Room
{
    public int RoomId { get; set; }

    public int BuildingId { get; set; }

    public string RoomNumber { get; set; } = null!;

    public int FloorNumber { get; set; }

    public string RoomType { get; set; } = null!;

    public int Capacity { get; set; }

    public decimal MonthlyRent { get; set; }

    public string GenderType { get; set; } = null!;

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Bed> Beds { get; set; } = new List<Bed>();

    public virtual Building Building { get; set; } = null!;

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();

    public virtual ICollection<RoomApplication> RoomApplications { get; set; } = new List<RoomApplication>();

    public virtual ICollection<RoomAssignment> RoomAssignments { get; set; } = new List<RoomAssignment>();
}
