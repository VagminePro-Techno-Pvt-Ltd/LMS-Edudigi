using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class ModernizeEnrollmentLifecycle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollment_CourseId",
                table: "CourseEnrollment");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedOn",
                table: "CourseEnrollment",
                type: "datetime2",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 7);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedOn",
                table: "CourseEnrollment",
                type: "datetime2",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 9);

            migrationBuilder.AddColumn<DateTime>(
                name: "DroppedOn",
                table: "CourseEnrollment",
                type: "datetime2",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 8);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "CourseEnrollment",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("Relational:ColumnOrder", 10);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "CourseEnrollment",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("Relational:ColumnOrder", 6);

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollment_CourseId_StudentId",
                table: "CourseEnrollment",
                columns: new[] { "CourseId", "StudentId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollment_CourseId_StudentId",
                table: "CourseEnrollment");

            migrationBuilder.DropColumn(
                name: "ApprovedOn",
                table: "CourseEnrollment");

            migrationBuilder.DropColumn(
                name: "CompletedOn",
                table: "CourseEnrollment");

            migrationBuilder.DropColumn(
                name: "DroppedOn",
                table: "CourseEnrollment");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "CourseEnrollment");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CourseEnrollment");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollment_CourseId",
                table: "CourseEnrollment",
                column: "CourseId");
        }
    }
}
