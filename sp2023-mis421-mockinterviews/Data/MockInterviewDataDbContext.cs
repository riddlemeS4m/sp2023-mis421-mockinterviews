using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using sp2023_mis421_mockinterviews.Interfaces.IDbContext;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Data
{
    public class MockInterviewDataDbContext : DbContext
    {
        public MockInterviewDataDbContext(DbContextOptions<MockInterviewDataDbContext> options)
            : base(options)
        {
        }

        public DbSet<Question>? FAQs { get; set; }
        public DbSet<Location>? Location { get; set; }
        public DbSet<sp2023_mis421_mockinterviews.Models.MockInterviewDb.Interview>? InterviewEvent { get; set; }
        public DbSet<sp2023_mis421_mockinterviews.Models.MockInterviewDb.InterviewerLocation>? LocationInterviewer { get; set; }
        public DbSet<sp2023_mis421_mockinterviews.Models.MockInterviewDb.InterviewerSignup>? SignupInterviewer { get; set; }
        public DbSet<sp2023_mis421_mockinterviews.Models.MockInterviewDb.InterviewerTimeslot>? SignupInterviewerTimeslot { get; set; }
        public DbSet<sp2023_mis421_mockinterviews.Models.MockInterviewDb.Timeslot>? Timeslot { get; set; }
        public DbSet<sp2023_mis421_mockinterviews.Models.MockInterviewDb.VolunteerTimeslot>? VolunteerEvent { get; set; }
        public DbSet<sp2023_mis421_mockinterviews.Models.MockInterviewDb.Event>? EventDate { get; set; }
        public DbSet<sp2023_mis421_mockinterviews.Models.MockInterviewDb.RosteredStudent>? MSTeamsStudentUpload { get; set; }
        public DbSet<sp2023_mis421_mockinterviews.Models.MockInterviewDb.Setting> GlobalConfigVar { get; set; }
        public DbSet<sp2023_mis421_mockinterviews.Models.MockInterviewDb.EmailTemplate> EmailTemplate { get; set; }

        public DbSet<T> Set<T>() where T : class
        {
            return base.Set<T>();
        }

        public void Add<T>(T entity) where T : class
        {
            base.Add(entity);
        }

        public void AddRange<T>(IEnumerable<T> entities) where T : class
        {
            base.AddRange(entities);
        }

        public void Update<T>(T entity) where T : class
        {
            base.Update(entity);
        }

        public void Remove<T>(T entity) where T : class
        {
            base.Remove(entity);
        }

        public EntityEntry<T> Entry<T>(T entity) where T : class
        {
            return base.Entry(entity);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}