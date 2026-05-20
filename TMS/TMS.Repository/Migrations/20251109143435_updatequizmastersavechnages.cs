using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class updatequizmastersavechnages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "StudentQuizAttempt",
                type: "int",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 8);

            migrationBuilder.AddColumn<int>(
                name: "CourseQuadrantId",
                table: "StudentQuizAttempt",
                type: "int",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 9);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "StudentQuizAttempt");

            migrationBuilder.DropColumn(
                name: "CourseQuadrantId",
                table: "StudentQuizAttempt");
        }
    }
}
