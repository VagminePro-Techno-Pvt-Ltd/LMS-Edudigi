using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BadgeMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CourseCategoryId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    NoOfTrainingApplicable = table.Column<int>(type: "int", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CertificateMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Initial = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Fax = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address3 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address4 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseCategoryMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseCategoryMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseEmploymentTypeMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    EmploymentTypeId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseEmploymentTypeMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseEnrollment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EnrolledOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseEnrollment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CourseCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CourseCategoryId = table.Column<int>(type: "int", nullable: false),
                    CourseDurationMonths = table.Column<int>(type: "int", nullable: false),
                    SemesterId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseMaster_CourseCategoryMaster_CourseCategoryId",
                        column: x => x.CourseCategoryId,
                        principalTable: "CourseCategoryMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourseQuadrantMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    QuadrantNumber = table.Column<int>(type: "int", nullable: false),
                    QuadrantTitle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseQuadrantMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseQuadrantMaster_CourseMaster_CourseId",
                        column: x => x.CourseId,
                        principalTable: "CourseMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DepartmentMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    DepartmentCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DesignationMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DivisionId = table.Column<int>(type: "int", nullable: false),
                    DesignationCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesignationMaster", x => x.Id);
                });

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
                });

            migrationBuilder.CreateTable(
                name: "DiscussionThread",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseQuadrantId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscussionThread", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscussionThread_CourseQuadrantMaster_CourseQuadrantId",
                        column: x => x.CourseQuadrantId,
                        principalTable: "CourseQuadrantMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DivisionMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    DivisionCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DivisionMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DivisionMaster_DepartmentMaster_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DepartmentMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmploymentTypeMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentTypeMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormControlMaster",
                columns: table => new
                {
                    FormId = table.Column<int>(type: "int", nullable: false),
                    ControlId = table.Column<int>(type: "int", nullable: false),
                    ControlName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormControlMaster", x => new { x.FormId, x.ControlId });
                });

            migrationBuilder.CreateTable(
                name: "FormControlRoleMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    FormId = table.Column<int>(type: "int", nullable: false),
                    ControlId = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormControlRoleMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Area = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Controller = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IconClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    MenuId = table.Column<int>(type: "int", nullable: true),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    ExcludeFromMenu = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LanguageMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LanguageCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LanguageMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LectureMaterial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseQuadrantId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LectureMaterial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LectureMaterial_CourseQuadrantMaster_CourseQuadrantId",
                        column: x => x.CourseQuadrantId,
                        principalTable: "CourseQuadrantMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LocationMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LocationCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IconClass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NominationRequest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainingId = table.Column<int>(type: "int", nullable: false),
                    CandidateId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NominationRequest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NominationRequestRemarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NominationRequestId = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NominationRequestStatus = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NominationRequestRemarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NominationRequestRemarks_NominationRequest_NominationRequestId",
                        column: x => x.NominationRequestId,
                        principalTable: "NominationRequest",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PhotoGalleryMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoGalleryMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhotoGalleryMaster_CourseMaster_CourseId",
                        column: x => x.CourseId,
                        principalTable: "CourseMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QueryMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueryMaster_CourseMaster_CourseId",
                        column: x => x.CourseId,
                        principalTable: "CourseMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuestionMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Question = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuestionType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuestionOptionMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Option = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCorrectAnswer = table.Column<bool>(type: "bit", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionOptionMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionOptionMaster_QuestionMaster_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QuestionMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuizMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseQuadrantId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizMaster_CourseQuadrantMaster_CourseQuadrantId",
                        column: x => x.CourseQuadrantId,
                        principalTable: "CourseQuadrantMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuizOption",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuizQuestionId = table.Column<int>(type: "int", nullable: false),
                    OptionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizOption", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuizQuestion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuizId = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrectOptionId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizQuestion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizQuestion_QuizMaster_QuizId",
                        column: x => x.QuizId,
                        principalTable: "QuizMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RankingMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RankingMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserTypeId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleMaster_UserType_UserTypeId",
                        column: x => x.UserTypeId,
                        principalTable: "UserType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    FormId = table.Column<int>(type: "int", nullable: false),
                    View = table.Column<bool>(type: "bit", nullable: false),
                    Add = table.Column<bool>(type: "bit", nullable: false),
                    Edit = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.FormId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_FormMaster_FormId",
                        column: x => x.FormId,
                        principalTable: "FormMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RolePermissions_RoleMaster_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RoleMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    EmploymentTypeId = table.Column<int>(type: "int", nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    DivisionId = table.Column<int>(type: "int", nullable: true),
                    DesignationId = table.Column<int>(type: "int", nullable: true),
                    SupervisorId = table.Column<int>(type: "int", nullable: true),
                    ManagerId = table.Column<int>(type: "int", nullable: true),
                    ContactNo = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMaster_DepartmentMaster_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DepartmentMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserMaster_DesignationMaster_DesignationId",
                        column: x => x.DesignationId,
                        principalTable: "DesignationMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserMaster_DivisionMaster_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "DivisionMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserMaster_EmploymentTypeMaster_EmploymentTypeId",
                        column: x => x.EmploymentTypeId,
                        principalTable: "EmploymentTypeMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserMaster_LocationMaster_LocationId",
                        column: x => x.LocationId,
                        principalTable: "LocationMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserMaster_RoleMaster_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RoleMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserMaster_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserMaster_UserMaster_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserMaster_UserMaster_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserMaster_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SemesterMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SequenceNo = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemesterMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SemesterMaster_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SemesterMaster_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SignatureMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignatureMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SignatureMaster_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SignatureMaster_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TrainingMatrix",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    DivisionId = table.Column<int>(type: "int", nullable: false),
                    DesignationId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    CoursePriority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingMatrix", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingMatrix_CourseMaster_CourseId",
                        column: x => x.CourseId,
                        principalTable: "CourseMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingMatrix_DepartmentMaster_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DepartmentMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingMatrix_DesignationMaster_DesignationId",
                        column: x => x.DesignationId,
                        principalTable: "DesignationMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingMatrix_DivisionMaster_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "DivisionMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingMatrix_LocationMaster_LocationId",
                        column: x => x.LocationId,
                        principalTable: "LocationMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingMatrix_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingMatrix_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TrainingScheduleStatusMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TextColorCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    BackgroundColorCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingScheduleStatusMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingScheduleStatusMaster_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingScheduleStatusMaster_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_RoleMaster_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RoleMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserRoles_UserMaster_UserId",
                        column: x => x.UserId,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VenueMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VenueId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    VenueSeat = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VenueMaster_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VenueMaster_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TrainingSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    IsAsPerMatrix = table.Column<bool>(type: "bit", nullable: false),
                    TrainingTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ConductedByDivisionId = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateTime>(type: "Date", nullable: false),
                    ToDate = table.Column<DateTime>(type: "Date", nullable: false),
                    FromTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    ToTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    VenueId = table.Column<int>(type: "int", nullable: false),
                    MinNumberOfSeats = table.Column<int>(type: "int", nullable: false),
                    MaxNumberOfSeats = table.Column<int>(type: "int", nullable: false),
                    InstructorId = table.Column<int>(type: "int", nullable: false),
                    IsQuizMandatory = table.Column<bool>(type: "bit", nullable: false),
                    IsFeedBackMandatory = table.Column<bool>(type: "bit", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    IsReScheduled = table.Column<bool>(type: "bit", nullable: false),
                    MeetingLink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingSchedules_CourseMaster_CourseId",
                        column: x => x.CourseId,
                        principalTable: "CourseMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingSchedules_DivisionMaster_ConductedByDivisionId",
                        column: x => x.ConductedByDivisionId,
                        principalTable: "DivisionMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingSchedules_TrainingScheduleStatusMaster_StatusId",
                        column: x => x.StatusId,
                        principalTable: "TrainingScheduleStatusMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingSchedules_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingSchedules_UserMaster_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingSchedules_UserMaster_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingSchedules_VenueMaster_VenueId",
                        column: x => x.VenueId,
                        principalTable: "VenueMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TrainingScheduleApprovalHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainingScheduleId = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    OldFromDate = table.Column<DateTime>(type: "Date", nullable: true),
                    OldToDate = table.Column<DateTime>(type: "Date", nullable: true),
                    OldFromTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    OldToTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    NewFromDate = table.Column<DateTime>(type: "Date", nullable: true),
                    NewToDate = table.Column<DateTime>(type: "Date", nullable: true),
                    NewFromTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    NewToTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingScheduleApprovalHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingScheduleApprovalHistory_TrainingSchedules_TrainingScheduleId",
                        column: x => x.TrainingScheduleId,
                        principalTable: "TrainingSchedules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingScheduleApprovalHistory_UserMaster_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingScheduleApprovalHistory_UserMaster_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "UserMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TrainingScheduleAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraningScheduleId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingScheduleAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingScheduleAttachments_TrainingSchedules_TraningScheduleId",
                        column: x => x.TraningScheduleId,
                        principalTable: "TrainingSchedules",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TrainingScheduleLanguage",
                columns: table => new
                {
                    TraningScheduleId = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingScheduleLanguage", x => new { x.TraningScheduleId, x.LanguageId });
                    table.ForeignKey(
                        name: "FK_TrainingScheduleLanguage_LanguageMaster_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "LanguageMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingScheduleLanguage_TrainingSchedules_TraningScheduleId",
                        column: x => x.TraningScheduleId,
                        principalTable: "TrainingSchedules",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "UserMaster",
                columns: new[] { "Id", "ContactNo", "CreatedBy", "CreatedOn", "DepartmentId", "DesignationId", "DivisionId", "Email", "EmploymentTypeId", "IsActive", "LocationId", "ManagerId", "Name", "Password", "RoleId", "SupervisorId", "UpdatedBy", "UpdatedOn" },
                values: new object[] { 1, null, 1, new DateTime(2022, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, "admin@TMS.com", null, true, null, null, "Super Admin User", "056QB10sjntdtPm4R3l6Hw==", null, null, null, null });

            migrationBuilder.InsertData(
                table: "UserType",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Super Admin" },
                    { 2, "Admin" },
                    { 3, "User" },
                    { 4, "Instructor" },
                    { 5, "Co-Ordinator" },
                    { 6, "Sr.Engineer" }
                });

            migrationBuilder.InsertData(
                table: "LanguageMaster",
                columns: new[] { "Id", "CountryCode", "CreatedBy", "CreatedOn", "Description", "IsActive", "LanguageCode", "Name", "UpdatedBy", "UpdatedOn" },
                values: new object[,]
                {
                    { 1, "IND", 1, new DateTime(2022, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, "en-IN", "English", null, null },
                    { 2, "UAE", 1, new DateTime(2022, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, "ar-AE", "Arabic", null, null }
                });

            migrationBuilder.InsertData(
                table: "MenuMaster",
                columns: new[] { "Id", "CreatedBy", "CreatedOn", "Description", "IconClass", "IsActive", "IsAdmin", "Name", "Sequence", "UpdatedBy", "UpdatedOn" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2022, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "fab fa-cuttlefish", true, true, "Common", 2, null, null },
                    { 2, 1, new DateTime(2022, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "fas fa-universal-access", true, true, "Access Master", 3, null, null },
                    { 3, 1, new DateTime(2022, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "fas fa-users-cog", true, true, "User Master", 4, null, null }
                });

            migrationBuilder.InsertData(
                table: "RoleMaster",
                columns: new[] { "Id", "CreatedBy", "CreatedOn", "Description", "IsActive", "Name", "UpdatedBy", "UpdatedOn", "UserTypeId" },
                values: new object[] { 1, 1, new DateTime(2022, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, "Super Admin", null, null, 1 });

            migrationBuilder.InsertData(
                table: "TrainingScheduleStatusMaster",
                columns: new[] { "Id", "BackgroundColorCode", "CreatedBy", "CreatedOn", "Description", "IsActive", "Name", "TextColorCode", "UpdatedBy", "UpdatedOn" },
                values: new object[,]
                {
                    { 1, null, 1, new DateTime(2022, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, "Planned", null, null, null },
                    { 2, null, 1, new DateTime(2022, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, "Scheduled", null, null, null },
                    { 3, null, 1, new DateTime(2022, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, "Rejected", null, null, null },
                    { 4, null, 1, new DateTime(2022, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, "Completed", null, null, null },
                    { 5, null, 1, new DateTime(2022, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, "Pending For Approval", null, null, null },
                    { 6, null, 1, new DateTime(2022, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, "Approved", null, null, null },
                    { 7, null, 1, new DateTime(2022, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, "Cancelled", null, null, null },
                    { 8, null, 1, new DateTime(2022, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, "ReScheduled", null, null, null }
                });

            migrationBuilder.InsertData(
                table: "FormMaster",
                columns: new[] { "Id", "Action", "Area", "Controller", "Description", "ExcludeFromMenu", "IconClass", "IsAdmin", "MenuId", "Name", "Sequence" },
                values: new object[,]
                {
                    { 1, "Index", "Admin", "Roles", null, false, "fas fa-user-tag", true, 3, "Role", 1 },
                    { 2, "Index", "Admin", "RolePermissions", null, false, "fas fa-universal-access", true, 3, "Role Permissions", 3 },
                    { 3, "Index", "Admin", "RoleControlPermission", null, false, "fas fa-comments-dollar", true, 3, "Role Control Permission", 4 },
                    { 4, "Index", "Admin", "User", null, false, "fas fa-user-tie", true, 3, "User", 5 },
                    { 5, "Index", "Admin", "CourseCategory", null, false, "fas fa-paw", true, 1, "Course Category", 8 },
                    { 6, "Index", "Admin", "Course", null, false, "fas fa-discourse", true, 1, "Course", 9 }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { 1, 1 });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "FormId", "RoleId", "Add", "Edit", "View" },
                values: new object[,]
                {
                    { 1, 1, true, true, true },
                    { 2, 1, true, true, true },
                    { 3, 1, true, true, true },
                    { 4, 1, true, true, true },
                    { 5, 1, true, true, true },
                    { 6, 1, true, true, true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BadgeMaster_CourseCategoryId",
                table: "BadgeMaster",
                column: "CourseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeMaster_CreatedBy",
                table: "BadgeMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeMaster_DepartmentId",
                table: "BadgeMaster",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeMaster_UpdatedBy",
                table: "BadgeMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateMaster_CreatedBy",
                table: "CertificateMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateMaster_UpdatedBy",
                table: "CertificateMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyMaster_CreatedBy",
                table: "CompanyMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyMaster_UpdatedBy",
                table: "CompanyMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CourseCategoryMaster_CreatedBy",
                table: "CourseCategoryMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CourseCategoryMaster_UpdatedBy",
                table: "CourseCategoryMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEmploymentTypeMappings_CourseId",
                table: "CourseEmploymentTypeMappings",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEmploymentTypeMappings_CreatedBy",
                table: "CourseEmploymentTypeMappings",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEmploymentTypeMappings_EmploymentTypeId",
                table: "CourseEmploymentTypeMappings",
                column: "EmploymentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEmploymentTypeMappings_UpdatedBy",
                table: "CourseEmploymentTypeMappings",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollment_CourseId",
                table: "CourseEnrollment",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollment_CreatedBy",
                table: "CourseEnrollment",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollment_UpdatedBy",
                table: "CourseEnrollment",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollment_UserId",
                table: "CourseEnrollment",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaster_CourseCategoryId",
                table: "CourseMaster",
                column: "CourseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaster_CreatedBy",
                table: "CourseMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaster_SemesterId",
                table: "CourseMaster",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaster_UpdatedBy",
                table: "CourseMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CourseQuadrantMaster_CourseId",
                table: "CourseQuadrantMaster",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseQuadrantMaster_CreatedBy",
                table: "CourseQuadrantMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CourseQuadrantMaster_UpdatedBy",
                table: "CourseQuadrantMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentMaster_CreatedBy",
                table: "DepartmentMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentMaster_LocationId",
                table: "DepartmentMaster",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentMaster_UpdatedBy",
                table: "DepartmentMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DesignationMaster_CreatedBy",
                table: "DesignationMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DesignationMaster_DivisionId",
                table: "DesignationMaster",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignationMaster_UpdatedBy",
                table: "DesignationMaster",
                column: "UpdatedBy");

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

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionThread_CourseQuadrantId",
                table: "DiscussionThread",
                column: "CourseQuadrantId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionThread_CreatedByUserId",
                table: "DiscussionThread",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionThread_UpdatedBy",
                table: "DiscussionThread",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DivisionMaster_CreatedBy",
                table: "DivisionMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DivisionMaster_DepartmentId",
                table: "DivisionMaster",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DivisionMaster_UpdatedBy",
                table: "DivisionMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentTypeMaster_CreatedBy",
                table: "EmploymentTypeMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentTypeMaster_UpdatedBy",
                table: "EmploymentTypeMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FormControlRoleMaster_CreatedBy",
                table: "FormControlRoleMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FormControlRoleMaster_FormId",
                table: "FormControlRoleMaster",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormControlRoleMaster_RoleId",
                table: "FormControlRoleMaster",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_FormControlRoleMaster_UpdatedBy",
                table: "FormControlRoleMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FormMaster_MenuId",
                table: "FormMaster",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_LanguageMaster_CreatedBy",
                table: "LanguageMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LanguageMaster_UpdatedBy",
                table: "LanguageMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LectureMaterial_CourseQuadrantId",
                table: "LectureMaterial",
                column: "CourseQuadrantId");

            migrationBuilder.CreateIndex(
                name: "IX_LectureMaterial_CreatedBy",
                table: "LectureMaterial",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LectureMaterial_UpdatedBy",
                table: "LectureMaterial",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LocationMaster_CreatedBy",
                table: "LocationMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LocationMaster_UpdatedBy",
                table: "LocationMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MenuMaster_CreatedBy",
                table: "MenuMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MenuMaster_UpdatedBy",
                table: "MenuMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NominationRequest_CandidateId",
                table: "NominationRequest",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationRequest_CreatedBy",
                table: "NominationRequest",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NominationRequest_TrainingId",
                table: "NominationRequest",
                column: "TrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationRequest_UpdatedBy",
                table: "NominationRequest",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NominationRequestRemarks_CreatedBy",
                table: "NominationRequestRemarks",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NominationRequestRemarks_NominationRequestId",
                table: "NominationRequestRemarks",
                column: "NominationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationRequestRemarks_UpdatedBy",
                table: "NominationRequestRemarks",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoGalleryMaster_CourseId",
                table: "PhotoGalleryMaster",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoGalleryMaster_CreatedBy",
                table: "PhotoGalleryMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoGalleryMaster_UpdatedBy",
                table: "PhotoGalleryMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_QueryMaster_CourseId",
                table: "QueryMaster",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_QueryMaster_CreatedBy",
                table: "QueryMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_QueryMaster_UpdatedBy",
                table: "QueryMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionMaster_CreatedBy",
                table: "QuestionMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionMaster_UpdatedBy",
                table: "QuestionMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptionMaster_CreatedBy",
                table: "QuestionOptionMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptionMaster_QuestionId",
                table: "QuestionOptionMaster",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptionMaster_UpdatedBy",
                table: "QuestionOptionMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_QuizMaster_CourseQuadrantId",
                table: "QuizMaster",
                column: "CourseQuadrantId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizMaster_CreatedBy",
                table: "QuizMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_QuizMaster_UpdatedBy",
                table: "QuizMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_QuizOption_CreatedBy",
                table: "QuizOption",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_QuizOption_QuizQuestionId",
                table: "QuizOption",
                column: "QuizQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizOption_UpdatedBy",
                table: "QuizOption",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestion_CreatedBy",
                table: "QuizQuestion",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestion_QuizId",
                table: "QuizQuestion",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestion_UpdatedBy",
                table: "QuizQuestion",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RankingMaster_CreatedBy",
                table: "RankingMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RankingMaster_UpdatedBy",
                table: "RankingMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RoleMaster_CreatedBy",
                table: "RoleMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RoleMaster_UpdatedBy",
                table: "RoleMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RoleMaster_UserTypeId",
                table: "RoleMaster",
                column: "UserTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_FormId",
                table: "RolePermissions",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_SemesterMaster_CreatedBy",
                table: "SemesterMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SemesterMaster_UpdatedBy",
                table: "SemesterMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SignatureMaster_CreatedBy",
                table: "SignatureMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SignatureMaster_UpdatedBy",
                table: "SignatureMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingMatrix_CourseId",
                table: "TrainingMatrix",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingMatrix_CreatedBy",
                table: "TrainingMatrix",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingMatrix_DepartmentId",
                table: "TrainingMatrix",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingMatrix_DesignationId",
                table: "TrainingMatrix",
                column: "DesignationId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingMatrix_DivisionId",
                table: "TrainingMatrix",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingMatrix_LocationId",
                table: "TrainingMatrix",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingMatrix_UpdatedBy",
                table: "TrainingMatrix",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingScheduleApprovalHistory_CreatedBy",
                table: "TrainingScheduleApprovalHistory",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingScheduleApprovalHistory_TrainingScheduleId",
                table: "TrainingScheduleApprovalHistory",
                column: "TrainingScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingScheduleApprovalHistory_UpdatedByUserId",
                table: "TrainingScheduleApprovalHistory",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingScheduleAttachments_TraningScheduleId",
                table: "TrainingScheduleAttachments",
                column: "TraningScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingScheduleLanguage_LanguageId",
                table: "TrainingScheduleLanguage",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSchedules_ConductedByDivisionId",
                table: "TrainingSchedules",
                column: "ConductedByDivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSchedules_CourseId",
                table: "TrainingSchedules",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSchedules_CreatedBy",
                table: "TrainingSchedules",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSchedules_InstructorId",
                table: "TrainingSchedules",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSchedules_StatusId",
                table: "TrainingSchedules",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSchedules_UpdatedBy",
                table: "TrainingSchedules",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSchedules_VenueId",
                table: "TrainingSchedules",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingScheduleStatusMaster_CreatedBy",
                table: "TrainingScheduleStatusMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingScheduleStatusMaster_UpdatedBy",
                table: "TrainingScheduleStatusMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserMaster_CreatedBy",
                table: "UserMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserMaster_DepartmentId",
                table: "UserMaster",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMaster_DesignationId",
                table: "UserMaster",
                column: "DesignationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMaster_DivisionId",
                table: "UserMaster",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMaster_EmploymentTypeId",
                table: "UserMaster",
                column: "EmploymentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMaster_LocationId",
                table: "UserMaster",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMaster_ManagerId",
                table: "UserMaster",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMaster_RoleId",
                table: "UserMaster",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMaster_SupervisorId",
                table: "UserMaster",
                column: "SupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMaster_UpdatedBy",
                table: "UserMaster",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_VenueMaster_CreatedBy",
                table: "VenueMaster",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VenueMaster_UpdatedBy",
                table: "VenueMaster",
                column: "UpdatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_BadgeMaster_CourseCategoryMaster_CourseCategoryId",
                table: "BadgeMaster",
                column: "CourseCategoryId",
                principalTable: "CourseCategoryMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BadgeMaster_DepartmentMaster_DepartmentId",
                table: "BadgeMaster",
                column: "DepartmentId",
                principalTable: "DepartmentMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BadgeMaster_UserMaster_CreatedBy",
                table: "BadgeMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BadgeMaster_UserMaster_UpdatedBy",
                table: "BadgeMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateMaster_UserMaster_CreatedBy",
                table: "CertificateMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateMaster_UserMaster_UpdatedBy",
                table: "CertificateMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyMaster_UserMaster_CreatedBy",
                table: "CompanyMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyMaster_UserMaster_UpdatedBy",
                table: "CompanyMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseCategoryMaster_UserMaster_CreatedBy",
                table: "CourseCategoryMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseCategoryMaster_UserMaster_UpdatedBy",
                table: "CourseCategoryMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEmploymentTypeMappings_CourseMaster_CourseId",
                table: "CourseEmploymentTypeMappings",
                column: "CourseId",
                principalTable: "CourseMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEmploymentTypeMappings_EmploymentTypeMaster_EmploymentTypeId",
                table: "CourseEmploymentTypeMappings",
                column: "EmploymentTypeId",
                principalTable: "EmploymentTypeMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEmploymentTypeMappings_UserMaster_CreatedBy",
                table: "CourseEmploymentTypeMappings",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEmploymentTypeMappings_UserMaster_UpdatedBy",
                table: "CourseEmploymentTypeMappings",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollment_CourseMaster_CourseId",
                table: "CourseEnrollment",
                column: "CourseId",
                principalTable: "CourseMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollment_UserMaster_CreatedBy",
                table: "CourseEnrollment",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollment_UserMaster_UpdatedBy",
                table: "CourseEnrollment",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollment_UserMaster_UserId",
                table: "CourseEnrollment",
                column: "UserId",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseMaster_SemesterMaster_SemesterId",
                table: "CourseMaster",
                column: "SemesterId",
                principalTable: "SemesterMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseMaster_UserMaster_CreatedBy",
                table: "CourseMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseMaster_UserMaster_UpdatedBy",
                table: "CourseMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseQuadrantMaster_UserMaster_CreatedBy",
                table: "CourseQuadrantMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseQuadrantMaster_UserMaster_UpdatedBy",
                table: "CourseQuadrantMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentMaster_LocationMaster_LocationId",
                table: "DepartmentMaster",
                column: "LocationId",
                principalTable: "LocationMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentMaster_UserMaster_CreatedBy",
                table: "DepartmentMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentMaster_UserMaster_UpdatedBy",
                table: "DepartmentMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DesignationMaster_DivisionMaster_DivisionId",
                table: "DesignationMaster",
                column: "DivisionId",
                principalTable: "DivisionMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DesignationMaster_UserMaster_CreatedBy",
                table: "DesignationMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DesignationMaster_UserMaster_UpdatedBy",
                table: "DesignationMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionComment_DiscussionThread_ThreadId",
                table: "DiscussionComment",
                column: "ThreadId",
                principalTable: "DiscussionThread",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionComment_UserMaster_CreatedBy",
                table: "DiscussionComment",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionComment_UserMaster_UpdatedBy",
                table: "DiscussionComment",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionComment_UserMaster_UserId",
                table: "DiscussionComment",
                column: "UserId",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionThread_UserMaster_CreatedByUserId",
                table: "DiscussionThread",
                column: "CreatedByUserId",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionThread_UserMaster_UpdatedBy",
                table: "DiscussionThread",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DivisionMaster_UserMaster_CreatedBy",
                table: "DivisionMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DivisionMaster_UserMaster_UpdatedBy",
                table: "DivisionMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmploymentTypeMaster_UserMaster_CreatedBy",
                table: "EmploymentTypeMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmploymentTypeMaster_UserMaster_UpdatedBy",
                table: "EmploymentTypeMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FormControlMaster_FormMaster_FormId",
                table: "FormControlMaster",
                column: "FormId",
                principalTable: "FormMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FormControlRoleMaster_FormMaster_FormId",
                table: "FormControlRoleMaster",
                column: "FormId",
                principalTable: "FormMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FormControlRoleMaster_RoleMaster_RoleId",
                table: "FormControlRoleMaster",
                column: "RoleId",
                principalTable: "RoleMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FormControlRoleMaster_UserMaster_CreatedBy",
                table: "FormControlRoleMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FormControlRoleMaster_UserMaster_UpdatedBy",
                table: "FormControlRoleMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FormMaster_MenuMaster_MenuId",
                table: "FormMaster",
                column: "MenuId",
                principalTable: "MenuMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LanguageMaster_UserMaster_CreatedBy",
                table: "LanguageMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LanguageMaster_UserMaster_UpdatedBy",
                table: "LanguageMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LectureMaterial_UserMaster_CreatedBy",
                table: "LectureMaterial",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LectureMaterial_UserMaster_UpdatedBy",
                table: "LectureMaterial",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LocationMaster_UserMaster_CreatedBy",
                table: "LocationMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LocationMaster_UserMaster_UpdatedBy",
                table: "LocationMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuMaster_UserMaster_CreatedBy",
                table: "MenuMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuMaster_UserMaster_UpdatedBy",
                table: "MenuMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NominationRequest_TrainingSchedules_TrainingId",
                table: "NominationRequest",
                column: "TrainingId",
                principalTable: "TrainingSchedules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NominationRequest_UserMaster_CandidateId",
                table: "NominationRequest",
                column: "CandidateId",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NominationRequest_UserMaster_CreatedBy",
                table: "NominationRequest",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NominationRequest_UserMaster_UpdatedBy",
                table: "NominationRequest",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NominationRequestRemarks_UserMaster_CreatedBy",
                table: "NominationRequestRemarks",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NominationRequestRemarks_UserMaster_UpdatedBy",
                table: "NominationRequestRemarks",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoGalleryMaster_UserMaster_CreatedBy",
                table: "PhotoGalleryMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoGalleryMaster_UserMaster_UpdatedBy",
                table: "PhotoGalleryMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QueryMaster_UserMaster_CreatedBy",
                table: "QueryMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QueryMaster_UserMaster_UpdatedBy",
                table: "QueryMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionMaster_UserMaster_CreatedBy",
                table: "QuestionMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionMaster_UserMaster_UpdatedBy",
                table: "QuestionMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionOptionMaster_UserMaster_CreatedBy",
                table: "QuestionOptionMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionOptionMaster_UserMaster_UpdatedBy",
                table: "QuestionOptionMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizMaster_UserMaster_CreatedBy",
                table: "QuizMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizMaster_UserMaster_UpdatedBy",
                table: "QuizMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizOption_QuizQuestion_QuizQuestionId",
                table: "QuizOption",
                column: "QuizQuestionId",
                principalTable: "QuizQuestion",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizOption_UserMaster_CreatedBy",
                table: "QuizOption",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizOption_UserMaster_UpdatedBy",
                table: "QuizOption",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizQuestion_UserMaster_CreatedBy",
                table: "QuizQuestion",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizQuestion_UserMaster_UpdatedBy",
                table: "QuizQuestion",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RankingMaster_UserMaster_CreatedBy",
                table: "RankingMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RankingMaster_UserMaster_UpdatedBy",
                table: "RankingMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RoleMaster_UserMaster_CreatedBy",
                table: "RoleMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoleMaster_UserMaster_UpdatedBy",
                table: "RoleMaster",
                column: "UpdatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DivisionMaster_DepartmentMaster_DepartmentId",
                table: "DivisionMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMaster_DepartmentMaster_DepartmentId",
                table: "UserMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_DesignationMaster_UserMaster_CreatedBy",
                table: "DesignationMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_DesignationMaster_UserMaster_UpdatedBy",
                table: "DesignationMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_DivisionMaster_UserMaster_CreatedBy",
                table: "DivisionMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_DivisionMaster_UserMaster_UpdatedBy",
                table: "DivisionMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_EmploymentTypeMaster_UserMaster_CreatedBy",
                table: "EmploymentTypeMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_EmploymentTypeMaster_UserMaster_UpdatedBy",
                table: "EmploymentTypeMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_LocationMaster_UserMaster_CreatedBy",
                table: "LocationMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_LocationMaster_UserMaster_UpdatedBy",
                table: "LocationMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_RoleMaster_UserMaster_CreatedBy",
                table: "RoleMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_RoleMaster_UserMaster_UpdatedBy",
                table: "RoleMaster");

            migrationBuilder.DropTable(
                name: "BadgeMaster");

            migrationBuilder.DropTable(
                name: "CertificateMaster");

            migrationBuilder.DropTable(
                name: "CompanyMaster");

            migrationBuilder.DropTable(
                name: "CourseEmploymentTypeMappings");

            migrationBuilder.DropTable(
                name: "CourseEnrollment");

            migrationBuilder.DropTable(
                name: "DiscussionComment");

            migrationBuilder.DropTable(
                name: "FormControlMaster");

            migrationBuilder.DropTable(
                name: "FormControlRoleMaster");

            migrationBuilder.DropTable(
                name: "LectureMaterial");

            migrationBuilder.DropTable(
                name: "NominationRequestRemarks");

            migrationBuilder.DropTable(
                name: "PhotoGalleryMaster");

            migrationBuilder.DropTable(
                name: "QueryMaster");

            migrationBuilder.DropTable(
                name: "QuestionOptionMaster");

            migrationBuilder.DropTable(
                name: "QuizOption");

            migrationBuilder.DropTable(
                name: "RankingMaster");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SignatureMaster");

            migrationBuilder.DropTable(
                name: "TrainingMatrix");

            migrationBuilder.DropTable(
                name: "TrainingScheduleApprovalHistory");

            migrationBuilder.DropTable(
                name: "TrainingScheduleAttachments");

            migrationBuilder.DropTable(
                name: "TrainingScheduleLanguage");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "DiscussionThread");

            migrationBuilder.DropTable(
                name: "NominationRequest");

            migrationBuilder.DropTable(
                name: "QuestionMaster");

            migrationBuilder.DropTable(
                name: "QuizQuestion");

            migrationBuilder.DropTable(
                name: "FormMaster");

            migrationBuilder.DropTable(
                name: "LanguageMaster");

            migrationBuilder.DropTable(
                name: "TrainingSchedules");

            migrationBuilder.DropTable(
                name: "QuizMaster");

            migrationBuilder.DropTable(
                name: "MenuMaster");

            migrationBuilder.DropTable(
                name: "TrainingScheduleStatusMaster");

            migrationBuilder.DropTable(
                name: "VenueMaster");

            migrationBuilder.DropTable(
                name: "CourseQuadrantMaster");

            migrationBuilder.DropTable(
                name: "CourseMaster");

            migrationBuilder.DropTable(
                name: "CourseCategoryMaster");

            migrationBuilder.DropTable(
                name: "SemesterMaster");

            migrationBuilder.DropTable(
                name: "DepartmentMaster");

            migrationBuilder.DropTable(
                name: "UserMaster");

            migrationBuilder.DropTable(
                name: "DesignationMaster");

            migrationBuilder.DropTable(
                name: "EmploymentTypeMaster");

            migrationBuilder.DropTable(
                name: "LocationMaster");

            migrationBuilder.DropTable(
                name: "RoleMaster");

            migrationBuilder.DropTable(
                name: "DivisionMaster");

            migrationBuilder.DropTable(
                name: "UserType");
        }
    }
}
