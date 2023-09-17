using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp2023_mis421_mockinterviews.Data.Migrations.UserDb
{
    public partial class AddedStudentClass : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Class",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Class",
                table: "AspNetUsers");
        }
    }
}
