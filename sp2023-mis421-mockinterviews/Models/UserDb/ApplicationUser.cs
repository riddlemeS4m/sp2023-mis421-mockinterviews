using Microsoft.AspNetCore.Identity;

namespace sp2023_mis421_mockinterviews.Models.UserDb
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Class { get; set; }
        public byte[]? ProfilePicture { get; set; }
        public byte[]? Resume { get; set; }
    }
}
