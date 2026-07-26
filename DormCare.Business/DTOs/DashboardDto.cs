namespace DormCare.Business.DTOs
{
    public class DashboardDto
    {
        public int TotalStudents { get; set; }
        public int TotalRooms { get; set; }
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public int AvailableBeds => TotalBeds - OccupiedBeds;
        public int PendingApplications { get; set; }
        public int UnpaidInvoices { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int PendingMaintenanceRequests { get; set; }
    }
}
