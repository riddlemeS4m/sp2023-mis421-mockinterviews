using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using sp2023_mis421_mockinterviews.Data.Constants;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class EventDate
    {
        public int Id { get; set; }
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }
        [Display(Name = "Event Name")]
        public string EventName { get; set; }
        [Display(Name = "For MIS 221?")]
        [DefaultValue(For221Constants.ForAllMIS)]
        [ValidateNever]
        public string For221 { get; set; }
        [Display(Name = "Deactivate?")]
        [DefaultValue(true)]
        public bool IsActive { get; set; }
    }
}
