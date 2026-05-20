using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class studentenrollementchanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old FK first
            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollment_UserMaster_UserId",
                table: "CourseEnrollment");

            // Rename column from UserId -> StudentId
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "CourseEnrollment",
                newName: "StudentId");

            // --- FIXED: Rename index manually using SQL to avoid ambiguity ---
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CourseEnrollment_UserId')
                BEGIN
                    EXEC sp_rename N'dbo.IX_CourseEnrollment_UserId', N'IX_CourseEnrollment_StudentId', N'INDEX';
                END
            ");

            // Recreate foreign key with new column name
            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollment_UserMaster_StudentId",
                table: "CourseEnrollment",
                column: "StudentId",
                principalTable: "UserMaster",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop FK with StudentId
            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollment_UserMaster_StudentId",
                table: "CourseEnrollment");

            // Rename column back
            migrationBuilder.RenameColumn(
                name: "StudentId",
                table: "CourseEnrollment",
                newName: "UserId");

            // --- FIXED: Rename index manually back using SQL ---
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CourseEnrollment_StudentId')
                BEGIN
                    EXEC sp_rename N'dbo.IX_CourseEnrollment_StudentId', N'IX_CourseEnrollment_UserId', N'INDEX';
                END
            ");

            // Restore old FK
            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollment_UserMaster_UserId",
                table: "CourseEnrollment",
                column: "UserId",
                principalTable: "UserMaster",
                principalColumn: "Id");
        }
    }
}
