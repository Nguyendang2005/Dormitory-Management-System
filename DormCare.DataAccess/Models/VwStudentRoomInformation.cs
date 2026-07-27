using System;
using System.Collections.Generic;

namespace DormCare.DataAccess.Models;

public partial class VwStudentRoomInformation
{
    public int StudentId { get; set; }

    public string StudentCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string ClassName { get; set; } = null!;

    public string BuildingName { get; set; } = null!;

    public string RoomNumber { get; set; } = null!;

    public string BedCode { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public string AssignmentStatus { get; set; } = null!;
}
