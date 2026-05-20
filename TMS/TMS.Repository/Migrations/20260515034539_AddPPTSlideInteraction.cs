using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class AddPPTSlideInteraction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttemptedOn",
                table: "LectureQuizAttempt");

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    TopicId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    TopicName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.TopicId);
                });

            migrationBuilder.CreateTable(
                name: "InteractivePPT",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    TopicId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UploadedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InteractivePPT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InteractivePPT_CourseMaster_CourseId",
                        column: x => x.CourseId,
                        principalTable: "CourseMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InteractivePPT_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "TopicId");
                    table.ForeignKey(
                        name: "FK_InteractivePPT_UnitMaster_UnitId",
                        column: x => x.UnitId,
                        principalTable: "UnitMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PPTSlideInteraction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PPTId = table.Column<int>(type: "int", nullable: false),
                    SlideNumber = table.Column<int>(type: "int", nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    ButtonText = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PPTSlideInteraction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PPTSlideInteraction_InteractivePPT_PPTId",
                        column: x => x.PPTId,
                        principalTable: "InteractivePPT",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InteractivePPT_CourseId",
                table: "InteractivePPT",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_InteractivePPT_TopicId",
                table: "InteractivePPT",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_InteractivePPT_UnitId",
                table: "InteractivePPT",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PPTSlideInteraction_PPTId",
                table: "PPTSlideInteraction",
                column: "PPTId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PPTSlideInteraction");

            migrationBuilder.DropTable(
                name: "InteractivePPT");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.AddColumn<DateTime>(
                name: "AttemptedOn",
                table: "LectureQuizAttempt",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified))
                .Annotation("Relational:ColumnOrder", 7);
        }
    }
}
