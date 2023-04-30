using sp2023_mis421_mockinterviews.Interfaces;

namespace sp2023_mis421_mockinterviews.Data.Access
{
    public class StudentSignupEmail : ASendAnEmail
    {

        public StudentSignupEmail()
        {
            FilePath = "./Content/student-signup-email.html";
        }

        public override void InjectHTMLContent()
        {
            HTMLContent = HTMLContent.Replace("{firstName}", ToName);
            HTMLContent = HTMLContent.Replace("{interviewList}", Times);
        }
    }
}
