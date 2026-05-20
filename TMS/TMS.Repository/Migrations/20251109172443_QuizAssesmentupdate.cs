using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class QuizAssesmentupdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FacultyQuizQuestionAssessment_FacultyQuizAssessment_FacultyQuizAssessmentId",
                table: "FacultyQuizQuestionAssessment");

            migrationBuilder.AlterColumn<decimal>(
                name: "StudentScore",
                table: "FacultyQuizQuestionAssessment",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)")
                .Annotation("Relational:ColumnOrder", 7)
                .OldAnnotation("Relational:ColumnOrder", 6);

            migrationBuilder.AlterColumn<string>(
                name: "QuestionText",
                table: "FacultyQuizQuestionAssessment",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)")
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AlterColumn<int>(
                name: "QuestionId",
                table: "FacultyQuizQuestionAssessment",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 4)
                .OldAnnotation("Relational:ColumnOrder", 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxScore",
                table: "FacultyQuizQuestionAssessment",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)")
                .Annotation("Relational:ColumnOrder", 6)
                .OldAnnotation("Relational:ColumnOrder", 5);

            migrationBuilder.AlterColumn<int>(
                name: "FacultyQuizAssessmentId",
                table: "FacultyQuizQuestionAssessment",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 3);

            migrationBuilder.AddColumn<int>(
                name: "QuizId",
                table: "FacultyQuizAssessment",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("Relational:ColumnOrder", 13);

            migrationBuilder.AddForeignKey(
                name: "FK_FacultyQuizQuestionAssessment_FacultyQuizAssessment_FacultyQuizAssessmentId",
                table: "FacultyQuizQuestionAssessment",
                column: "FacultyQuizAssessmentId",
                principalTable: "FacultyQuizAssessment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FacultyQuizQuestionAssessment_FacultyQuizAssessment_FacultyQuizAssessmentId",
                table: "FacultyQuizQuestionAssessment");

            migrationBuilder.DropColumn(
                name: "QuizId",
                table: "FacultyQuizAssessment");

            migrationBuilder.AlterColumn<decimal>(
                name: "StudentScore",
                table: "FacultyQuizQuestionAssessment",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)")
                .Annotation("Relational:ColumnOrder", 6)
                .OldAnnotation("Relational:ColumnOrder", 7);

            migrationBuilder.AlterColumn<string>(
                name: "QuestionText",
                table: "FacultyQuizQuestionAssessment",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)")
                .Annotation("Relational:ColumnOrder", 4)
                .OldAnnotation("Relational:ColumnOrder", 5);

            migrationBuilder.AlterColumn<int>(
                name: "QuestionId",
                table: "FacultyQuizQuestionAssessment",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 3)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxScore",
                table: "FacultyQuizQuestionAssessment",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)")
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 6);

            migrationBuilder.AlterColumn<int>(
                name: "FacultyQuizAssessmentId",
                table: "FacultyQuizQuestionAssessment",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("Relational:ColumnOrder", 3);

            migrationBuilder.AddForeignKey(
                name: "FK_FacultyQuizQuestionAssessment_FacultyQuizAssessment_FacultyQuizAssessmentId",
                table: "FacultyQuizQuestionAssessment",
                column: "FacultyQuizAssessmentId",
                principalTable: "FacultyQuizAssessment",
                principalColumn: "Id");
        }
    }
}
