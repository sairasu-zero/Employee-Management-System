-- Employee Records Management System (ERMS)
-- Database Initialization & Migration Script for PostgreSQL

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Roles
CREATE TABLE IF NOT EXISTS "Roles" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "Description" text,
    "IsSystemRole" boolean NOT NULL DEFAULT FALSE,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_Roles" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Roles_Name" ON "Roles" ("Name");

-- Permissions
CREATE TABLE IF NOT EXISTS "Permissions" (
    "Id" uuid NOT NULL,
    "Module" text NOT NULL,
    "Action" text NOT NULL,
    "Name" text NOT NULL,
    "Description" text,
    CONSTRAINT "PK_Permissions" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Permissions_Name" ON "Permissions" ("Name");

-- RolePermissions
CREATE TABLE IF NOT EXISTS "RolePermissions" (
    "RoleId" uuid NOT NULL,
    "PermissionId" uuid NOT NULL,
    "GrantedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "GrantedBy" text,
    CONSTRAINT "PK_RolePermissions" PRIMARY KEY ("RoleId", "PermissionId"),
    CONSTRAINT "FK_RolePermissions_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_RolePermissions_Permissions_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES "Permissions" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_RolePermissions_PermissionId" ON "RolePermissions" ("PermissionId");

-- Departments
CREATE TABLE IF NOT EXISTS "Departments" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "Code" text NOT NULL,
    "Description" text,
    "ManagerEmployeeId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_Departments" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Departments_Code" ON "Departments" ("Code");

-- Employees
CREATE TABLE IF NOT EXISTS "Employees" (
    "Id" uuid NOT NULL,
    "EmployeeNumber" text NOT NULL,
    "FirstName" text NOT NULL,
    "MiddleName" text,
    "LastName" text NOT NULL,
    "Email" text NOT NULL,
    "Phone" text,
    "DepartmentId" uuid,
    "Position" text NOT NULL,
    "EmploymentType" text NOT NULL,
    "HireDate" timestamp with time zone NOT NULL,
    "Status" text NOT NULL DEFAULT 'Active',
    "Address" text,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Employees" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Employees_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE SET NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Employees_EmployeeNumber" ON "Employees" ("EmployeeNumber");
CREATE INDEX IF NOT EXISTS "IX_Employees_Email" ON "Employees" ("Email");
CREATE INDEX IF NOT EXISTS "IX_Employees_DepartmentId" ON "Employees" ("DepartmentId");

-- Add foreign key from Departments to Employees for ManagerEmployeeId
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Departments_Employees_ManagerEmployeeId'
    ) THEN
        ALTER TABLE "Departments"
        ADD CONSTRAINT "FK_Departments_Employees_ManagerEmployeeId"
        FOREIGN KEY ("ManagerEmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL;
    END IF;
END $$;
CREATE INDEX IF NOT EXISTS "IX_Departments_ManagerEmployeeId" ON "Departments" ("ManagerEmployeeId");

-- EmploymentHistories
CREATE TABLE IF NOT EXISTS "EmploymentHistories" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "PreviousPosition" text,
    "NewPosition" text NOT NULL,
    "PreviousDepartmentId" uuid,
    "NewDepartmentId" uuid,
    "ChangeType" text NOT NULL,
    "ChangeDate" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Remarks" text,
    "ApprovedBy" text,
    CONSTRAINT "PK_EmploymentHistories" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmploymentHistories_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_EmploymentHistories_Departments_PreviousDepartmentId" FOREIGN KEY ("PreviousDepartmentId") REFERENCES "Departments" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_EmploymentHistories_Departments_NewDepartmentId" FOREIGN KEY ("NewDepartmentId") REFERENCES "Departments" ("Id") ON DELETE SET NULL
);
CREATE INDEX IF NOT EXISTS "IX_EmploymentHistories_EmployeeId" ON "EmploymentHistories" ("EmployeeId");
CREATE INDEX IF NOT EXISTS "IX_EmploymentHistories_PreviousDepartmentId" ON "EmploymentHistories" ("PreviousDepartmentId");
CREATE INDEX IF NOT EXISTS "IX_EmploymentHistories_NewDepartmentId" ON "EmploymentHistories" ("NewDepartmentId");

-- Users
CREATE TABLE IF NOT EXISTS "Users" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid,
    "Username" text NOT NULL,
    "Email" text NOT NULL,
    "PasswordHash" text NOT NULL,
    "PasswordSalt" text NOT NULL,
    "RoleId" uuid,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "RequireMfa" boolean NOT NULL DEFAULT FALSE,
    "LastLoginAt" timestamp with time zone,
    "FailedLoginAttempts" integer NOT NULL DEFAULT 0,
    "LockoutEnd" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Users_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Users_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE SET NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Username" ON "Users" ("Username");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_EmployeeId" ON "Users" ("EmployeeId");
CREATE INDEX IF NOT EXISTS "IX_Users_RoleId" ON "Users" ("RoleId");

-- UserRoles
CREATE TABLE IF NOT EXISTS "UserRoles" (
    "UserId" uuid NOT NULL,
    "RoleId" uuid NOT NULL,
    "AssignedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "AssignedBy" text,
    CONSTRAINT "PK_UserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_UserRoles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserRoles_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_UserRoles_RoleId" ON "UserRoles" ("RoleId");

-- MfaConfigurations
CREATE TABLE IF NOT EXISTS "MfaConfigurations" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "SecretEncrypted" text NOT NULL,
    "IsEnabled" boolean NOT NULL DEFAULT FALSE,
    "EnrolledAt" timestamp with time zone,
    "LastVerifiedAt" timestamp with time zone,
    CONSTRAINT "PK_MfaConfigurations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_MfaConfigurations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_MfaConfigurations_UserId" ON "MfaConfigurations" ("UserId");

-- RecoveryCodes
CREATE TABLE IF NOT EXISTS "RecoveryCodes" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "CodeHash" text NOT NULL,
    "IsUsed" boolean NOT NULL DEFAULT FALSE,
    "UsedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_RecoveryCodes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RecoveryCodes_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_RecoveryCodes_UserId" ON "RecoveryCodes" ("UserId");

-- RefreshTokens
CREATE TABLE IF NOT EXISTS "RefreshTokens" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "TokenHash" text NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "RevokedAt" timestamp with time zone,
    CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_TokenHash" ON "RefreshTokens" ("TokenHash");

-- AuditLogs
CREATE TABLE IF NOT EXISTS "AuditLogs" (
    "Id" uuid NOT NULL,
    "UserId" uuid,
    "UserIdentity" text,
    "ActivityType" text NOT NULL,
    "Category" text NOT NULL,
    "Description" text NOT NULL,
    "TargetResource" text,
    "ActionResult" text NOT NULL,
    "IpAddress" text,
    "Timestamp" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AuditLogs_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
);
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_UserId" ON "AuditLogs" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Category" ON "AuditLogs" ("Category");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_ActivityType" ON "AuditLogs" ("ActivityType");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Timestamp" ON "AuditLogs" ("Timestamp");

-- LoginAttempts
CREATE TABLE IF NOT EXISTS "LoginAttempts" (
    "Id" uuid NOT NULL,
    "UsernameOrEmail" text NOT NULL,
    "IsSuccessful" boolean NOT NULL,
    "FailureReason" text,
    "IpAddress" text,
    "AttemptedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_LoginAttempts" PRIMARY KEY ("Id")
);
CREATE INDEX IF NOT EXISTS "IX_LoginAttempts_UsernameOrEmail" ON "LoginAttempts" ("UsernameOrEmail");
CREATE INDEX IF NOT EXISTS "IX_LoginAttempts_AttemptedAt" ON "LoginAttempts" ("AttemptedAt");

-- EmployeeDocuments
CREATE TABLE IF NOT EXISTS "EmployeeDocuments" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "DocumentName" text NOT NULL,
    "DocumentType" text NOT NULL,
    "OriginalFileName" text NOT NULL,
    "StoredFileName" text NOT NULL,
    "ContentType" text NOT NULL,
    "FileSize" bigint NOT NULL,
    "StoragePath" text NOT NULL,
    "UploadedBy" text,
    "UploadedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT "PK_EmployeeDocuments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmployeeDocuments_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_EmployeeDocuments_EmployeeId" ON "EmployeeDocuments" ("EmployeeId");
CREATE INDEX IF NOT EXISTS "IX_EmployeeDocuments_IsDeleted" ON "EmployeeDocuments" ("IsDeleted");

-- LeaveRequests
CREATE TABLE IF NOT EXISTS "LeaveRequests" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "LeaveType" text NOT NULL,
    "StartDate" timestamp with time zone NOT NULL,
    "EndDate" timestamp with time zone NOT NULL,
    "Days" integer NOT NULL,
    "Reason" text NOT NULL,
    "Status" text NOT NULL DEFAULT 'Pending',
    "ApprovedBy" text,
    "ApprovedAt" timestamp with time zone,
    "RejectionReason" text,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_LeaveRequests" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LeaveRequests_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_LeaveRequests_EmployeeId" ON "LeaveRequests" ("EmployeeId");
CREATE INDEX IF NOT EXISTS "IX_LeaveRequests_Status" ON "LeaveRequests" ("Status");
CREATE INDEX IF NOT EXISTS "IX_LeaveRequests_StartDate" ON "LeaveRequests" ("StartDate");
CREATE INDEX IF NOT EXISTS "IX_LeaveRequests_CreatedAt" ON "LeaveRequests" ("CreatedAt");

-- Record EF Core Migration state
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261106120247_InitialERMSDb', '8.0.10')
ON CONFLICT ("MigrationId") DO NOTHING;
