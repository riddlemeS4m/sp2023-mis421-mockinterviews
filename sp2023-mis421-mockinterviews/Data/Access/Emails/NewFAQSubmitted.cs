using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Interfaces;

namespace sp2023_mis421_mockinterviews.Data.Access.Emails
{
    public class NewFAQSubmitted : ASendAnEmail
    {
        public NewFAQSubmitted()
        {
            FilePath += "new-faq-submitted.html";
        }
        public override void InjectHTMLContent()
        {
            HTMLContent = HTMLContent.Replace("{adminName}", CurrentAdmin.FirstName);
            HTMLContent = HTMLContent.Replace("{name}", ToName);
            HTMLContent = HTMLContent.Replace("{question}", Times);
        }
    }
}
