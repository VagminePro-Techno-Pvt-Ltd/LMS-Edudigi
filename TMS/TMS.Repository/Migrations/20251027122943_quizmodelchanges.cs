using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class quizmodelchanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizOption_QuizQuestion_QuizQuestionId",
                table: "QuizOption");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizQuestion_QuizMaster_QuizId",
                table: "QuizQuestion");

            migrationBuilder.AlterColumn<int>(
                name: "CorrectOptionId",
                table: "QuizQuestion",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 6)
                .OldAnnotation("Relational:ColumnOrder", 5);

            migrationBuilder.AddColumn<int>(
                name: "QuestionType",
                table: "QuizQuestion",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("Relational:ColumnOrder", 5);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "QuizMaster",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100)
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AlterColumn<int>(
                name: "CourseQuadrantId",
                table: "QuizMaster",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 4)
                .OldAnnotation("Relational:ColumnOrder", 3);

            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "QuizMaster",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("Relational:ColumnOrder", 3);

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestion_CorrectOptionId",
                table: "QuizQuestion",
                column: "CorrectOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizMaster_CourseId",
                table: "QuizMaster",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizMaster_CourseMaster_CourseId",
                table: "QuizMaster",
                column: "CourseId",
                principalTable: "CourseMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizOption_QuizQuestion_QuizQuestionId",
                table: "QuizOption",
                column: "QuizQuestionId",
                principalTable: "QuizQuestion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizQuestion_QuizMaster_QuizId",
                table: "QuizQuestion",
                column: "QuizId",
                principalTable: "QuizMaster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizQuestion_QuizOption_CorrectOptionId",
                table: "QuizQuestion",
                column: "CorrectOptionId",
                principalTable: "QuizOption",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizMaster_CourseMaster_CourseId",
                table: "QuizMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizOption_QuizQuestion_QuizQuestionId",
                table: "QuizOption");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizQuestion_QuizMaster_QuizId",
                table: "QuizQuestion");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizQuestion_QuizOption_CorrectOptionId",
                table: "QuizQuestion");

            migrationBuilder.DropIndex(
                name: "IX_QuizQuestion_CorrectOptionId",
                table: "QuizQuestion");

            migrationBuilder.DropIndex(
                name: "IX_QuizMaster_CourseId",
                table: "QuizMaster");

            migrationBuilder.DropColumn(
                name: "QuestionType",
                table: "QuizQuestion");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "QuizMaster");

            migrationBuilder.AlterColumn<int>(
                name: "CorrectOptionId",
                table: "QuizQuestion",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 6);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "QuizMaster",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100)
                .Annotation("Relational:ColumnOrder", 4)
                .OldAnnotation("Relational:ColumnOrder", 5);

            migrationBuilder.AlterColumn<int>(
                name: "CourseQuadrantId",
                table: "QuizMaster",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 3)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizOption_QuizQuestion_QuizQuestionId",
                table: "QuizOption",
                column: "QuizQuestionId",
                principalTable: "QuizQuestion",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizQuestion_QuizMaster_QuizId",
                table: "QuizQuestion",
                column: "QuizId",
                principalTable: "QuizMaster",
                principalColumn: "Id");
        }
    }
}
