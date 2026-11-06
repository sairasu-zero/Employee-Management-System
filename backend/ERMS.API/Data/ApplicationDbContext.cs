using Microsoft.EntityFrameworkCore;

namespace ERMS.API.Data;

/// <summary>
/// Primary Database Context for the Employee Records Management System (ERMS).
/// 
/// Phase 1 sets up the EF Core context and PostgreSQL provider connectivity.
/// Subsequent phases will incrementally introduce the core ERMS entity models:
///   1.  Employees
///   2.  Users
///   3.  Roles
///   4.  Permissions
///   5.  UserRoles
///   6.  RolePermissions
///   7.  Departments
///   8.  EmployeeDocuments
///   9.  EmploymentHistories
///   10. MFAConfigurations
///   11. RecoveryCodes
///   12. AuditLogs
///   13. LoginAttempts
///   14. RefreshTokens
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Standard convention for PostgreSQL:
        // Entity configurations will be added here or via IEntityTypeConfiguration classes
        // in the Models/ or Data/Configurations/ directory as modules are developed.
    }
}
