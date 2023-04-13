using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp2023_mis421_mockinterviews.Data.Migrations.MockInterviewDb
{
    public partial class addedAllModels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "room",
                table: "Location",
                newName: "Room");

            migrationBuilder.RenameColumn(
                name: "isVirtual",
                table: "Location",
                newName: "IsVirtual");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Location",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "isPerson",
                table: "Location",
                newName: "InPerson");

            migrationBuilder.RenameColumn(
                name: "question",
                table: "FAQs",
                newName: "Question");

            migrationBuilder.RenameColumn(
                name: "answer",
                table: "FAQs",
                newName: "Answer");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "FAQs",
                newName: "Id");

            migrationBuilder.CreateTable(
                name: "Interviewer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interviewer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaxTimeSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Limit = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaxTimeSlots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Student",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    IsAmbassador = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Student", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Timeslot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Time = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsVolunteer = table.Column<bool>(type: "bit", nullable: false),
                    IsInterviewer = table.Column<bool>(type: "bit", nullable: false),
                    IsStudent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timeslot", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocationInterviewer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewerId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationInterviewer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationInterviewer_Interviewer_InterviewerId",
                        column: x => x.InterviewerId,
                        principalTable: "Interviewer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LocationInterviewer_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SignupInterviewer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsVirtual = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InPerson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsTechnical = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsBehavioral = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InterviewerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignupInterviewer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SignupInterviewer_Interviewer_InterviewerId",
                        column: x => x.InterviewerId,
                        principalTable: "Interviewer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerEvent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    TimeslotId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VolunteerEvent_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VolunteerEvent_Timeslot_TimeslotId",
                        column: x => x.TimeslotId,
                        principalTable: "Timeslot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SignupInterviewerTimeslot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SignupInterviewerId = table.Column<int>(type: "int", nullable: false),
                    TimeslotId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignupInterviewerTimeslot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SignupInterviewerTimeslot_SignupInterviewer_SignupInterviewerId",
                        column: x => x.SignupInterviewerId,
                        principalTable: "SignupInterviewer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SignupInterviewerTimeslot_Timeslot_TimeslotId",
                        column: x => x.TimeslotId,
                        principalTable: "Timeslot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterviewEvent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    TimeslotId = table.Column<int>(type: "int", nullable: false),
                    InterviewType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InterviewerRating = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InterviewerFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignupInterviewerTimeslotId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewEvent_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewEvent_SignupInterviewerTimeslot_SignupInterviewerTimeslotId",
                        column: x => x.SignupInterviewerTimeslotId,
                        principalTable: "SignupInterviewerTimeslot",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewEvent_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewEvent_Timeslot_TimeslotId",
                        column: x => x.TimeslotId,
                        principalTable: "Timeslot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewEvent_LocationId",
                table: "InterviewEvent",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewEvent_SignupInterviewerTimeslotId",
                table: "InterviewEvent",
                column: "SignupInterviewerTimeslotId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewEvent_StudentId",
                table: "InterviewEvent",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewEvent_TimeslotId",
                table: "InterviewEvent",
                column: "TimeslotId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationInterviewer_InterviewerId",
                table: "LocationInterviewer",
                column: "InterviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationInterviewer_LocationId",
                table: "LocationInterviewer",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SignupInterviewer_InterviewerId",
                table: "SignupInterviewer",
                column: "InterviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_SignupInterviewerTimeslot_SignupInterviewerId",
                table: "SignupInterviewerTimeslot",
                column: "SignupInterviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_SignupInterviewerTimeslot_TimeslotId",
                table: "SignupInterviewerTimeslot",
                column: "TimeslotId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerEvent_StudentId",
                table: "VolunteerEvent",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerEvent_TimeslotId",
                table: "VolunteerEvent",
                column: "TimeslotId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewEvent");

            migrationBuilder.DropTable(
                name: "LocationInterviewer");

            migrationBuilder.DropTable(
                name: "MaxTimeSlots");

            migrationBuilder.DropTable(
                name: "VolunteerEvent");

            migrationBuilder.DropTable(
                name: "SignupInterviewerTimeslot");

            migrationBuilder.DropTable(
                name: "Student");

            migrationBuilder.DropTable(
                name: "SignupInterviewer");

            migrationBuilder.DropTable(
                name: "Timeslot");

            migrationBuilder.DropTable(
                name: "Interviewer");

            migrationBuilder.RenameColumn(
                name: "Room",
                table: "Location",
                newName: "room");

            migrationBuilder.RenameColumn(
                name: "IsVirtual",
                table: "Location",
                newName: "isVirtual");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Location",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "InPerson",
                table: "Location",
                newName: "isPerson");

            migrationBuilder.RenameColumn(
                name: "Question",
                table: "FAQs",
                newName: "question");

            migrationBuilder.RenameColumn(
                name: "Answer",
                table: "FAQs",
                newName: "answer");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "FAQs",
                newName: "id");
        }
    }
}
