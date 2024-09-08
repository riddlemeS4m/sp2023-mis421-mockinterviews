using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Interfaces.IDbContext
{
    public interface ISignupDbContext
    {
        public DbSet<Question> Questions { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Interview> Interviews { get; set; }
        public DbSet<InterviewerLocation> InterviewerLocations { get; set; }
        public DbSet<InterviewerSignup> InterviewerSignups { get; set; }
        public DbSet<InterviewerTimeslot> InterviewerTimeslots { get; set; }
        public DbSet<Timeslot> Timeslots { get; set; }
        public DbSet<VolunteerTimeslot> VolunteerTimeslots { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<RosteredStudent> Roster { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }

        DbSet<T> Set<T>() where T : class;
        void Add<T>(T entity) where T : class;
        void AddRange<T>(IEnumerable<T> entities) where T : class;
        void Update<T>(T entity) where T : class;
        void Remove<T>(T entity) where T : class;
        EntityEntry<T> Entry<T>(T entity) where T : class;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}