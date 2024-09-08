using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Interfaces.IDbContext;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using sp2023_mis421_mockinterviews.Models.ViewModels;

namespace sp2023_mis421_mockinterviews.Services.SignupDb
{
    public class InterviewerTimeslotService : GenericSignupDbService<InterviewerTimeslot>
    {
        public InterviewerTimeslotService(ISignupDbContext context) : base(context)
        {
        }
    }
}