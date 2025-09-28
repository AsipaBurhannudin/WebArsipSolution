using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebArsip.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPermissionNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DocumentDocId",
                table: "UserPermissions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_DocumentDocId",
                table: "UserPermissions",
                column: "DocumentDocId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserPermissions_Documents_DocumentDocId",
                table: "UserPermissions",
                column: "DocumentDocId",
                principalTable: "Documents",
                principalColumn: "DocId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserPermissions_Documents_DocumentDocId",
                table: "UserPermissions");

            migrationBuilder.DropIndex(
                name: "IX_UserPermissions_DocumentDocId",
                table: "UserPermissions");

            migrationBuilder.DropColumn(
                name: "DocumentDocId",
                table: "UserPermissions");
        }
    }
}
