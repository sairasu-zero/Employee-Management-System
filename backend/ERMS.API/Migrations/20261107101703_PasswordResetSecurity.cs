using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERMS.API.Migrations
{
    /// <inheritdoc />
    public partial class PasswordResetSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiresAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetTokenHash",
                table: "Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionVersion",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DocumentName",
                table: "EmployeeDocuments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                table: "EmployeeDocuments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "EmployeeDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "EmployeeDocuments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StoragePath",
                table: "EmployeeDocuments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StoredFileName",
                table: "EmployeeDocuments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "EmployeeDocuments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaveType = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Days = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_PasswordResetTokenHash",
                table: "Users",
                column: "PasswordResetTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_IsDeleted",
                table: "EmployeeDocuments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_CreatedAt",
                table: "LeaveRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmployeeId",
                table: "LeaveRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_StartDate",
                table: "LeaveRequests",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_Status",
                table: "LeaveRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_Users_PasswordResetTokenHash",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeDocuments_IsDeleted",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiresAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SessionVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DocumentName",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "StoragePath",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "StoredFileName",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "EmployeeDocuments");
        }
    }
}
