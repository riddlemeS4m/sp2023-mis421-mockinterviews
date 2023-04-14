using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp2023_mis421_mockinterviews.Data.Migrations.MockInterviewDb
{
    public partial class editedTimeslots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EventDateId",
                table: "Timeslot",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EventDate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventDate", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Timeslot_EventDateId",
                table: "Timeslot",
                column: "EventDateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Timeslot_EventDate_EventDateId",
                table: "Timeslot",
                column: "EventDateId",
                principalTable: "EventDate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Timeslot_EventDate_EventDateId",
                table: "Timeslot");

            migrationBuilder.DropTable(
                name: "EventDate");

            migrationBuilder.DropIndex(
                name: "IX_Timeslot_EventDateId",
                table: "Timeslot");

            migrationBuilder.DropColumn(
                name: "EventDateId",
                table: "Timeslot");
        }
    }
}
