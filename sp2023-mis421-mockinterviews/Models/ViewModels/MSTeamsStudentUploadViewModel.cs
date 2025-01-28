using System.ComponentModel.DataAnnotations;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class MSTeamsStudentUploadViewModel
    {
        [Display(Name = "RosteredStudent Data")]
        public IFormFile? RosterData { get; set; }
    }
}
