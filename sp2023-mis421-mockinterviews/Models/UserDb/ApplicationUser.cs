using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace sp2023_mis421_mockinterviews.Models.UserDb
{
    public class ApplicationUser : IdentityUser
    {
        [Display(Name ="First Name")]
        public string? FirstName { get; set; }
        [Display(Name = "Last Name")]
        public string? LastName { get; set; }
        public string? Class { get; set; }
        public string? Company { get; set; }
        [Display(Name = "Profile Picture")]
        public byte[]? ProfilePicture { get; set; }
        public byte[]? Resume { get; set; }
    }
}
