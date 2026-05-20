using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class AddLectureVideoQuiz : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscussionComment");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "DiscussionThread",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100)
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedOn",
                table: "DiscussionThread",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2")
                .Annotation("Relational:ColumnOrder", 7)
                .OldAnnotation("Relational:ColumnOrder", 6);

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByUserId",
                table: "DiscussionThread",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 6)
                .OldAnnotation("Relational:ColumnOrder", 5);

            migrationBuilder.AlterColumn<int>(
                name: "CourseQuadrantId",
                table: "DiscussionThread",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "DiscussionThread",
                type: "bit",
                nullable: false,
                defaultValue: false)
                .Annotation("Relational:ColumnOrder", 8);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "DiscussionThread",
                type: "bit",
                nullable: false,
                defaultValue: false)
                .Annotation("Relational:ColumnOrder", 9);

            migrationBuilder.AddColumn<int>(
                name: "ProgramId",
                table: "DiscussionThread",
                type: "int",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 4);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "DiscussionThread",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("Relational:ColumnOrder", 10);

            migrationBuilder.CreateTable(
                name: "DiscussionReply",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThreadId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ParentReplyId = table.Column<int>(type: "int", nullable: true),
                    CommentText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAccepted = table.Column<bool>(type: "bit", nullable: false),
                    CommentedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscussionReply", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscussionReply_DiscussionReply_ParentReplyId",
                        column: x => x.ParentReplyId,
                        principalTable: "DiscussionReply",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscussionReply_DiscussionThread_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "DiscussionThread",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscussionReply_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscussionReply_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscussionReply_UserMaster_UserId",
                        column: x => x.UserId,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LectureQuizQuestion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LectureMaterialId = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionA = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OptionB = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OptionC = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OptionD = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CorrectOption = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    ShowAtSeconds = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LectureQuizQuestion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LectureQuizQuestion_LectureMaterial_LectureMaterialId",
                        column: x => x.LectureMaterialId,
                        principalTable: "LectureMaterial",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LectureQuizQuestion_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LectureQuizQuestion_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TopicMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopicMaster_UnitMaster_UnitId",
                        column: x => x.UnitId,
                        principalTable: "UnitMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TopicMaster_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TopicMaster_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DiscussionAttachment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThreadId = table.Column<int>(type: "int", nullable: true),
                    ReplyId = table.Column<int>(type: "int", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedBy = table.Column<int>(type: "int", nullable: false),
                    UploadedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscussionAttachment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscussionAttachment_DiscussionReply_ReplyId",
                        column: x => x.ReplyId,
                        principalTable: "DiscussionReply",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscussionAttachment_DiscussionThread_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "DiscussionThread",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscussionAttachment_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscussionAttachment_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LectureQuizAttempt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    LectureQuizQuestionId = table.Column<int>(type: "int", nullable: false),
                    SelectedOption = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    AttemptedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LectureQuizAttempt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LectureQuizAttempt_LectureQuizQuestion_LectureQuizQuestionId",
                        column: x => x.LectureQuizQuestionId,
                        principalTable: "LectureQuizQuestion",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LectureQuizAttempt_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LectureQuizAttempt_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VideoMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TopicId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VideoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoMaster_TopicMaster_TopicId",
                        column: x => x.TopicId,
                        principalTable: "TopicMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VideoMaster_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VideoMaster_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VideoProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    VideoId = table.Column<int>(type: "int", nullable: false),
                    LastWatchedSeconds = table.Column<int>(type: "int", nullable: false),
                    MaxAllowedSeconds = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoProgress_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VideoProgress_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VideoProgress_VideoMaster_VideoId",
                        column: x => x.VideoId,
                        principalTable: "VideoMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VideoQuizQuestion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VideoId = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionA = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OptionB = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OptionC = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OptionD = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CorrectOption = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    TriggerTimeSeconds = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoQuizQuestion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoQuizQuestion_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VideoQuizQuestion_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VideoQuizQuestion_VideoMaster_VideoId",
                        column: x => x.VideoId,
                        principalTable: "VideoMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VideoQuizAttempt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    VideoQuizQuestionId = table.Column<int>(type: "int", nullable: false),
                    SelectedOption = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    AttemptedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoQuizAttempt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoQuizAttempt_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VideoQuizAttempt_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VideoQuizAttempt_VideoQuizQuestion_VideoQuizQuestionId",
                        column: x => x.VideoQuizQuestionId,
                        principalTable: "VideoQuizQuestion",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionThread_ProgramId",
                table: "DiscussionThread",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionAttachment_CreatedBy",
                table: "DiscussionAttachment",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionAttachment_ReplyId",
                table: "DiscussionAttachment",
                column: "ReplyId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionAttachment_ThreadId",
                table: "DiscussionAttachment",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionAttachment_UpdatedBy",
                table: "DiscussionAttachment",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionReply_CreatedBy",
                table: "DiscussionReply",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionReply_ParentReplyId",
                table: "DiscussionReply",
                column: "ParentReplyId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionReply_ThreadId",
                table: "DiscussionReply",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionReply_UpdatedBy",
                table: "DiscussionReply",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionReply_UserId",
                table: "DiscussionReply",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LectureQuizAttempt_CreatedBy",
                table: "LectureQuizAttempt",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LectureQuizAttempt_LectureQuizQuestionId",
                table: "LectureQuizAttempt",
                column: "LectureQuizQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_LectureQuizAttempt_Student_Question",
                table: "LectureQuizAttempt",
                columns: new[] { "StudentId", "LectureQuizQuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LectureQuizAttempt_UpdatedBy",
                table: "LectureQuizAttempt",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LectureQuizQuestion_CreatedBy",
                table: "LectureQuizQuestion",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LectureQuizQuestion_LectureMaterialId",
                table: "LectureQuizQuestion",
                column: "LectureMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_LectureQuizQuestion_UpdatedBy",
                table: "LectureQuizQuestion",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TopicMaster_CreatedBy",
                table: "TopicMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TopicMaster_UnitId",
                table: "TopicMaster",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicMaster_UpdatedBy",
                table: "TopicMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VideoMaster_CreatedBy",
                table: "VideoMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VideoMaster_TopicId",
                table: "VideoMaster",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoMaster_UpdatedBy",
                table: "VideoMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VideoProgress_CreatedBy",
                table: "VideoProgress",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VideoProgress_Student_Video",
                table: "VideoProgress",
                columns: new[] { "StudentId", "VideoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoProgress_UpdatedBy",
                table: "VideoProgress",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VideoProgress_VideoId",
                table: "VideoProgress",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoQuizAttempt_CreatedBy",
                table: "VideoQuizAttempt",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VideoQuizAttempt_Student_Question",
                table: "VideoQuizAttempt",
                columns: new[] { "StudentId", "VideoQuizQuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoQuizAttempt_UpdatedBy",
                table: "VideoQuizAttempt",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VideoQuizAttempt_VideoQuizQuestionId",
                table: "VideoQuizAttempt",
                column: "VideoQuizQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoQuizQuestion_CreatedBy",
                table: "VideoQuizQuestion",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VideoQuizQuestion_UpdatedBy",
                table: "VideoQuizQuestion",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VideoQuizQuestion_VideoId",
                table: "VideoQuizQuestion",
                column: "VideoId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionThread_CourseCategoryMaster_ProgramId",
                table: "DiscussionThread",
                column: "ProgramId",
                principalTable: "CourseCategoryMaster",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscussionThread_CourseCategoryMaster_ProgramId",
                table: "DiscussionThread");

            migrationBuilder.DropTable(
                name: "DiscussionAttachment");

            migrationBuilder.DropTable(
                name: "LectureQuizAttempt");

            migrationBuilder.DropTable(
                name: "VideoProgress");

            migrationBuilder.DropTable(
                name: "VideoQuizAttempt");

            migrationBuilder.DropTable(
                name: "DiscussionReply");

            migrationBuilder.DropTable(
                name: "LectureQuizQuestion");

            migrationBuilder.DropTable(
                name: "VideoQuizQuestion");

            migrationBuilder.DropTable(
                name: "VideoMaster");

            migrationBuilder.DropTable(
                name: "TopicMaster");

            migrationBuilder.DropIndex(
                name: "IX_DiscussionThread_ProgramId",
                table: "DiscussionThread");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "DiscussionThread");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "DiscussionThread");

            migrationBuilder.DropColumn(
                name: "ProgramId",
                table: "DiscussionThread");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "DiscussionThread");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "DiscussionThread",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200)
                .Annotation("Relational:ColumnOrder", 4)
                .OldAnnotation("Relational:ColumnOrder", 5);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedOn",
                table: "DiscussionThread",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2")
                .Annotation("Relational:ColumnOrder", 6)
                .OldAnnotation("Relational:ColumnOrder", 7);

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByUserId",
                table: "DiscussionThread",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 6);

            migrationBuilder.AlterColumn<int>(
                name: "CourseQuadrantId",
                table: "DiscussionThread",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "DiscussionComment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThreadId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CommentText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommentedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscussionComment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscussionComment_DiscussionThread_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "DiscussionThread",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscussionComment_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscussionComment_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscussionComment_UserMaster_UserId",
                        column: x => x.UserId,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionComment_CreatedBy",
                table: "DiscussionComment",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionComment_ThreadId",
                table: "DiscussionComment",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionComment_UpdatedBy",
                table: "DiscussionComment",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionComment_UserId",
                table: "DiscussionComment",
                column: "UserId");
        }
    }
}
