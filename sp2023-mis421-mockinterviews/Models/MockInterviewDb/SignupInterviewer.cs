using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using sp2023_mis421_mockinterviews.Data.Constants;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class SignupInterviewer
    {
        public int Id { get; set; }
        [ValidateNever]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        [ValidateNever]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
        [Display(Name = InterviewLocationConstants.IsVirtual)]
        public bool IsVirtual { get; set; }
        [Display(Name = InterviewLocationConstants.InPerson)]
        public bool InPerson { get; set; }
        [Display(Name = InterviewTypeConstants.Technical)]
        public bool IsTechnical { get; set; }
        [Display(Name = InterviewTypeConstants.Behavioral)]
        public bool IsBehavioral { get; set; }
        [Display(Name = InterviewTypeConstants.Case)]
        public bool IsCase { get; set; }
        [ValidateNever]
        public string InterviewerId { get; set; }
        [Display(Name = "Lunch Required")]
        public bool? Lunch { get; set; }
        [Display(Name = "Interview Type")]
        public string? InterviewType { get; set; }
        [DefaultValue(false)]
        public bool CheckedIn { get; set; }
    }
}
