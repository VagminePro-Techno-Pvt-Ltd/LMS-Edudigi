using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class changedSemester : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "SemesterMaster");

            migrationBuilder.DropColumn(
                name: "SequenceNo",
                table: "SemesterMaster");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "SemesterMaster");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "SemesterMaster",
                type: "datetime2",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 5);

            migrationBuilder.AddColumn<int>(
                name: "SequenceNo",
                table: "SemesterMaster",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("Relational:ColumnOrder", 3);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "SemesterMaster",
                type: "datetime2",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 4);
        }
    }
}
