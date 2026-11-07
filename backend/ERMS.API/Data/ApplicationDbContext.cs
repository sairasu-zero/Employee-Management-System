using ERMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ERMS.API.Data;

/// <summary>
/// Primary Database Context for the Employee Records Management System (ERMS).
/// Encapsulates PostgreSQL relational mappings for workforce records, authentication,
/// role-based access control (RBAC), and security audit trails.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Core Workforce Entities
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<EmploymentHistory> EmploymentHistories => Set<EmploymentHistory>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    // Identity & Authentication
    public DbSet<User> Users => Set<User>();
    public DbSet<MfaConfiguration> MfaConfigurations => Set<MfaConfiguration>();
    public DbSet<RecoveryCode> RecoveryCodes => Set<RecoveryCode>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();

    // Role-Based Access Control (RBAC)
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // Security & Compliance
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Composite Key Configurations
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });

            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            entity.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 2. User & Employee Linkage
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.PasswordResetTokenHash).IsUnique();
            entity.Property(u => u.PasswordResetTokenHash).HasMaxLength(64);

            entity.HasOne(u => u.Employee)
                .WithOne(e => e.User)
                .HasForeignKey<User>(u => u.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(u => u.MfaConfiguration)
                .WithOne(m => m.User)
                .HasForeignKey<MfaConfiguration>(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.EmployeeNumber).IsUnique();
            entity.HasIndex(e => e.Email);

            entity.HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasIndex(d => d.Code).IsUnique();

            entity.HasOne(d => d.ManagerEmployee)
                .WithMany()
                .HasForeignKey(d => d.ManagerEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EmploymentHistory>(entity =>
        {
            entity.HasOne(eh => eh.PreviousDepartment)
                .WithMany(d => d.PreviousDepartmentHistories)
                .HasForeignKey(eh => eh.PreviousDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(eh => eh.NewDepartment)
                .WithMany(d => d.NewDepartmentHistories)
                .HasForeignKey(eh => eh.NewDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // 3. Security & Audit Indexing
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(a => a.Timestamp);
            entity.HasIndex(a => a.Category);
            entity.HasIndex(a => a.ActivityType);
            entity.HasIndex(a => a.UserId);
        });

        modelBuilder.Entity<LoginAttempt>(entity =>
        {
            entity.HasIndex(l => l.UsernameOrEmail);
            entity.HasIndex(l => l.AttemptedAt);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(r => r.TokenHash);
            entity.HasIndex(r => r.UserId);
        });

        modelBuilder.Entity<RecoveryCode>(entity =>
        {
            entity.HasIndex(rc => rc.UserId);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasIndex(p => p.Name).IsUnique();
        });

        // 4. Leave Management & Document Tracking
        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasIndex(lr => lr.EmployeeId);
            entity.HasIndex(lr => lr.Status);
            entity.HasIndex(lr => lr.StartDate);
            entity.HasIndex(lr => lr.CreatedAt);

            entity.HasOne(lr => lr.Employee)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(lr => lr.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmployeeDocument>(entity =>
        {
            entity.HasIndex(ed => ed.EmployeeId);
            entity.HasIndex(ed => ed.IsDeleted);
            entity.HasQueryFilter(ed => !ed.IsDeleted);

            entity.HasOne(ed => ed.Employee)
                .WithMany(e => e.Documents)
                .HasForeignKey(ed => ed.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
