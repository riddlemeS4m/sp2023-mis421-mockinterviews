using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp2023_mis421_mockinterviews.Data.Migrations.MockInterviewDb
{
    public partial class AddedLocationDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EventDateId",
                table: "LocationInterviewer",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationInterviewer_EventDateId",
                table: "LocationInterviewer",
                column: "EventDateId");

            migrationBuilder.AddForeignKey(
                name: "FK_LocationInterviewer_EventDate_EventDateId",
                table: "LocationInterviewer",
                column: "EventDateId",
                principalTable: "EventDate",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LocationInterviewer_EventDate_EventDateId",
                table: "LocationInterviewer");

            migrationBuilder.DropIndex(
                name: "IX_LocationInterviewer_EventDateId",
                table: "LocationInterviewer");

            migrationBuilder.DropColumn(
                name: "EventDateId",
                table: "LocationInterviewer");
        }
    }
}
