using System.ComponentModel.DataAnnotations;

namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class EventDate
    {
        public int Id { get; set; }
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }
        public string EventName { get; set; }
    }
}
