namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class EventStatisticsViewModel
    {
        public List<ParticipantCountPerDateViewModel> EventStatistics { get; set; }
        public int? TotalStudents { get; set; }
        public int? TotalInterviewers { get; set; }
        public int? TotalVolunteers { get; set; }
    }
}
