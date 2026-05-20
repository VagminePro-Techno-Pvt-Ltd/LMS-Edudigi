using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class QuizAssesment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FacultyQuizAssessment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttemptId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    StudentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuadrantId = table.Column<int>(type: "int", nullable: false),
                    QuadrantName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuizName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttemptedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultyQuizAssessment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacultyQuizAssessment_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FacultyQuizAssessment_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FacultyQuizQuestionAssessment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StudentScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FacultyQuizAssessmentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultyQuizQuestionAssessment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacultyQuizQuestionAssessment_FacultyQuizAssessment_FacultyQuizAssessmentId",
                        column: x => x.FacultyQuizAssessmentId,
                        principalTable: "FacultyQuizAssessment",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FacultyQuizQuestionAssessment_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FacultyQuizQuestionAssessment_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacultyQuizAssessment_CreatedBy",
                table: "FacultyQuizAssessment",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FacultyQuizAssessment_UpdatedBy",
                table: "FacultyQuizAssessment",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FacultyQuizQuestionAssessment_CreatedBy",
                table: "FacultyQuizQuestionAssessment",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FacultyQuizQuestionAssessment_FacultyQuizAssessmentId",
                table: "FacultyQuizQuestionAssessment",
                column: "FacultyQuizAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FacultyQuizQuestionAssessment_UpdatedBy",
                table: "FacultyQuizQuestionAssessment",
                column: "UpdatedBy");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacultyQuizQuestionAssessment");

            migrationBuilder.DropTable(
                name: "FacultyQuizAssessment");
        }
    }
}
