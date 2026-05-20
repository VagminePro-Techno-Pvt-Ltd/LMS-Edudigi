using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class studentenrollmentchanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentQuizResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    QuizId = table.Column<int>(type: "int", nullable: false),
                    AttemptedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalQuestions = table.Column<int>(type: "int", nullable: false),
                    CorrectAnswers = table.Column<int>(type: "int", nullable: false),
                    ScorePercent = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentQuizResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentQuizResponses_QuizMaster_QuizId",
                        column: x => x.QuizId,
                        principalTable: "QuizMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentQuizResponses_UserMaster_StudentId",
                        column: x => x.StudentId,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StudentQuizAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentQuizResponseId = table.Column<int>(type: "int", nullable: false),
                    QuizQuestionId = table.Column<int>(type: "int", nullable: false),
                    SelectedOptionId = table.Column<int>(type: "int", nullable: true),
                    WrittenAnswer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentQuizAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentQuizAnswers_QuizOption_SelectedOptionId",
                        column: x => x.SelectedOptionId,
                        principalTable: "QuizOption",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentQuizAnswers_QuizQuestion_QuizQuestionId",
                        column: x => x.QuizQuestionId,
                        principalTable: "QuizQuestion",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentQuizAnswers_StudentQuizResponses_StudentQuizResponseId",
                        column: x => x.StudentQuizResponseId,
                        principalTable: "StudentQuizResponses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuizAnswers_QuizQuestionId",
                table: "StudentQuizAnswers",
                column: "QuizQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuizAnswers_SelectedOptionId",
                table: "StudentQuizAnswers",
                column: "SelectedOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuizAnswers_StudentQuizResponseId",
                table: "StudentQuizAnswers",
                column: "StudentQuizResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuizResponses_QuizId",
                table: "StudentQuizResponses",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuizResponses_StudentId",
                table: "StudentQuizResponses",
                column: "StudentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentQuizAnswers");

            migrationBuilder.DropTable(
                name: "StudentQuizResponses");
        }
    }
}
