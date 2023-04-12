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
    }
}