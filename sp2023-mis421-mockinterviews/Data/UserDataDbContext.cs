using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Models;
using sp2023_mis421_mockinterviews.Models.UserDb;

namespace sp2023_mis421_mockinterviews.Data
{
    public class UserDataDbContext : IdentityDbContext<ApplicationUser>
    {
        public UserDataDbContext(DbContextOptions<UserDataDbContext> options)
            : base(options)
        {
        }
    }
}
