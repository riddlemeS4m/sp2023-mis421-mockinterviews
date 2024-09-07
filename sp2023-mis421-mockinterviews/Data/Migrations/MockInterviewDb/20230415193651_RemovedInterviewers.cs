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
                table: "InterviewerLocation");

            migrationBuilder.DropForeignKey(
                name: "FK_SignupInterviewer_Interviewer_InterviewerId",
                table: "InterviewerSignup");

            migrationBuilder.DropTable(
                name: "Interviewer");

            migrationBuilder.DropIndex(
                name: "IX_SignupInterviewer_InterviewerId",
                table: "InterviewerSignup");

            migrationBuilder.DropIndex(
                name: "IX_LocationInterviewer_InterviewerId",
                table: "InterviewerLocation");

            migrationBuilder.AlterColumn<string>(
                name: "InterviewerId",
                table: "InterviewerSignup",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "InterviewerId",
                table: "InterviewerLocation",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "InterviewerId",
                table: "InterviewerSignup",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "InterviewerId",
                table: "InterviewerLocation",
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
                table: "InterviewerSignup",
                column: "InterviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationInterviewer_InterviewerId",
                table: "InterviewerLocation",
                column: "InterviewerId");

            migrationBuilder.AddForeignKey(
                name: "FK_LocationInterviewer_Interviewer_InterviewerId",
                table: "InterviewerLocation",
                column: "InterviewerId",
                principalTable: "Interviewer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SignupInterviewer_Interviewer_InterviewerId",
                table: "InterviewerSignup",
                column: "InterviewerId",
                principalTable: "Interviewer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
