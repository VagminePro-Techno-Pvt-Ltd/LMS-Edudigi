using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    /// <inheritdoc />
    public partial class DiagnoseInternalAssessmentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentQuizAnswer_StudentQuizAttempt_StudentQuizAttemptId",
                table: "StudentQuizAnswer");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentQuizAnswer_StudentQuizResponses_StudentQuizResponseId",
                table: "StudentQuizAnswer");

            migrationBuilder.DropIndex(
                name: "IX_StudentQuizAnswer_StudentQuizResponseId",
                table: "StudentQuizAnswer");

            migrationBuilder.DropColumn(
                name: "StudentQuizResponseId",
                table: "StudentQuizAnswer");

            migrationBuilder.CreateTable(
                name: "ExamMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    FacultyId = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    TotalMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PassingMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AllowedAttempts = table.Column<int>(type: "int", nullable: false),
                    ShuffleQuestions = table.Column<bool>(type: "bit", nullable: false),
                    ShowResultImmediately = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamMaster_CourseMaster_CourseId",
                        column: x => x.CourseId,
                        principalTable: "CourseMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamMaster_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamMaster_UserMaster_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamMaster_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InternalAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxMarks = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AllowLateSubmission = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    ResultsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalAssessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InternalAssessmentSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    AnswerText = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubmittedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsLate = table.Column<bool>(type: "bit", nullable: false),
                    Marks = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Feedback = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EvaluatedBy = table.Column<int>(type: "int", nullable: true),
                    EvaluatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalAssessmentSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LectureVideoProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    LectureMaterialId = table.Column<int>(type: "int", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    WatchedSeconds = table.Column<int>(type: "int", nullable: false),
                    ProgressPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    LastPositionSeconds = table.Column<int>(type: "int", nullable: false),
                    LastWatchedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LectureVideoProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LectureVideoProgress_LectureMaterial_LectureMaterialId",
                        column: x => x.LectureMaterialId,
                        principalTable: "LectureMaterial",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LectureVideoProgress_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LectureVideoProgress_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuestionBank",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    FacultyId = table.Column<int>(type: "int", nullable: false),
                    QuestionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DifficultyLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Marks = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NegativeMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionBank", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionBank_CourseMaster_CourseId",
                        column: x => x.CourseId,
                        principalTable: "CourseMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QuestionBank_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QuestionBank_UserMaster_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QuestionBank_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExamAssignment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    BatchId = table.Column<int>(type: "int", nullable: true),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    AssignedBy = table.Column<int>(type: "int", nullable: false),
                    AssignedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAssignment_ExamMaster_ExamId",
                        column: x => x.ExamId,
                        principalTable: "ExamMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamAssignment_UserMaster_AssignedBy",
                        column: x => x.AssignedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamAssignment_UserMaster_StudentId",
                        column: x => x.StudentId,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExamSection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    SectionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TotalQuestions = table.Column<int>(type: "int", nullable: false),
                    TotalMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSection_ExamMaster_ExamId",
                        column: x => x.ExamId,
                        principalTable: "ExamMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StudentExamAttempt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemainingSeconds = table.Column<int>(type: "int", nullable: false),
                    IsSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Percentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ResultStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentExamAttempt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentExamAttempt_ExamMaster_ExamId",
                        column: x => x.ExamId,
                        principalTable: "ExamMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentExamAttempt_UserMaster_StudentId",
                        column: x => x.StudentId,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuestionOption",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    OptionText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionOption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionOption_QuestionBank_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QuestionBank",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExamQuestionMap",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamQuestionMap", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamQuestionMap_ExamMaster_ExamId",
                        column: x => x.ExamId,
                        principalTable: "ExamMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamQuestionMap_ExamSection_SectionId",
                        column: x => x.SectionId,
                        principalTable: "ExamSection",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamQuestionMap_QuestionBank_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QuestionBank",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExamResult",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttemptId = table.Column<int>(type: "int", nullable: false),
                    TotalQuestions = table.Column<int>(type: "int", nullable: false),
                    AttemptedQuestions = table.Column<int>(type: "int", nullable: false),
                    CorrectAnswers = table.Column<int>(type: "int", nullable: false),
                    WrongAnswers = table.Column<int>(type: "int", nullable: false),
                    ObtainedMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ResultStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PublishedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamResult_StudentExamAttempt_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "StudentExamAttempt",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FacultyEvaluation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttemptId = table.Column<int>(type: "int", nullable: false),
                    FacultyId = table.Column<int>(type: "int", nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvaluatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalMarksAwarded = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultyEvaluation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacultyEvaluation_StudentExamAttempt_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "StudentExamAttempt",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FacultyEvaluation_UserMaster_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StudentExamAnswer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttemptId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    SelectedOptionId = table.Column<int>(type: "int", nullable: true),
                    SubjectiveAnswer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObtainedMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentExamAnswer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentExamAnswer_QuestionBank_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QuestionBank",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentExamAnswer_QuestionOption_SelectedOptionId",
                        column: x => x.SelectedOptionId,
                        principalTable: "QuestionOption",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentExamAnswer_StudentExamAttempt_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "StudentExamAttempt",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAssignment_AssignedBy",
                table: "ExamAssignment",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAssignment_ExamId",
                table: "ExamAssignment",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAssignment_StudentId",
                table: "ExamAssignment",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamMaster_CourseId",
                table: "ExamMaster",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamMaster_CreatedBy",
                table: "ExamMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExamMaster_FacultyId",
                table: "ExamMaster",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamMaster_UpdatedBy",
                table: "ExamMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestionMap_ExamId",
                table: "ExamQuestionMap",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestionMap_QuestionId",
                table: "ExamQuestionMap",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestionMap_SectionId",
                table: "ExamQuestionMap",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamResult_AttemptId",
                table: "ExamResult",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSection_ExamId",
                table: "ExamSection",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_FacultyEvaluation_AttemptId",
                table: "FacultyEvaluation",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_FacultyEvaluation_FacultyId",
                table: "FacultyEvaluation",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_LectureVideoProgress_CreatedBy",
                table: "LectureVideoProgress",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LectureVideoProgress_LectureMaterialId",
                table: "LectureVideoProgress",
                column: "LectureMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_LectureVideoProgress_Student_Material",
                table: "LectureVideoProgress",
                columns: new[] { "StudentId", "LectureMaterialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LectureVideoProgress_UpdatedBy",
                table: "LectureVideoProgress",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBank_CourseId",
                table: "QuestionBank",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBank_CreatedBy",
                table: "QuestionBank",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBank_FacultyId",
                table: "QuestionBank",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBank_UpdatedBy",
                table: "QuestionBank",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOption_QuestionId",
                table: "QuestionOption",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamAnswer_AttemptId",
                table: "StudentExamAnswer",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamAnswer_QuestionId",
                table: "StudentExamAnswer",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamAnswer_SelectedOptionId",
                table: "StudentExamAnswer",
                column: "SelectedOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamAttempt_ExamId",
                table: "StudentExamAttempt",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamAttempt_StudentId",
                table: "StudentExamAttempt",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentQuizAnswer_StudentQuizAttempt_StudentQuizAttemptId",
                table: "StudentQuizAnswer",
                column: "StudentQuizAttemptId",
                principalTable: "StudentQuizAttempt",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentQuizAnswer_StudentQuizAttempt_StudentQuizAttemptId",
                table: "StudentQuizAnswer");

            migrationBuilder.DropTable(
                name: "ExamAssignment");

            migrationBuilder.DropTable(
                name: "ExamQuestionMap");

            migrationBuilder.DropTable(
                name: "ExamResult");

            migrationBuilder.DropTable(
                name: "FacultyEvaluation");

            migrationBuilder.DropTable(
                name: "InternalAssessments");

            migrationBuilder.DropTable(
                name: "InternalAssessmentSubmissions");

            migrationBuilder.DropTable(
                name: "LectureVideoProgress");

            migrationBuilder.DropTable(
                name: "StudentExamAnswer");

            migrationBuilder.DropTable(
                name: "ExamSection");

            migrationBuilder.DropTable(
                name: "QuestionOption");

            migrationBuilder.DropTable(
                name: "StudentExamAttempt");

            migrationBuilder.DropTable(
                name: "QuestionBank");

            migrationBuilder.DropTable(
                name: "ExamMaster");

            migrationBuilder.AddColumn<int>(
                name: "StudentQuizResponseId",
                table: "StudentQuizAnswer",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuizAnswer_StudentQuizResponseId",
                table: "StudentQuizAnswer",
                column: "StudentQuizResponseId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentQuizAnswer_StudentQuizAttempt_StudentQuizAttemptId",
                table: "StudentQuizAnswer",
                column: "StudentQuizAttemptId",
                principalTable: "StudentQuizAttempt",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentQuizAnswer_StudentQuizResponses_StudentQuizResponseId",
                table: "StudentQuizAnswer",
                column: "StudentQuizResponseId",
                principalTable: "StudentQuizResponses",
                principalColumn: "Id");
        }
    }
}
