using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;

namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class SignupInterviewer
    {
        public int Id { get; set; }
        [ValidateNever]
        public string FirstName { get; set; }
        [ValidateNever]
        public string LastName { get; set; }
        public bool IsVirtual { get; set; }
        public bool InPerson { get; set; }
        public bool IsTechnical { get; set; }
        public bool IsBehavioral { get; set; }
        [ValidateNever]
        public string InterviewerId { get; set; }
    }
}
