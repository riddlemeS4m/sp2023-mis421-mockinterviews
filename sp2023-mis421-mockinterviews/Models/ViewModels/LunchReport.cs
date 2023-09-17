using System.ComponentModel.DataAnnotations;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class LunchReport
    {
        public string Name { get; set; }
        [Display(Name = "Wants Lunch?")]
        public bool LunchDesire { get; set; }
        [Display(Name = "For Date")]
        [DataType(DataType.Date)]
        public DateTime ForDate { get; set; }
    }
}
