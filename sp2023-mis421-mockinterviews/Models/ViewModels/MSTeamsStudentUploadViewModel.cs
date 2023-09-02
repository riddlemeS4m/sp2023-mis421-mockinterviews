using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class MSTeamsStudentUploadViewModel
    {
        [Display(Name = "Roster Data")]
        public IFormFile? RosterData { get; set; }
    }
}
