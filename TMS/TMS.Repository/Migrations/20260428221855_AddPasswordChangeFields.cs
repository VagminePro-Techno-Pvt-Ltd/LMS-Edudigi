using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class AddPasswordChangeFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ForcePasswordChange",
                table: "UserMaster",
                type: "bit",
                nullable: false,
                defaultValue: false)
                .Annotation("Relational:ColumnOrder", 7);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordLastChanged",
                table: "UserMaster",
                type: "datetime2",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 8);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ForcePasswordChange",
                table: "UserMaster");

            migrationBuilder.DropColumn(
                name: "PasswordLastChanged",
                table: "UserMaster");
        }
    }
}
