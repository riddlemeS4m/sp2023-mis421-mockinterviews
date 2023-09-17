namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class Location
    {
        public int Id { get; set; }
        public string Room { get; set; }
        public bool IsVirtual { get; set; }
        public bool InPerson { get; set; }
    }
}
