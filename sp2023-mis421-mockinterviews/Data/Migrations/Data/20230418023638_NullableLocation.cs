using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp2023_mis421_mockinterviews.Data.Migrations.Data
{
    public partial class NullableLocation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LocationInterviewer_Location_LocationId",
                table: "LocationInterviewer");

            migrationBuilder.AlterColumn<int>(
                name: "LocationId",
                table: "LocationInterviewer",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_LocationInterviewer_Location_LocationId",
                table: "LocationInterviewer",
                column: "LocationId",
                principalTable: "Location",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LocationInterviewer_Location_LocationId",
                table: "LocationInterviewer");

            migrationBuilder.AlterColumn<int>(
                name: "LocationId",
                table: "LocationInterviewer",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LocationInterviewer_Location_LocationId",
                table: "LocationInterviewer",
                column: "LocationId",
                principalTable: "Location",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
