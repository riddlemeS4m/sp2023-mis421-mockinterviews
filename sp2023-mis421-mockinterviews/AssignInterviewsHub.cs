using Microsoft.AspNetCore.SignalR;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews
{
    public class AssignInterviewsHub : Hub
    {
        public async Task SendUpdate(InterviewEvent interviewEvent, string studentname, string interviewername, string time, string date)
        {
            await Clients.All.SendAsync("ReceiveInterviewEventUpdate", interviewEvent, studentname, interviewername, time, date);
        }
    }
}
