using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp2023_mis421_mockinterviews.Data.Migrations.MockInterviewDb
{
    public partial class IncludingLunchAndInterviewType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InterviewerPreference",
                table: "InterviewerLocation",
                newName: "Preference");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "InterviewerSignup",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCase",
                table: "InterviewerSignup",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Lunch",
                table: "InterviewerSignup",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "InterviewerSignup");

            migrationBuilder.DropColumn(
                name: "IsCase",
                table: "InterviewerSignup");

            migrationBuilder.DropColumn(
                name: "Lunch",
                table: "InterviewerSignup");

            migrationBuilder.RenameColumn(
                name: "Preference",
                table: "InterviewerLocation",
                newName: "InterviewerPreference");
        }
    }
}
