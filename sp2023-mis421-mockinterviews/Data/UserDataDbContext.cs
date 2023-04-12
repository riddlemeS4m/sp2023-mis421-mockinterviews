using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Models;

namespace sp2023_mis421_mockinterviews.Data
{
    public class UserDataDbContext : IdentityDbContext
    {
        public UserDataDbContext(DbContextOptions<UserDataDbContext> options)
            : base(options)
        {
        }
    }
}
