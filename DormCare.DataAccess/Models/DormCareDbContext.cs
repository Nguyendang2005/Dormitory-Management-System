using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DormCare.DataAccess.Models;

public partial class DormCareDbContext : DbContext
{
    public DormCareDbContext()
    {
    }

    public DormCareDbContext(DbContextOptions<DormCareDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Bed> Beds { get; set; }

    public virtual DbSet<Building> Buildings { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<RoomApplication> RoomApplications { get; set; }

    public virtual DbSet<RoomAssignment> RoomAssignments { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VwMonthlyRevenue> VwMonthlyRevenues { get; set; }

    public virtual DbSet<VwRoomOccupancy> VwRoomOccupancies { get; set; }

    public virtual DbSet<VwStudentRoomInformation> VwStudentRoomInformations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.\\MSSQLSERVER01;Database=DormCareDB;User Id=sa;Password=123456;TrustServerCertificate=True;Encrypt=False;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId).HasName("PK__AuditLog__EB5F6CBDC28FD3ED");

            entity.Property(e => e.Action)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_AuditLogs_CreatedAt");
            entity.Property(e => e.EntityName)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_AuditLogs_User");
        });

        modelBuilder.Entity<Bed>(entity =>
        {
            entity.HasKey(e => e.BedId).HasName("PK__Beds__A8A7104079F73F97");

            entity.HasIndex(e => new { e.RoomId, e.Status }, "IX_Beds_Room_Status");

            entity.HasIndex(e => e.BedCode, "UQ_Beds_BedCode").IsUnique();

            entity.HasIndex(e => new { e.RoomId, e.BedNumber }, "UQ_Beds_Room_BedNumber").IsUnique();

            entity.Property(e => e.BedCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.BedNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Beds_CreatedAt");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Available", "DF_Beds_Status");

            entity.HasOne(d => d.Room).WithMany(p => p.Beds)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Beds_Rooms");
        });

        modelBuilder.Entity<Building>(entity =>
        {
            entity.HasKey(e => e.BuildingId).HasName("PK__Building__5463CDC4D69CD675");

            entity.HasIndex(e => e.BuildingCode, "UQ_Buildings_Code").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.BuildingCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.BuildingName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Buildings_CreatedAt");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active", "DF_Buildings_Status");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__Invoices__D796AAB5C1D686C3");

            entity.HasIndex(e => e.DueDate, "IX_Invoices_DueDate");

            entity.HasIndex(e => e.Status, "IX_Invoices_Status");

            entity.HasIndex(e => e.InvoiceCode, "UQ_Invoices_Code").IsUnique();

            entity.HasIndex(e => new { e.StudentId, e.BillingMonth }, "UQ_Invoices_Student_Month")
                .IsUnique()
                .HasFilter("[Status] <> 'Cancelled'");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Invoices_CreatedAt");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.InvoiceCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.OtherFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RoomFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ServiceFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Unpaid", "DF_Invoices_Status");
            entity.Property(e => e.TotalAmount)
                .HasComputedColumnSql("((([RoomFee]+[ServiceFee])+[OtherFee])-[DiscountAmount])", true)
                .HasColumnType("decimal(21, 2)");

            entity.HasOne(d => d.Room).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_Room");

            entity.HasOne(d => d.Student).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_Student");
        });

        modelBuilder.Entity<MaintenanceRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__Maintena__33A8517A8C00D84A");

            entity.HasIndex(e => new { e.Status, e.Priority }, "IX_Maintenance_Status_Priority");

            entity.HasIndex(e => e.RequestCode, "UQ_Maintenance_Code").IsUnique();

            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Maintenance_CreatedAt");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Priority)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Medium", "DF_Maintenance_Priority");
            entity.Property(e => e.RequestCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ResolutionNote).HasMaxLength(1000);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Submitted", "DF_Maintenance_Status");
            entity.Property(e => e.StudentFeedback).HasMaxLength(500);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.AssignedToNavigation).WithMany(p => p.MaintenanceRequests)
                .HasForeignKey(d => d.AssignedTo)
                .HasConstraintName("FK_Maintenance_Assignee");

            entity.HasOne(d => d.Room).WithMany(p => p.MaintenanceRequests)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Maintenance_Room");

            entity.HasOne(d => d.Student).WithMany(p => p.MaintenanceRequests)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Maintenance_Student");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E128DC4EFB1");

            entity.HasIndex(e => new { e.UserId, e.IsRead }, "IX_Notifications_User_Read");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Notifications_CreatedAt");
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.NotificationType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notifications_User");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A38F3B8FB3E");

            entity.HasIndex(e => e.InvoiceId, "IX_Payments_Invoice");

            entity.HasIndex(e => e.PaymentCode, "UQ_Payments_Code").IsUnique();

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.PaymentCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.PaymentDate).HasDefaultValueSql("(sysutcdatetime())", "DF_Payments_Date");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Completed", "DF_Payments_Status");
            entity.Property(e => e.TransactionReference)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Invoice");

            entity.HasOne(d => d.ReceivedByNavigation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ReceivedBy)
                .HasConstraintName("FK_Payments_ReceivedBy");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PK__Rooms__32863939D62BB72A");

            entity.HasIndex(e => e.BuildingId, "IX_Rooms_BuildingId");

            entity.HasIndex(e => e.Status, "IX_Rooms_Status");

            entity.HasIndex(e => new { e.BuildingId, e.RoomNumber }, "UQ_Rooms_Building_Room").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Rooms_CreatedAt");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.GenderType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MonthlyRent).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RoomNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RoomType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Available", "DF_Rooms_Status");

            entity.HasOne(d => d.Building).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.BuildingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Rooms_Buildings");
        });

        modelBuilder.Entity<RoomApplication>(entity =>
        {
            entity.HasKey(e => e.ApplicationId).HasName("PK__RoomAppl__C93A4C9908212DF6");

            entity.HasIndex(e => e.Status, "IX_Applications_Status");

            entity.HasIndex(e => e.StudentId, "IX_Applications_Student");

            entity.HasIndex(e => e.ApplicationCode, "UQ_Applications_Code").IsUnique();

            entity.Property(e => e.ApplicationCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ApplicationDate).HasDefaultValueSql("(sysutcdatetime())", "DF_Applications_Date");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Applications_CreatedAt");
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.ReviewNote).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending", "DF_Applications_Status");

            entity.HasOne(d => d.PreferredBed).WithMany(p => p.RoomApplications)
                .HasForeignKey(d => d.PreferredBedId)
                .HasConstraintName("FK_Applications_Bed");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.RoomApplications)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK_Applications_Reviewer");

            entity.HasOne(d => d.Room).WithMany(p => p.RoomApplications)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Applications_Room");

            entity.HasOne(d => d.Student).WithMany(p => p.RoomApplications)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Applications_Student");
        });

        modelBuilder.Entity<RoomAssignment>(entity =>
        {
            entity.HasKey(e => e.AssignmentId).HasName("PK__RoomAssi__32499E77C616BA7A");

            entity.Property(e => e.AssignmentType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Assignments_CreatedAt");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active", "DF_Assignments_Status");

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.RoomAssignments)
                .HasForeignKey(d => d.AssignedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Assignments_Manager");

            entity.HasOne(d => d.Bed).WithMany(p => p.RoomAssignments)
                .HasForeignKey(d => d.BedId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Assignments_Bed");

            entity.HasOne(d => d.Room).WithMany(p => p.RoomAssignments)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Assignments_Room");

            entity.HasOne(d => d.Student).WithMany(p => p.RoomAssignments)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Assignments_Student");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Students__32C52B999B7FCC58");

            entity.HasIndex(e => e.FullName, "IX_Students_FullName");

            entity.HasIndex(e => e.Status, "IX_Students_Status");

            entity.HasIndex(e => e.Email, "UQ_Students_Email").IsUnique();

            entity.HasIndex(e => e.StudentCode, "UQ_Students_StudentCode").IsUnique();

            entity.HasIndex(e => e.UserId, "UQ_Students_UserId").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.Campus).HasMaxLength(100);
            entity.Property(e => e.ClassName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Students_CreatedAt");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.EmergencyContactName).HasMaxLength(100);
            entity.Property(e => e.EmergencyContactPhone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Major).HasMaxLength(100);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active", "DF_Students_Status");
            entity.Property(e => e.StudentCode)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithOne(p => p.Student)
                .HasForeignKey<Student>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Students_Users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C6C4297B2");

            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.HasIndex(e => e.Username, "UQ_Users_Username").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Users_CreatedAt");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Users_IsActive");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwMonthlyRevenue>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_MonthlyRevenue");

            entity.Property(e => e.TotalRevenue).HasColumnType("decimal(38, 2)");
        });

        modelBuilder.Entity<VwRoomOccupancy>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_RoomOccupancy");

            entity.Property(e => e.BuildingName).HasMaxLength(100);
            entity.Property(e => e.MonthlyRent).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RoomNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwStudentRoomInformation>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_StudentRoomInformation");

            entity.Property(e => e.AssignmentStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.BedCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.BuildingName).HasMaxLength(100);
            entity.Property(e => e.ClassName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.RoomNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.StudentCode)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
