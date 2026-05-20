using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Repository.Migrations
{
    public partial class updatesusermodel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentMaster_UserMaster_CreatedBy",
                table: "DepartmentMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_DesignationMaster_UserMaster_CreatedBy",
                table: "DesignationMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_DivisionMaster_UserMaster_CreatedBy",
                table: "DivisionMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_EmploymentTypeMaster_UserMaster_CreatedBy",
                table: "EmploymentTypeMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_LocationMaster_UserMaster_CreatedBy",
                table: "LocationMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMaster_DepartmentMaster_DepartmentId",
                table: "UserMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMaster_DesignationMaster_DesignationId",
                table: "UserMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMaster_DivisionMaster_DivisionId",
                table: "UserMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMaster_EmploymentTypeMaster_EmploymentTypeId",
                table: "UserMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMaster_LocationMaster_LocationId",
                table: "UserMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMaster_UserMaster_ManagerId",
                table: "UserMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMaster_UserMaster_SupervisorId",
                table: "UserMaster");

            migrationBuilder.DropIndex(
                name: "IX_UserMaster_DepartmentId",
                table: "UserMaster");

            migrationBuilder.DropIndex(
                name: "IX_UserMaster_DesignationId",
                table: "UserMaster");

            migrationBuilder.DropIndex(
                name: "IX_UserMaster_DivisionId",
                table: "UserMaster");

            migrationBuilder.DropIndex(
                name: "IX_UserMaster_EmploymentTypeId",
                table: "UserMaster");

            migrationBuilder.DropIndex(
                name: "IX_UserMaster_LocationId",
                table: "UserMaster");

            migrationBuilder.DropIndex(
                name: "IX_UserMaster_ManagerId",
                table: "UserMaster");

            migrationBuilder.DropIndex(
                name: "IX_UserMaster_SupervisorId",
                table: "UserMaster");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "UserMaster");

            migrationBuilder.DropColumn(
                name: "DesignationId",
                table: "UserMaster");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "UserMaster");

            migrationBuilder.DropColumn(
                name: "EmploymentTypeId",
                table: "UserMaster");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "UserMaster");

            migrationBuilder.DropColumn(
                name: "ManagerId",
                table: "UserMaster");

            migrationBuilder.DropColumn(
                name: "SupervisorId",
                table: "UserMaster");

            migrationBuilder.AlterColumn<int>(
                name: "ContactNo",
                table: "UserMaster",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 6)
                .OldAnnotation("Relational:ColumnOrder", 13);

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentMaster_UserMaster_CreatedBy",
                table: "DepartmentMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DesignationMaster_UserMaster_CreatedBy",
                table: "DesignationMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DivisionMaster_UserMaster_CreatedBy",
                table: "DivisionMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmploymentTypeMaster_UserMaster_CreatedBy",
                table: "EmploymentTypeMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LocationMaster_UserMaster_CreatedBy",
                table: "LocationMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentMaster_UserMaster_CreatedBy",
                table: "DepartmentMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_DesignationMaster_UserMaster_CreatedBy",
                table: "DesignationMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_DivisionMaster_UserMaster_CreatedBy",
                table: "DivisionMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_EmploymentTypeMaster_UserMaster_CreatedBy",
                table: "EmploymentTypeMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_LocationMaster_UserMaster_CreatedBy",
                table: "LocationMaster");

            migrationBuilder.AlterColumn<int>(
                name: "ContactNo",
                table: "UserMaster",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 13)
                .OldAnnotation("Relational:ColumnOrder", 6);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "UserMaster",
                type: "int",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 8);

            migrationBuilder.AddColumn<int>(
                name: "DesignationId",
                table: "UserMaster",
                type: "int",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 10);

            migrationBuilder.AddColumn<int>(
                name: "DivisionId",
                table: "UserMaster",
                type: "int",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 9);

            migrationBuilder.AddColumn<int>(
                name: "EmploymentTypeId",
                table: "UserMaster",
                type: "int",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 6);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "UserMaster",
                type: "int",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 7);

            migrationBuilder.AddColumn<int>(
                name: "ManagerId",
                table: "UserMaster",
                type: "int",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 12);

            migrationBuilder.AddColumn<int>(
                name: "SupervisorId",
                table: "UserMaster",
                type: "int",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 11);

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
                name: "IX_UserMaster_SupervisorId",
                table: "UserMaster",
                column: "SupervisorId");

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentMaster_UserMaster_CreatedBy",
                table: "DepartmentMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DesignationMaster_UserMaster_CreatedBy",
                table: "DesignationMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DivisionMaster_UserMaster_CreatedBy",
                table: "DivisionMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmploymentTypeMaster_UserMaster_CreatedBy",
                table: "EmploymentTypeMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LocationMaster_UserMaster_CreatedBy",
                table: "LocationMaster",
                column: "CreatedBy",
                principalTable: "UserMaster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMaster_DepartmentMaster_DepartmentId",
                table: "UserMaster",
                column: "DepartmentId",
                principalTable: "DepartmentMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserMaster_DesignationMaster_DesignationId",
                table: "UserMaster",
                column: "DesignationId",
                principalTable: "DesignationMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserMaster_DivisionMaster_DivisionId",
                table: "UserMaster",
                column: "DivisionId",
                principalTable: "DivisionMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserMaster_EmploymentTypeMaster_EmploymentTypeId",
                table: "UserMaster",
                column: "EmploymentTypeId",
                principalTable: "EmploymentTypeMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserMaster_LocationMaster_LocationId",
                table: "UserMaster",
                column: "LocationId",
                principalTable: "LocationMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserMaster_UserMaster_ManagerId",
                table: "UserMaster",
                column: "ManagerId",
                principalTable: "UserMaster",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserMaster_UserMaster_SupervisorId",
                table: "UserMaster",
                column: "SupervisorId",
                principalTable: "UserMaster",
                principalColumn: "Id");
        }
    }
}
