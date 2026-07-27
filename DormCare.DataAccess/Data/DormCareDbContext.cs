using DormCare.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DormCare.DataAccess.Data
{
    public class DormCareDbContext : DbContext
    {
        public DormCareDbContext(DbContextOptions<DormCareDbContext> options)
            : base(options)
        {
        }

        public DormCareDbContext()
        {
        }

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
                entity.HasOne(a => a.PreferredBed)
                      .WithMany(b => b.RoomApplications)
                      .HasForeignKey(a => a.PreferredBedId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.Reviewer)
                      .WithMany()
                      .HasForeignKey(a => a.ReviewedBy)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RoomAssignment>(entity =>
            {
                entity.HasKey(e => e.AssignmentId);
                entity.HasOne(a => a.Student)
                      .WithMany(s => s.RoomAssignments)
                      .HasForeignKey(a => a.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.Room)
                      .WithMany(r => r.RoomAssignments)
                      .HasForeignKey(a => a.RoomId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.Bed)
                      .WithMany(b => b.RoomAssignments)
                      .HasForeignKey(a => a.BedId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.Manager)
                      .WithMany()
                      .HasForeignKey(a => a.AssignedBy)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.InvoiceId);
                entity.HasIndex(e => e.InvoiceCode).IsUnique();
                entity.HasOne(i => i.Student)
                      .WithMany(s => s.Invoices)
                      .HasForeignKey(i => i.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(i => i.Room)
                      .WithMany(r => r.Invoices)
                      .HasForeignKey(i => i.RoomId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.PaymentId);
                entity.HasIndex(e => e.PaymentCode).IsUnique();
                entity.HasOne(p => p.Invoice)
                      .WithMany(i => i.Payments)
                      .HasForeignKey(p => p.InvoiceId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(p => p.Receiver)
                      .WithMany()
                      .HasForeignKey(p => p.ReceivedBy)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MaintenanceRequest>(entity =>
            {
                entity.HasKey(e => e.RequestId);
                entity.HasIndex(e => e.RequestCode).IsUnique();
                entity.HasOne(m => m.Student)
                      .WithMany(s => s.MaintenanceRequests)
                      .HasForeignKey(m => m.StudentId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(m => m.Room)
                      .WithMany(r => r.MaintenanceRequests)
                      .HasForeignKey(m => m.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(m => m.Assignee)
                      .WithMany()
                      .HasForeignKey(m => m.AssignedTo)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.NotificationId);
                entity.HasOne(n => n.User)
                      .WithMany()
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.AuditLogId);
                entity.HasOne(a => a.User)
                      .WithMany()
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
