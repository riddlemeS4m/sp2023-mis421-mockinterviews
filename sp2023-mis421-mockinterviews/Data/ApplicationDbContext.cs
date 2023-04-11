using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Models;

namespace sp2023_mis421_mockinterviews.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<sp2023_mis421_mockinterviews.Models.FAQs>? FAQs { get; set; }
        public DbSet<sp2023_mis421_mockinterviews.Models.Location>? Location { get; set; }
    }
}