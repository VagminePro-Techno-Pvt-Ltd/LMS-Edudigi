using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class AddAssignmentModule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssignmentMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProgramId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PassMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AllowLateSubmission = table.Column<bool>(type: "bit", nullable: false),
                    LatePenalty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AttachmentPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentMaster_CourseCategoryMaster_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "CourseCategoryMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssignmentMaster_CourseMaster_CourseId",
                        column: x => x.CourseId,
                        principalTable: "CourseMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssignmentMaster_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssignmentMaster_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssignmentSubmission",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SubmissionText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachmentPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubmittedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentSubmission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentSubmission_AssignmentMaster_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "AssignmentMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssignmentSubmission_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssignmentSubmission_UserMaster_StudentId",
                        column: x => x.StudentId,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssignmentSubmission_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssignmentGrade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    Marks = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvaluatedBy = table.Column<int>(type: "int", nullable: false),
                    EvaluatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentGrade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentGrade_AssignmentSubmission_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "AssignmentSubmission",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssignmentGrade_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssignmentGrade_UserMaster_EvaluatedBy",
                        column: x => x.EvaluatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssignmentGrade_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentGrade_CreatedBy",
                table: "AssignmentGrade",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentGrade_EvaluatedBy",
                table: "AssignmentGrade",
                column: "EvaluatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentGrade_SubmissionId",
                table: "AssignmentGrade",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentGrade_UpdatedBy",
                table: "AssignmentGrade",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentMaster_CourseId",
                table: "AssignmentMaster",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentMaster_CreatedBy",
                table: "AssignmentMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentMaster_ProgramId",
                table: "AssignmentMaster",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentMaster_UpdatedBy",
                table: "AssignmentMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmission_AssignmentId",
                table: "AssignmentSubmission",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmission_CreatedBy",
                table: "AssignmentSubmission",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmission_StudentId",
                table: "AssignmentSubmission",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmission_UpdatedBy",
                table: "AssignmentSubmission",
                column: "UpdatedBy");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentGrade");

            migrationBuilder.DropTable(
                name: "AssignmentSubmission");

            migrationBuilder.DropTable(
                name: "AssignmentMaster");
        }
    }
}
