using Microsoft.EntityFrameworkCore;
using DormCare.Domain.Entities;

namespace DormCare.DataAccess.Data
{
    public class DormCareDbContext : DbContext
    {
        public DormCareDbContext() { }

        public DormCareDbContext(DbContextOptions<DormCareDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<Building> Buildings { get; set; } = null!;
        public DbSet<Room> Rooms { get; set; } = null!;
        public DbSet<Bed> Beds { get; set; } = null!;
        public DbSet<RoomApplication> RoomApplications { get; set; } = null!;
        public DbSet<RoomAssignment> RoomAssignments { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Real SQL Server Connection String specified by user
                optionsBuilder.UseSqlServer("Server=DANG;Database=DormCareDB;User Id=sa;Password=123456;TrustServerCertificate=True;Encrypt=False;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.StudentId);
                entity.HasIndex(e => e.StudentCode).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasOne(s => s.User)
                      .WithOne(u => u.StudentProfile)
                      .HasForeignKey<Student>(s => s.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Building>(entity =>
            {
                entity.HasKey(e => e.BuildingId);
                entity.HasIndex(e => e.BuildingCode).IsUnique();
            });

            modelBuilder.Entity<Room>(entity =>
            {
                entity.HasKey(e => e.RoomId);
                entity.HasOne(r => r.Building)
                      .WithMany(b => b.Rooms)
                      .HasForeignKey(r => r.BuildingId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Bed>(entity =>
            {
                entity.HasKey(e => e.BedId);
                entity.HasIndex(e => e.BedCode).IsUnique();
                entity.HasOne(b => b.Room)
                      .WithMany(r => r.Beds)
                      .HasForeignKey(b => b.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RoomApplication>(entity =>
            {
                entity.HasKey(e => e.ApplicationId);
                entity.HasOne(a => a.Student)
                      .WithMany(s => s.RoomApplications)
                      .HasForeignKey(a => a.StudentId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(a => a.Room)
                      .WithMany(r => r.RoomApplications)
                      .HasForeignKey(a => a.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(a => a.Reviewer)
                      .WithMany()
                      .HasForeignKey(a => a.ReviewedBy)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RoomAssignment>(entity =>
            {
                entity.HasKey(e => e.AssignmentId);
                entity.HasOne(a => a.Student)
                      .WithMany(s => s.RoomAssignments)
                      .HasForeignKey(a => a.StudentId);
                entity.HasOne(a => a.Room)
                      .WithMany(r => r.RoomAssignments)
                      .HasForeignKey(a => a.RoomId);
                entity.HasOne(a => a.Bed)
                      .WithMany(b => b.RoomAssignments)
                      .HasForeignKey(a => a.BedId);
                entity.HasOne(a => a.Manager)
                      .WithMany()
                      .HasForeignKey(a => a.AssignedBy)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.InvoiceId);
                entity.Property(e => e.TotalAmount)
                      .HasComputedColumnSql("RoomFee + ServiceFee + OtherFee - DiscountAmount", stored: true);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.PaymentId);
                entity.HasOne(p => p.Receiver)
                      .WithMany()
                      .HasForeignKey(p => p.ReceivedBy)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MaintenanceRequest>(entity =>
            {
                entity.HasKey(e => e.RequestId);
                entity.HasOne(m => m.Assignee)
                      .WithMany()
                      .HasForeignKey(m => m.AssignedTo)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.NotificationId);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.AuditLogId);
            });
        }
    }
}
