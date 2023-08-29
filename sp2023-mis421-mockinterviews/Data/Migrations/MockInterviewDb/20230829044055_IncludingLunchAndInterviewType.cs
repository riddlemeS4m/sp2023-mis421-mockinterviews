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
                table: "LocationInterviewer",
                newName: "LocationPreference");

            migrationBuilder.AddColumn<string>(
                name: "InterviewType",
                table: "SignupInterviewer",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCase",
                table: "SignupInterviewer",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Lunch",
                table: "SignupInterviewer",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterviewType",
                table: "SignupInterviewer");

            migrationBuilder.DropColumn(
                name: "IsCase",
                table: "SignupInterviewer");

            migrationBuilder.DropColumn(
                name: "Lunch",
                table: "SignupInterviewer");

            migrationBuilder.RenameColumn(
                name: "LocationPreference",
                table: "LocationInterviewer",
                newName: "InterviewerPreference");
        }
    }
}
