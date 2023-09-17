using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class MSTeamsStudentUpload
    {
        public int Id { get; set; }
        public string? MicrosoftId { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        [DefaultValue(false)]
        [Display(Name="In 221?")]
        public bool In221 { get; set; }
        [DefaultValue(false)]
        [Display(Name = "In MSMIS?")]
        public bool InMasters { get; set; }
    }
}
