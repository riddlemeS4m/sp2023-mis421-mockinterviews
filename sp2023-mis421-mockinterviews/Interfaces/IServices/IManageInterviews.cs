using sp2023_mis421_mockinterviews.Models.ViewModels;

namespace sp2023_mis421_mockinterviews.Interfaces.IServices
{
    public interface IManageInterviews
    {
        public Task AssignStudentsToInterviewers(Dictionary<int, string> keyValuePairs);
        public Task<List<InterviewEventManageViewModel>> ListOfAssignedStudents();
    }
}