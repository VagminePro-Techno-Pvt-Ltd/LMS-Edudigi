using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class AddedMaterialType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FileType",
                table: "LectureMaterial",
                newName: "MaterialType");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaterialType",
                table: "LectureMaterial",
                newName: "FileType");
        }
    }
}
