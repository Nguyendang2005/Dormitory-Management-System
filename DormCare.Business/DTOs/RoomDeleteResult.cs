namespace DormCare.Business.DTOs
{
    public class RoomDeleteResult
    {
        public bool CanDelete { get; set; }
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public int ResidingStudents { get; set; }
        public int ActiveAssignments { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
