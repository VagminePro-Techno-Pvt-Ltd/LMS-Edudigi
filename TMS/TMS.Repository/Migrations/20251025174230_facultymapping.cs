using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class facultymapping : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseFacultyMaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    FacultyId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseFacultyMaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseFacultyMaps_CourseMaster_CourseId",
                        column: x => x.CourseId,
                        principalTable: "CourseMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseFacultyMaps_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseFacultyMaps_UserMaster_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseFacultyMaps_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "FormMaster",
                columns: new[] { "Id", "Action", "Area", "Controller", "Description", "ExcludeFromMenu", "IconClass", "IsAdmin", "MenuId", "Name", "Sequence" },
                values: new object[] { 10, "Index", "Admin", "FacultyMapping", null, false, "fas fa-discourse", true, 1, "Faculty Mapping", 13 });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "FormId", "RoleId", "Add", "Edit", "View" },
                values: new object[] { 10, 1, true, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_CourseFacultyMaps_CourseId",
                table: "CourseFacultyMaps",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseFacultyMaps_CreatedBy",
                table: "CourseFacultyMaps",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CourseFacultyMaps_FacultyId",
                table: "CourseFacultyMaps",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseFacultyMaps_UpdatedBy",
                table: "CourseFacultyMaps",
                column: "UpdatedBy");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseFacultyMaps");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "FormId", "RoleId" },
                keyValues: new object[] { 10, 1 });

            migrationBuilder.DeleteData(
                table: "FormMaster",
                keyColumn: "Id",
                keyValue: 10);
        }
    }
}
