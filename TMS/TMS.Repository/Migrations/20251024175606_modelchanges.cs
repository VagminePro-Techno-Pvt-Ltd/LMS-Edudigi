using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class modelchanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseQuadrantMaster_CourseMaster_CourseId",
                table: "CourseQuadrantMaster");

            migrationBuilder.DropIndex(
                name: "IX_CourseQuadrantMaster_CourseId",
                table: "CourseQuadrantMaster");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "CourseQuadrantMaster");

            migrationBuilder.RenameColumn(
                name: "QuadrantTitle",
                table: "CourseQuadrantMaster",
                newName: "Name");

            migrationBuilder.AlterColumn<int>(
                name: "QuadrantNumber",
                table: "CourseQuadrantMaster",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 3)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CourseQuadrantMaster",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50)
                .Annotation("Relational:ColumnOrder", 4)
                .OldAnnotation("Relational:ColumnOrder", 5);

            migrationBuilder.AddColumn<int>(
                name: "CourseMasterId",
                table: "CourseQuadrantMaster",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "FormMaster",
                keyColumn: "Id",
                keyValue: 7,
                column: "Name",
                value: "Semester");

            migrationBuilder.InsertData(
                table: "FormMaster",
                columns: new[] { "Id", "Action", "Area", "Controller", "Description", "ExcludeFromMenu", "IconClass", "IsAdmin", "MenuId", "Name", "Sequence" },
                values: new object[] { 8, "Index", "Admin", "QuadrantMaster", null, false, "fas fa-discourse", true, 1, "Quadrant", 11 });

            migrationBuilder.InsertData(
                table: "FormMaster",
                columns: new[] { "Id", "Action", "Area", "Controller", "Description", "ExcludeFromMenu", "IconClass", "IsAdmin", "MenuId", "Name", "Sequence" },
                values: new object[] { 9, "Index", "Admin", "LectureMaterial", null, false, "fas fa-discourse", true, 1, "Lecture Material", 12 });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "FormId", "RoleId", "Add", "Edit", "View" },
                values: new object[] { 8, 1, true, true, true });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "FormId", "RoleId", "Add", "Edit", "View" },
                values: new object[] { 9, 1, true, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_CourseQuadrantMaster_CourseMasterId",
                table: "CourseQuadrantMaster",
                column: "CourseMasterId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseQuadrantMaster_CourseMaster_CourseMasterId",
                table: "CourseQuadrantMaster",
                column: "CourseMasterId",
                principalTable: "CourseMaster",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseQuadrantMaster_CourseMaster_CourseMasterId",
                table: "CourseQuadrantMaster");

            migrationBuilder.DropIndex(
                name: "IX_CourseQuadrantMaster_CourseMasterId",
                table: "CourseQuadrantMaster");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "FormId", "RoleId" },
                keyValues: new object[] { 8, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "FormId", "RoleId" },
                keyValues: new object[] { 9, 1 });

            migrationBuilder.DeleteData(
                table: "FormMaster",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "FormMaster",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DropColumn(
                name: "CourseMasterId",
                table: "CourseQuadrantMaster");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "CourseQuadrantMaster",
                newName: "QuadrantTitle");

            migrationBuilder.AlterColumn<int>(
                name: "QuadrantNumber",
                table: "CourseQuadrantMaster",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 4)
                .OldAnnotation("Relational:ColumnOrder", 3);

            migrationBuilder.AlterColumn<string>(
                name: "QuadrantTitle",
                table: "CourseQuadrantMaster",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50)
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "CourseQuadrantMaster",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("Relational:ColumnOrder", 3);

            migrationBuilder.UpdateData(
                table: "FormMaster",
                keyColumn: "Id",
                keyValue: 7,
                column: "Name",
                value: "Semester Master");

            migrationBuilder.CreateIndex(
                name: "IX_CourseQuadrantMaster_CourseId",
                table: "CourseQuadrantMaster",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseQuadrantMaster_CourseMaster_CourseId",
                table: "CourseQuadrantMaster",
                column: "CourseId",
                principalTable: "CourseMaster",
                principalColumn: "Id");
        }
    }
}
