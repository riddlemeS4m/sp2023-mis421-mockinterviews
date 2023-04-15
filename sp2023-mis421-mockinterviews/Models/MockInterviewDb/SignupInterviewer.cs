using System.ComponentModel.DataAnnotations.Schema;

namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class SignupInterviewer
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string IsVirtual { get; set; }
        public string InPerson { get; set; }
        public string IsTechnical { get; set; }
        public string IsBehavioral { get; set; }
        public string InterviewerId { get; set; }
    }
}
