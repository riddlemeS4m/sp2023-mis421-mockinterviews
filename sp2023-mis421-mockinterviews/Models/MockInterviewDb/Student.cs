namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class Student
    {
        //do we need isactive field?

        //LT - yes just in case
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Semester { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsAmbassador { get; set; }
        public bool IsActive { get; set; }
    }
}
