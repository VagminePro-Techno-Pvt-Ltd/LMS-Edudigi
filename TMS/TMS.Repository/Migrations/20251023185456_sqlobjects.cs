using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class sqlobjects : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateSQLObjects();

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
