using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class SignupInterviewerViewModel
    {
        public InterviewerSignup SignupInterviewer { get; set; }
        public Dictionary<string, string> Resumes { get; set; }
    }
}