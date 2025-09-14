using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using sp2023_mis421_mockinterviews.Interfaces.IDbContext;
using sp2023_mis421_mockinterviews.Models.UserDb;

namespace sp2023_mis421_mockinterviews.Data.Contexts
{
    public class UsersDbContext : IdentityDbContext<ApplicationUser>, IUserDbContext
    {
        public UsersDbContext(DbContextOptions<UsersDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }

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
