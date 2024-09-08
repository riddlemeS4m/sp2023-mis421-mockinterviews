using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Interfaces.IDbContext;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Services.SignupDb
{
    public class InterviewerLocationService : GenericSignupDbService<InterviewerLocation>
    {
        public InterviewerLocationService(ISignupDbContext context) : base(context)
        {
        }

        public async Task<Dictionary<string, string>> GetInterviewersRoomsByIds(IEnumerable<string> userIds)
        {
            var dict = await _dbSet.Where(x => userIds.Contains(x.InterviewerId) && x.Event.Date.Date == DateTime.Now.Date)
                .Select(x => new {Id = x.InterviewerId, x.Location.Room})
                .ToDictionaryAsync(x => x.Id, x => x.Room);

            return dict;
        }
    }
}