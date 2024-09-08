using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Interfaces.IDbContext;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Services.SignupDb
{
    public class InterviewService : GenericSignupDbService<Interview>
    {
        public InterviewService(ISignupDbContext context) : base(context)
        {
        }

        public async Task<Interview> GetByIdAsync(int id)
        {
            return await _dbSet.Include(x => x.InterviewerTimeslot)
                .ThenInclude(x => x.InterviewerSignup)
                .Include(x => x.Timeslot)
                .ThenInclude(x => x.Event)
                .Include(x => x.Location)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<string>> GetActivelyInterviewingInterviewerIds()
        {
            return await _dbSet.Include(x => x.InterviewerTimeslot)
                .ThenInclude(x => x.InterviewerSignup)
                .Where(x => x.Status == StatusConstants.Ongoing)
                .Select(x => x.InterviewerTimeslot.InterviewerSignup.InterviewerId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<IEnumerable<Interview>> GetTopNonCompletedInterviews(int numberOfInterviews)
        {
            return await _dbSet.Include(i => i.Location)
                .Include(i => i.InterviewerTimeslot)
                .ThenInclude(i => i.InterviewerSignup)
                .Include(i => i.Timeslot)
                .ThenInclude(j => j.Event)
                .Where(i => i.Status != StatusConstants.Completed &&
                    i.Status != StatusConstants.NoShow &&
                    i.Timeslot.Event.IsActive)
                .OrderBy(i => i.TimeslotId)
                .Take(numberOfInterviews)
                .ToListAsync();
        }

        public async Task<IEnumerable<Interview>> GetActiveInterviewsForOneStudent(string userId)
        {
            return await _dbSet.Include(x => x.Timeslot)
                .ThenInclude(x => x.Event)
                .Where(x => x.StudentId == userId
                    && x.Timeslot.Event.IsActive)
                .ToListAsync();
        }

        public Dictionary<int, string> GetStudentIdsFromInterviews(IEnumerable<Interview> interviews)
        {
            return interviews
                .Select(x => new {x.Id, x.StudentId})
                .ToDictionary(x => x.Id, x => x.StudentId);
        }

        public Dictionary<int, int> GetInterviewSignupIdsFromInterviews(IEnumerable<Interview> interviews)
        {
            return interviews
                .Select(x => new {x.Id, x.InterviewerTimeslot.InterviewerSignupId})
                .ToDictionary(x => x.Id, x => x.InterviewerSignupId);
        }
    }
}