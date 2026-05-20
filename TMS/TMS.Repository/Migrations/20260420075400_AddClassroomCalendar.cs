using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class AddClassroomCalendar : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClassSeries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: true),
                    UnitId = table.Column<int>(type: "int", nullable: true),
                    FacultyId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    RecurrenceRule = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MeetingProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassSeries_CourseMaster_CourseId",
                        column: x => x.CourseId,
                        principalTable: "CourseMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassSeries_SubjectMaster_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "SubjectMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassSeries_UnitMaster_UnitId",
                        column: x => x.UnitId,
                        principalTable: "UnitMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassSeries_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassSeries_UserMaster_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassSeries_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ClassInstances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeriesId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: true),
                    UnitId = table.Column<int>(type: "int", nullable: true),
                    FacultyId = table.Column<int>(type: "int", nullable: false),
                    ScheduledStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MeetingProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderMeetingId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    JoinUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HostUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecordingUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassInstances_ClassSeries_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "ClassSeries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassInstances_CourseMaster_CourseId",
                        column: x => x.CourseId,
                        principalTable: "CourseMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassInstances_SubjectMaster_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "SubjectMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassInstances_UnitMaster_UnitId",
                        column: x => x.UnitId,
                        principalTable: "UnitMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassInstances_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassInstances_UserMaster_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassInstances_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ClassAttendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassInstanceId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    AttendanceStatus = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeftAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassAttendances_ClassInstances_ClassInstanceId",
                        column: x => x.ClassInstanceId,
                        principalTable: "ClassInstances",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassAttendances_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassAttendances_UserMaster_StudentId",
                        column: x => x.StudentId,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassAttendances_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassAttendances_ClassInstanceId",
                table: "ClassAttendances",
                column: "ClassInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassAttendances_CreatedBy",
                table: "ClassAttendances",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ClassAttendances_StudentId",
                table: "ClassAttendances",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassAttendances_UpdatedBy",
                table: "ClassAttendances",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ClassInstances_CourseId",
                table: "ClassInstances",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassInstances_CreatedBy",
                table: "ClassInstances",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ClassInstances_FacultyId",
                table: "ClassInstances",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassInstances_SeriesId",
                table: "ClassInstances",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassInstances_SubjectId",
                table: "ClassInstances",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassInstances_UnitId",
                table: "ClassInstances",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassInstances_UpdatedBy",
                table: "ClassInstances",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSeries_CourseId",
                table: "ClassSeries",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSeries_CreatedBy",
                table: "ClassSeries",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSeries_FacultyId",
                table: "ClassSeries",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSeries_SubjectId",
                table: "ClassSeries",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSeries_UnitId",
                table: "ClassSeries",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSeries_UpdatedBy",
                table: "ClassSeries",
                column: "UpdatedBy");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassAttendances");

            migrationBuilder.DropTable(
                name: "ClassInstances");

            migrationBuilder.DropTable(
                name: "ClassSeries");
        }
    }
}
