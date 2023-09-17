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
        public bool For221 { get; set; }
        [DefaultValue(true)]
        public bool IsActive { get; set; }
    }
}
