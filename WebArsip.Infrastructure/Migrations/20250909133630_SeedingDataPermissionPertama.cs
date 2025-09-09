using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WebArsip.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedingDataPermissionPertama : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Documents",
                columns: new[] { "DocId", "CreatedDate", "Description", "FilePath", "Status", "Title", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2025, 9, 9, 13, 36, 29, 887, DateTimeKind.Utc).AddTicks(4433), "Dokumen contoh awal untuk permission", "/uploads/docs/sample.pdf", "Active", "Panduan Arsip Awal", null });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionId", "CanDelete", "CanDownload", "CanEdit", "CanUpload", "CanView", "DocId", "RoleId" },
                values: new object[,]
                {
                    { 1, true, true, true, true, true, 1, 1 },
                    { 2, true, false, true, true, true, 1, 2 },
                    { 3, false, true, false, false, true, 1, 3 },
                    { 4, false, false, true, true, true, 1, 4 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Documents",
                keyColumn: "DocId",
                keyValue: 1);
        }
    }
}
