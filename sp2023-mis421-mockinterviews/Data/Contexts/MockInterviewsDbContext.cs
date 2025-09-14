using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using sp2023_mis421_mockinterviews.Interfaces.IDbContext;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Data.Contexts
{
    public class MockInterviewsDbContext : DbContext, ISignupDbContext
    {
        public MockInterviewsDbContext(DbContextOptions<MockInterviewsDbContext> options)
            : base(options)
        {
        }

        public DbSet<Question> Questions { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Interview> Interviews { get; set; }
        public DbSet<InterviewerLocation> InterviewerLocations { get; set; }
        public DbSet<InterviewerSignup> InterviewerSignups { get; set; }
        public DbSet<InterviewerTimeslot> InterviewerTimeslots { get; set; }
        public DbSet<Timeslot> Timeslots { get; set; }
        public DbSet<VolunteerTimeslot> VolunteerTimeslots { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<RosteredStudent> RosteredStudents { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public IModel Model => base.Model;

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

        public IEnumerable<EntityEntry<T>> GetChangeTracker<T>() where T : class
        {
            return ChangeTracker.Entries<T>();
        }
    }
}