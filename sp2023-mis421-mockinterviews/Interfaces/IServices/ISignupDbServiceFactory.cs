using sp2023_mis421_mockinterviews.Services.SignupDb;

namespace sp2023_mis421_mockinterviews.Interfaces.IServices
{
    public interface ISignupDbServiceFactory
    {
        public EventService Events { get; }
        public InterviewService Interviews { get; }
        public InterviewerLocationService InterviewerLocations { get; }
        public InterviewerSignupService InterviewerSignups { get; }
        public InterviewerTimeslotService InterviewerTimeslots { get; }
        public SettingsService Settings { get; }
        public TimeslotService Timeslots { get; }
    }
}