using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp2023_mis421_mockinterviews.Data.Migrations.MockInterviewDb
{
    public partial class AddedLocationDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "InterviewerLocation",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationInterviewer_EventDateId",
                table: "InterviewerLocation",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_LocationInterviewer_EventDate_EventDateId",
                table: "InterviewerLocation",
                column: "EventId",
                principalTable: "Event",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LocationInterviewer_EventDate_EventDateId",
                table: "InterviewerLocation");

            migrationBuilder.DropIndex(
                name: "IX_LocationInterviewer_EventDateId",
                table: "InterviewerLocation");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "InterviewerLocation");
        }
    }
}
