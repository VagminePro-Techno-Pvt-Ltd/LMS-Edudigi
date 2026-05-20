using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class Coyrsedbchangesfields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CIE",
                table: "CourseMaster",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m)
                .Annotation("Relational:ColumnOrder", 8);

            migrationBuilder.AddColumn<decimal>(
                name: "ESE",
                table: "CourseMaster",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m)
                .Annotation("Relational:ColumnOrder", 9);

            migrationBuilder.AddColumn<int>(
                name: "NoofCredit",
                table: "CourseMaster",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("Relational:ColumnOrder", 7);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CIE",
                table: "CourseMaster");

            migrationBuilder.DropColumn(
                name: "ESE",
                table: "CourseMaster");

            migrationBuilder.DropColumn(
                name: "NoofCredit",
                table: "CourseMaster");
        }
    }
}
