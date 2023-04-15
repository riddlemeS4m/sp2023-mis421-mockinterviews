using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp2023_mis421_mockinterviews.Data.Migrations.MockInterviewDb
{
    public partial class RemovedInterviewers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LocationInterviewer_Interviewer_InterviewerId",
                table: "LocationInterviewer");

            migrationBuilder.DropForeignKey(
                name: "FK_SignupInterviewer_Interviewer_InterviewerId",
                table: "SignupInterviewer");

            migrationBuilder.DropTable(
                name: "Interviewer");

            migrationBuilder.DropIndex(
                name: "IX_SignupInterviewer_InterviewerId",
                table: "SignupInterviewer");

            migrationBuilder.DropIndex(
                name: "IX_LocationInterviewer_InterviewerId",
                table: "LocationInterviewer");

            migrationBuilder.AlterColumn<string>(
                name: "InterviewerId",
                table: "SignupInterviewer",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "InterviewerId",
                table: "LocationInterviewer",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "InterviewerId",
                table: "SignupInterviewer",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "InterviewerId",
                table: "LocationInterviewer",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "Interviewer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interviewer", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SignupInterviewer_InterviewerId",
                table: "SignupInterviewer",
                column: "InterviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationInterviewer_InterviewerId",
                table: "LocationInterviewer",
                column: "InterviewerId");

            migrationBuilder.AddForeignKey(
                name: "FK_LocationInterviewer_Interviewer_InterviewerId",
                table: "LocationInterviewer",
                column: "InterviewerId",
                principalTable: "Interviewer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SignupInterviewer_Interviewer_InterviewerId",
                table: "SignupInterviewer",
                column: "InterviewerId",
                principalTable: "Interviewer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
