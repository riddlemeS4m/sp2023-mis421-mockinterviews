using System.ComponentModel.DataAnnotations;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class TimeRangeViewModel
    {
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }
        public string? Location { get; set; }
        public List<int> TimeslotIds { get; set; }

    }
}
