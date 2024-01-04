using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Interfaces;

namespace sp2023_mis421_mockinterviews.Data.Access.Emails
{
    public class VolunteerCancellationNotification : ASendAnEmail
    {
        public VolunteerCancellationNotification()
        {
            FilePath += "volunteer-cancellation-notification.html";
        }
        public override void InjectHTMLContent()
        {
            HTMLContent = HTMLContent.Replace("{adminName}", SuperUser.FirstName);
            HTMLContent = HTMLContent.Replace("{name}", ToName);
            HTMLContent = HTMLContent.Replace("{times}", Times);
        }
    }
}
