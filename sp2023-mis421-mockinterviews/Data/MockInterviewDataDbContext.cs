using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Data
{
    public class MockInterviewDataDbContext : DbContext
    {
        public MockInterviewDataDbContext(DbContextOptions<MockInterviewDataDbContext> options)
            : base(options)
        {
        }

        public DbSet<FAQs>? FAQs { get; set; }
        public DbSet<Location>? Location { get; set; }
        public DbSet<sp2023_mis421_mockinterviews.Models.MockInterviewDb.Interviewer>? Interviewer { get; set; }
        public DbSet<sp2023_mis421_mockinterviews.Models.MockInterviewDb.InterviewEvent>? InterviewEvent { get; set; }
        public DbSet<sp2023_mis421_mockinterviews.Models.MockInterviewDb.LocationInterviewer>? LocationInterviewer { get; set; }
        public DbSet<sp2023_mis421_mockinterviews.Models.MockInterviewDb.SignupInterviewer>? SignupInterviewer { get; set; }
        public DbSet<sp2023_mis421_mockinterviews.Models.MockInterviewDb.SignupInterviewerTimeslot>? SignupInterviewerTimeslot { get; set; }
        public DbSet<sp2023_mis421_mockinterviews.Models.MockInterviewDb.Timeslot>? Timeslot { get; set; }
        public DbSet<sp2023_mis421_mockinterviews.Models.MockInterviewDb.VolunteerEvent>? VolunteerEvent { get; set; }
        public DbSet<sp2023_mis421_mockinterviews.Models.MockInterviewDb.EventDate>? EventDate { get; set; }
    }
}