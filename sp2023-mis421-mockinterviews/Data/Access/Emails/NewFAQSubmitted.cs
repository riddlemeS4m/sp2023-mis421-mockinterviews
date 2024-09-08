using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Data.Seeds;
using sp2023_mis421_mockinterviews.Interfaces.IServices;

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
            HTMLContent = HTMLContent.Replace("{adminName}", SuperUser.FirstName);
            HTMLContent = HTMLContent.Replace("{name}", ToName);
            HTMLContent = HTMLContent.Replace("{question}", Times);
        }
    }
}
