using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebArsip.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_Documents_DocumentDocId",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_DocumentDocId",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "DocumentDocId",
                table: "Permissions");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_DocId",
                table: "Permissions",
                column: "DocId");

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_Documents_DocId",
                table: "Permissions",
                column: "DocId",
                principalTable: "Documents",
                principalColumn: "DocId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_Documents_DocId",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_DocId",
                table: "Permissions");

            migrationBuilder.AddColumn<int>(
                name: "DocumentDocId",
                table: "Permissions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_DocumentDocId",
                table: "Permissions",
                column: "DocumentDocId");

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_Documents_DocumentDocId",
                table: "Permissions",
                column: "DocumentDocId",
                principalTable: "Documents",
                principalColumn: "DocId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
