using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TMS.Repository;

namespace TMS.Repository.Migrations;

[DbContext(typeof(ApplicationDBContext))]
[Migration("20261009130000_AddInternalAssessment")]
public partial class AddInternalAssessment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "InternalAssessments", columns: table => new
        {
            Id = table.Column<int>(nullable:false).Annotation("SqlServer:Identity", "1, 1"),
            Title = table.Column<string>(maxLength:200, nullable:false),
            Instructions = table.Column<string>(maxLength:4000, nullable:true),
            Type = table.Column<string>(maxLength:30, nullable:false),
            CourseId = table.Column<int>(nullable:false),
            SubjectId = table.Column<int>(nullable:true),
            DueDate = table.Column<DateTime>(nullable:false),
            MaxMarks = table.Column<decimal>(type:"decimal(10,2)", nullable:false),
            AllowLateSubmission = table.Column<bool>(nullable:false),
            IsPublished = table.Column<bool>(nullable:false),
            ResultsPublished = table.Column<bool>(nullable:false),
            CreatedBy = table.Column<int>(nullable:false),
            CreatedOn = table.Column<DateTime>(nullable:false),
            IsDeleted = table.Column<bool>(nullable:false)
        }, constraints: table => table.PrimaryKey("PK_InternalAssessments", x=>x.Id));
        migrationBuilder.CreateIndex("IX_InternalAssessments_CourseId", "InternalAssessments", "CourseId");
        migrationBuilder.CreateTable(name: "InternalAssessmentSubmissions", columns: table => new
        {
            Id = table.Column<int>(nullable:false).Annotation("SqlServer:Identity", "1, 1"),
            AssessmentId = table.Column<int>(nullable:false),
            StudentId = table.Column<int>(nullable:false),
            AnswerText = table.Column<string>(maxLength:10000, nullable:true),
            FilePath = table.Column<string>(maxLength:500, nullable:true),
            SubmittedOn = table.Column<DateTime>(nullable:false),
            IsLate = table.Column<bool>(nullable:false),
            Marks = table.Column<decimal>(type:"decimal(10,2)", nullable:true),
            Feedback = table.Column<string>(maxLength:2000, nullable:true),
            EvaluatedBy = table.Column<int>(nullable:true),
            EvaluatedOn = table.Column<DateTime>(nullable:true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_InternalAssessmentSubmissions", x=>x.Id);
            table.ForeignKey("FK_InternalAssessmentSubmissions_InternalAssessments_AssessmentId", x=>x.AssessmentId, "InternalAssessments", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("IX_InternalAssessmentSubmissions_AssessmentId_StudentId", "InternalAssessmentSubmissions", new[]{"AssessmentId","StudentId"}, unique:true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("InternalAssessmentSubmissions");
        migrationBuilder.DropTable("InternalAssessments");
    }
}
