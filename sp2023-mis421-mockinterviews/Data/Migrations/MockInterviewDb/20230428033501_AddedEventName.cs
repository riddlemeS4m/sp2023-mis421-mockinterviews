using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp2023_mis421_mockinterviews.Data.Migrations.MockInterviewDb
{
    public partial class AddedEventName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventName",
                table: "EventDate",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventName",
                table: "EventDate");
        }
    }
}
