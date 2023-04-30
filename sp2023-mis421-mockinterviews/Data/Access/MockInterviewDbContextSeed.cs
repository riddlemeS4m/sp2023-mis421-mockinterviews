using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Data.Access
{
    public class MockInterviewDbContextSeed
    {
        public static async Task SeedTimeslots(MockInterviewDataDbContext context)
        {
            var dates = await context.EventDate.ToListAsync();

            if (dates.Count != 0)
            {
                var times = await context.Timeslot.ToListAsync();
                var timeslots = TimeslotSeed.SeedTimeslots(dates);

                foreach (Timeslot timeslot in timeslots)
                {
                    if (!times.Any(x => x.Time == timeslot.Time && x.EventDate.Date == timeslot.EventDate.Date))
                    {
                        context.Add(timeslot);
                        await context.SaveChangesAsync();
                    }
                }
            }
        }
    }
}
