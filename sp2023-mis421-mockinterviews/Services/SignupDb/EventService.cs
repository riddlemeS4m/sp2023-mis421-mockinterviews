using sp2023_mis421_mockinterviews.Interfaces.IDbContext;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Services.SignupDb
{
    public class EventService : GenericSignupDbService<Event>
    {
        public EventService(ISignupDbContext context) : base(context)
        {
        }
    }
}