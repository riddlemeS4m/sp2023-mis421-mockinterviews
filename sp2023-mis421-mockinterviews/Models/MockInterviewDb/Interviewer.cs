namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class Interviewer
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Rating { get; set; }
        public bool IsActive { get; set; }
    }
}
