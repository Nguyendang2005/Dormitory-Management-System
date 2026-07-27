namespace DormCare.Business.DTOs
{
    public class BuildingDeleteResult
    {
        public bool CanDelete { get; set; }
        public int TotalRooms { get; set; }
        public int TotalBeds { get; set; }
        public int ResidingStudents { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
