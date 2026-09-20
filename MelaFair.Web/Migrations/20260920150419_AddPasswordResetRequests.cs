using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MelaFair.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VendorId",
                table: "JobPostings",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PasswordResetRequests",
                columns: table => new
                {
                    PasswordResetRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetRequests", x => x.PasswordResetRequestId);
                    table.ForeignKey(
                        name: "FK_PasswordResetRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_VendorId",
                table: "JobPostings",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetRequests_TokenHash",
                table: "PasswordResetRequests",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetRequests_UserId_ExpiresAt",
                table: "PasswordResetRequests",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_JobPostings_AspNetUsers_VendorId",
                table: "JobPostings",
                column: "VendorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobPostings_AspNetUsers_VendorId",
                table: "JobPostings");

            migrationBuilder.DropTable(
                name: "PasswordResetRequests");

            migrationBuilder.DropIndex(
                name: "IX_JobPostings_VendorId",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "JobPostings");
        }
    }
}
