using Microsoft.AspNetCore.SignalR;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Services.SignalR
{
    public class AssignInterviewsHub : Hub
    {
        public async Task SendUpdate(Interview interviewEvent, string studentname, string interviewername, string time, string date)
        {
            await Clients.All.SendAsync("ReceiveInterviewEventUpdate", interviewEvent, studentname, interviewername, time, date);
        }
    }
}
