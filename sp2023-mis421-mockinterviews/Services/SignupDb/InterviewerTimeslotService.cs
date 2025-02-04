using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Interfaces.IDbContext;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using sp2023_mis421_mockinterviews.Models.ViewModels;

namespace sp2023_mis421_mockinterviews.Services.SignupDb
{
    public class InterviewerTimeslotService : GenericSignupDbService<InterviewerTimeslot>
    {
        private readonly ILogger<InterviewerTimeslotService> _logger;
        public InterviewerTimeslotService(ISignupDbContext context, ILogger<InterviewerTimeslotService> logger) : base(context)
        {
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all active interviewer timeslots, including related interviewer signups 
        /// and associated event details, filtering for active events.
        /// </summary>
        /// <returns>A collection of active <see cref="InterviewerTimeslot"/> entities.</returns>
        public async Task<IEnumerable<InterviewerTimeslot>> GetAllActiveInterviewers()
        {
            var allInterviewers = await _dbSet
                .Include(s => s.InterviewerSignup)
                .Include(s => s.Timeslot)
                .ThenInclude(s => s.Event)
                .Where(s => s.Timeslot.Event.IsActive)
                .Distinct()
                .ToListAsync();

            return allInterviewers;
        }

        public async Task<IEnumerable<InterviewerTimeslot>> GetAllActiveInterviewersByIds(List<string> ids)
        {
            var allInterviewers = await _dbSet
                .Include(s => s.InterviewerSignup)
                .Include(s => s.Timeslot)
                .ThenInclude(s => s.Event)
                .Where(s => s.Timeslot.Event.IsActive
                    && ids.Contains(s.InterviewerSignup.InterviewerId))
                .Distinct()
                .ToListAsync();

            return allInterviewers;
        }
    }
}