using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class AddCourseMaterialMapping : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseMaterialMapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LectureMaterialId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    CourseQuadrantId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    AssignedBy = table.Column<int>(type: "int", nullable: false),
                    AssignedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseMaterialMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseMaterialMapping_CourseMaster_CourseId",
                        column: x => x.CourseId,
                        principalTable: "CourseMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseMaterialMapping_CourseQuadrantMaster_CourseQuadrantId",
                        column: x => x.CourseQuadrantId,
                        principalTable: "CourseQuadrantMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseMaterialMapping_LectureMaterial_LectureMaterialId",
                        column: x => x.LectureMaterialId,
                        principalTable: "LectureMaterial",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseMaterialMapping_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseMaterialMapping_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterialMapping_CourseId",
                table: "CourseMaterialMapping",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterialMapping_CourseQuadrantId",
                table: "CourseMaterialMapping",
                column: "CourseQuadrantId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterialMapping_CreatedBy",
                table: "CourseMaterialMapping",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterialMapping_LectureMaterialId",
                table: "CourseMaterialMapping",
                column: "LectureMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterialMapping_UpdatedBy",
                table: "CourseMaterialMapping",
                column: "UpdatedBy");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseMaterialMapping");
        }
    }
}
