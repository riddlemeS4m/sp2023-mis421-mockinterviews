namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class UserProfileViewModel
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public byte[]? ProfilePicture { get; set; }
        public byte[]? Resume { get; set; }
    }
}
