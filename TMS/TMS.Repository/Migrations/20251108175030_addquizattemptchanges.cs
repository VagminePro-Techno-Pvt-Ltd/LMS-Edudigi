using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class addquizattemptchanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentQuizAnswers_QuizOption_SelectedOptionId",
                table: "StudentQuizAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentQuizAnswers_QuizQuestion_QuizQuestionId",
                table: "StudentQuizAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentQuizAnswers_StudentQuizResponses_StudentQuizResponseId",
                table: "StudentQuizAnswers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StudentQuizAnswers",
                table: "StudentQuizAnswers");

            migrationBuilder.RenameTable(
                name: "StudentQuizAnswers",
                newName: "StudentQuizAnswer");

            migrationBuilder.RenameIndex(
                name: "IX_StudentQuizAnswers_StudentQuizResponseId",
                table: "StudentQuizAnswer",
                newName: "IX_StudentQuizAnswer_StudentQuizResponseId");

            migrationBuilder.RenameIndex(
                name: "IX_StudentQuizAnswers_SelectedOptionId",
                table: "StudentQuizAnswer",
                newName: "IX_StudentQuizAnswer_SelectedOptionId");

            migrationBuilder.RenameIndex(
                name: "IX_StudentQuizAnswers_QuizQuestionId",
                table: "StudentQuizAnswer",
                newName: "IX_StudentQuizAnswer_QuizQuestionId");

            migrationBuilder.AlterColumn<string>(
                name: "WrittenAnswer",
                table: "StudentQuizAnswer",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 6)
                .OldAnnotation("Relational:ColumnOrder", 7);

            migrationBuilder.AlterColumn<int>(
                name: "StudentQuizResponseId",
                table: "StudentQuizAnswer",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 3)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AlterColumn<int>(
                name: "SelectedOptionId",
                table: "StudentQuizAnswer",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 6);

            migrationBuilder.AlterColumn<int>(
                name: "QuizQuestionId",
                table: "StudentQuizAnswer",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 4)
                .OldAnnotation("Relational:ColumnOrder", 5);

            migrationBuilder.AlterColumn<bool>(
                name: "IsCorrect",
                table: "StudentQuizAnswer",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 7)
                .OldAnnotation("Relational:ColumnOrder", 8);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "StudentQuizAnswer",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 0)
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("Relational:ColumnOrder", 3)
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "StudentQuizAnswer",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("Relational:ColumnOrder", 10002);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "StudentQuizAnswer",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified))
                .Annotation("Relational:ColumnOrder", 10003);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "StudentQuizAnswer",
                type: "bit",
                nullable: false,
                defaultValue: false)
                .Annotation("Relational:ColumnOrder", 10001);

            migrationBuilder.AddColumn<int>(
                name: "StudentQuizAttemptId",
                table: "StudentQuizAnswer",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "StudentQuizAnswer",
                type: "int",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 10004);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "StudentQuizAnswer",
                type: "datetime2",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 10005);

            migrationBuilder.AddPrimaryKey(
                name: "PK_StudentQuizAnswer",
                table: "StudentQuizAnswer",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "StudentQuizAttempt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    QuizId = table.Column<int>(type: "int", nullable: false),
                    AttemptDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentQuizAttempt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentQuizAttempt_QuizMaster_QuizId",
                        column: x => x.QuizId,
                        principalTable: "QuizMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentQuizAttempt_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentQuizAttempt_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuizAnswer_CreatedBy",
                table: "StudentQuizAnswer",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuizAnswer_StudentQuizAttemptId",
                table: "StudentQuizAnswer",
                column: "StudentQuizAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuizAnswer_UpdatedBy",
                table: "StudentQuizAnswer",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuizAttempt_CreatedBy",
                table: "StudentQuizAttempt",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuizAttempt_QuizId",
                table: "StudentQuizAttempt",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuizAttempt_UpdatedBy",
                table: "StudentQuizAttempt",
                column: "UpdatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentQuizAnswer_QuizOption_SelectedOptionId",
                table: "StudentQuizAnswer",
                column: "SelectedOptionId",
                principalTable: "QuizOption",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentQuizAnswer_QuizQuestion_QuizQuestionId",
                table: "StudentQuizAnswer",
                column: "QuizQuestionId",
                principalTable: "QuizQuestion",
                principalColumn: "Id");

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

            migrationBuilder.AddForeignKey(
                name: "FK_StudentQuizAnswer_UserMaster_CreatedBy",
                table: "StudentQuizAnswer",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentQuizAnswer_UserMaster_UpdatedBy",
                table: "StudentQuizAnswer",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentQuizAnswer_QuizOption_SelectedOptionId",
                table: "StudentQuizAnswer");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentQuizAnswer_QuizQuestion_QuizQuestionId",
                table: "StudentQuizAnswer");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentQuizAnswer_StudentQuizAttempt_StudentQuizAttemptId",
                table: "StudentQuizAnswer");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentQuizAnswer_StudentQuizResponses_StudentQuizResponseId",
                table: "StudentQuizAnswer");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentQuizAnswer_UserMaster_CreatedBy",
                table: "StudentQuizAnswer");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentQuizAnswer_UserMaster_UpdatedBy",
                table: "StudentQuizAnswer");

            migrationBuilder.DropTable(
                name: "StudentQuizAttempt");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StudentQuizAnswer",
                table: "StudentQuizAnswer");

            migrationBuilder.DropIndex(
                name: "IX_StudentQuizAnswer_CreatedBy",
                table: "StudentQuizAnswer");

            migrationBuilder.DropIndex(
                name: "IX_StudentQuizAnswer_StudentQuizAttemptId",
                table: "StudentQuizAnswer");

            migrationBuilder.DropIndex(
                name: "IX_StudentQuizAnswer_UpdatedBy",
                table: "StudentQuizAnswer");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "StudentQuizAnswer");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "StudentQuizAnswer");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "StudentQuizAnswer");

            migrationBuilder.DropColumn(
                name: "StudentQuizAttemptId",
                table: "StudentQuizAnswer");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "StudentQuizAnswer");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "StudentQuizAnswer");

            migrationBuilder.RenameTable(
                name: "StudentQuizAnswer",
                newName: "StudentQuizAnswers");

            migrationBuilder.RenameIndex(
                name: "IX_StudentQuizAnswer_StudentQuizResponseId",
                table: "StudentQuizAnswers",
                newName: "IX_StudentQuizAnswers_StudentQuizResponseId");

            migrationBuilder.RenameIndex(
                name: "IX_StudentQuizAnswer_SelectedOptionId",
                table: "StudentQuizAnswers",
                newName: "IX_StudentQuizAnswers_SelectedOptionId");

            migrationBuilder.RenameIndex(
                name: "IX_StudentQuizAnswer_QuizQuestionId",
                table: "StudentQuizAnswers",
                newName: "IX_StudentQuizAnswers_QuizQuestionId");

            migrationBuilder.AlterColumn<string>(
                name: "WrittenAnswer",
                table: "StudentQuizAnswers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 7)
                .OldAnnotation("Relational:ColumnOrder", 6);

            migrationBuilder.AlterColumn<int>(
                name: "StudentQuizResponseId",
                table: "StudentQuizAnswers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 4)
                .OldAnnotation("Relational:ColumnOrder", 3);

            migrationBuilder.AlterColumn<int>(
                name: "SelectedOptionId",
                table: "StudentQuizAnswers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 6)
                .OldAnnotation("Relational:ColumnOrder", 5);

            migrationBuilder.AlterColumn<int>(
                name: "QuizQuestionId",
                table: "StudentQuizAnswers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AlterColumn<bool>(
                name: "IsCorrect",
                table: "StudentQuizAnswers",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 8)
                .OldAnnotation("Relational:ColumnOrder", 7);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "StudentQuizAnswers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 3)
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("Relational:ColumnOrder", 0)
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StudentQuizAnswers",
                table: "StudentQuizAnswers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentQuizAnswers_QuizOption_SelectedOptionId",
                table: "StudentQuizAnswers",
                column: "SelectedOptionId",
                principalTable: "QuizOption",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentQuizAnswers_QuizQuestion_QuizQuestionId",
                table: "StudentQuizAnswers",
                column: "QuizQuestionId",
                principalTable: "QuizQuestion",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentQuizAnswers_StudentQuizResponses_StudentQuizResponseId",
                table: "StudentQuizAnswers",
                column: "StudentQuizResponseId",
                principalTable: "StudentQuizResponses",
                principalColumn: "Id");
        }
    }
}
