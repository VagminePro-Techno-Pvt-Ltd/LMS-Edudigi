using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddLectureTranscript : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LectureTranscript",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LectureMaterialId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DurationSeconds = table.Column<double>(type: "float", nullable: false),
                    FullText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    LastProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SegmentsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LectureTranscript", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LectureTranscript_LectureMaterial_LectureMaterialId",
                        column: x => x.LectureMaterialId,
                        principalTable: "LectureMaterial",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LectureTranscript_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LectureTranscript_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LectureTranscriptSegment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LectureTranscriptId = table.Column<int>(type: "int", nullable: false),
                    StartTimeSeconds = table.Column<double>(type: "float", nullable: false),
                    EndTimeSeconds = table.Column<double>(type: "float", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SequenceOrder = table.Column<int>(type: "int", nullable: false),
                    Speaker = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LectureTranscriptSegment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LectureTranscriptSegment_LectureTranscript_LectureTranscriptId",
                        column: x => x.LectureTranscriptId,
                        principalTable: "LectureTranscript",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LectureTranscriptSegment_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LectureTranscriptSegment_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LectureTranscript_CreatedBy",
                table: "LectureTranscript",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LectureTranscript_LectureMaterialId",
                table: "LectureTranscript",
                column: "LectureMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_LectureTranscript_UpdatedBy",
                table: "LectureTranscript",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LectureTranscriptSegment_CreatedBy",
                table: "LectureTranscriptSegment",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LectureTranscriptSegment_LectureTranscriptId",
                table: "LectureTranscriptSegment",
                column: "LectureTranscriptId");

            migrationBuilder.CreateIndex(
                name: "IX_LectureTranscriptSegment_UpdatedBy",
                table: "LectureTranscriptSegment",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LectureTranscriptSegment");

            migrationBuilder.DropTable(
                name: "LectureTranscript");
        }
    }
}
