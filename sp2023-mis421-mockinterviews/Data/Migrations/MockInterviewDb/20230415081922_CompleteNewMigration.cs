using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp2023_mis421_mockinterviews.Data.Migrations.MockInterviewDb
{
    public partial class CompleteNewMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Event",
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

            migrationBuilder.CreateTable(
                name: "Question",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Question = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FAQs", x => x.Id);
                });

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
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Room = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsVirtual = table.Column<bool>(type: "bit", nullable: false),
                    InPerson = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Location", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Timeslots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Time = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventDateId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsVolunteer = table.Column<bool>(type: "bit", nullable: false),
                    IsInterviewer = table.Column<bool>(type: "bit", nullable: false),
                    IsStudent = table.Column<bool>(type: "bit", nullable: false),
                    MaxSignUps = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timeslot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timeslot_EventDate_EventDateId",
                        column: x => x.EventDateId,
                        principalTable: "Event",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterviewerSignup",
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
                name: "InterviewerLocation",
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
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerTimeslot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeslotId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VolunteerEvent_Timeslot_TimeslotId",
                        column: x => x.TimeslotId,
                        principalTable: "Timeslots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterviewerTimeslot",
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
                        principalTable: "InterviewerSignup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SignupInterviewerTimeslot_Timeslot_TimeslotId",
                        column: x => x.TimeslotId,
                        principalTable: "Timeslots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Interview",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
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
                        principalTable: "Locations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewEvent_SignupInterviewerTimeslot_SignupInterviewerTimeslotId",
                        column: x => x.SignupInterviewerTimeslotId,
                        principalTable: "InterviewerTimeslot",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewEvent_Timeslot_TimeslotId",
                        column: x => x.TimeslotId,
                        principalTable: "Timeslots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewEvent_LocationId",
                table: "Interview",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewEvent_SignupInterviewerTimeslotId",
                table: "Interview",
                column: "InterviewerTimeslotId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewEvent_TimeslotId",
                table: "Interview",
                column: "TimeslotId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationInterviewer_InterviewerId",
                table: "InterviewerLocation",
                column: "InterviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationInterviewer_LocationId",
                table: "InterviewerLocation",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SignupInterviewer_InterviewerId",
                table: "InterviewerSignup",
                column: "InterviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_SignupInterviewerTimeslot_SignupInterviewerId",
                table: "InterviewerTimeslot",
                column: "InterviewerSignupId");

            migrationBuilder.CreateIndex(
                name: "IX_SignupInterviewerTimeslot_TimeslotId",
                table: "InterviewerTimeslot",
                column: "TimeslotId");

            migrationBuilder.CreateIndex(
                name: "IX_Timeslot_EventDateId",
                table: "Timeslots",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerEvent_TimeslotId",
                table: "VolunteerTimeslot",
                column: "TimeslotId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Question");

            migrationBuilder.DropTable(
                name: "Interview");

            migrationBuilder.DropTable(
                name: "InterviewerLocation");

            migrationBuilder.DropTable(
                name: "VolunteerTimeslot");

            migrationBuilder.DropTable(
                name: "InterviewerTimeslot");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "InterviewerSignup");

            migrationBuilder.DropTable(
                name: "Timeslots");

            migrationBuilder.DropTable(
                name: "Interviewer");

            migrationBuilder.DropTable(
                name: "Event");
        }
    }
}
