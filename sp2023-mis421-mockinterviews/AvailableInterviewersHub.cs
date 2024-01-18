using Microsoft.AspNetCore.SignalR;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using sp2023_mis421_mockinterviews.Models.ViewModels;

namespace sp2023_mis421_mockinterviews
{
    public class AvailableInterviewersHub : Hub
    {
        public async Task SendUpdate(List<AvailableInterviewer> interviewers)
        {
            await Clients.All.SendAsync("ReceiveAvailableInterviewersUpdate", interviewers);
        }
    }
}
