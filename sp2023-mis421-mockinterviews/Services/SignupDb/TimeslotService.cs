using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Interfaces.IDbContext;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Services.SignupDb
{
    public class TimeslotService : GenericSignupDbService<Timeslot>
    {
        public TimeslotService(ISignupDbContext context) : base(context)
        {
        }

        /// <summary>
        ///     Returns enumerable list of timeslots that are not labeled with the specified For221 state.
        /// </summary>
        public async Task<IEnumerable<Timeslot>> GetAvailableStudentsTimeslotsByClass(For221 for221)
        {
            return await _dbSet
                .Where(x => x.IsStudent)
                .Include(y => y.Event)
                .Where(x => _context.Interviews.Count(y => y.TimeslotId == x.Id) < x.MaxSignUps)
                .Where(x => x.Event.For221 != for221
                    && x.Event.IsActive)
                .ToListAsync();
        }
    }
}