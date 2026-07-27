using System;
using System.Collections.Generic;

namespace DormCare.DataAccess.Models;

public partial class VwRoomOccupancy
{
    public string BuildingName { get; set; } = null!;

    public int RoomId { get; set; }

    public string RoomNumber { get; set; } = null!;

    public int Capacity { get; set; }

    public int? OccupiedBeds { get; set; }

    public int? AvailableBeds { get; set; }

    public int? MaintenanceBeds { get; set; }

    public decimal MonthlyRent { get; set; }

    public string Status { get; set; } = null!;
}
