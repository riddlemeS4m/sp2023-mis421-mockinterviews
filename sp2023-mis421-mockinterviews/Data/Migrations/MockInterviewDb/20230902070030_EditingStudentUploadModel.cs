using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp2023_mis421_mockinterviews.Data.Migrations.MockInterviewDb
{
    public partial class EditingStudentUploadModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MicrosoftId",
                table: "MSTeamsStudentUpload",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "In221",
                table: "MSTeamsStudentUpload",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "InMasters",
                table: "MSTeamsStudentUpload",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "In221",
                table: "MSTeamsStudentUpload");

            migrationBuilder.DropColumn(
                name: "InMasters",
                table: "MSTeamsStudentUpload");

            migrationBuilder.AlterColumn<string>(
                name: "MicrosoftId",
                table: "MSTeamsStudentUpload",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
