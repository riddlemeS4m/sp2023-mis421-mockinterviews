using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Interfaces.IDbContext;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Services.SignupDb
{
    public class InterviewerSignupService : GenericSignupDbService<InterviewerSignup>
    {
        public InterviewerSignupService(ISignupDbContext context) : base(context)
        {
        }

        public async Task<Dictionary<string,string>> GetCheckedInAndAvailableInterviewerIdsWithTypes(IEnumerable<string> busyInterviewers)
        {
            return await _dbSet.Where(x => x.CheckedIn && !busyInterviewers.Contains(x.InterviewerId))
                .Select(x => new {Id = x.InterviewerId, x.Type})
                .ToDictionaryAsync(x => x.Id, x => x.Type);
        }

        ///<summary>
        ///     Parameters: Takes a Dictionary of InterviewId, InterviewerSignupId <br />
        ///     Returns: A Dictionary of InterviewerSignupId, InterviewerSignup.InterviewerId
        ///</summary>
        public async Task<Dictionary<int, string>> GetInterviewerIdsFromInterviews(Dictionary<int, int> interviews)
        {
            
            return await _dbSet.Where(x => interviews.Values.Contains(x.Id))
                .Select(x => new {x.Id, x.InterviewerId})
                .ToDictionaryAsync(x => x.Id, x => x.InterviewerId);
        }
    }
}