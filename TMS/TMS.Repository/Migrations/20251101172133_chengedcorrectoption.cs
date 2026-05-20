using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class chengedcorrectoption : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCorrectOption",
                table: "QuizOption",
                type: "bit",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 5);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCorrectOption",
                table: "QuizOption");
        }
    }
}
