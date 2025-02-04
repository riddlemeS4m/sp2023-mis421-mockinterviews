using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using SendGrid;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Interfaces.IDbContext;
using sp2023_mis421_mockinterviews.Interfaces.IServices;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using sp2023_mis421_mockinterviews.Models.UserDb;
using sp2023_mis421_mockinterviews.Models.ViewModels;
using sp2023_mis421_mockinterviews.Services.SignalR;
using sp2023_mis421_mockinterviews.Services.SignupDb;
using sp2023_mis421_mockinterviews.Services.UserDb;

namespace sp2023_mis421_mockinterviews.Services.Controllers
{
    public class ManageInterviewsService : IManageInterviews
    {
        private readonly ISignupDbServiceFactory _signupDb;
        private readonly UserService _userService;
        private readonly ISendGridClient _sendGridClient;
        private readonly IHubContext<AssignInterviewsHub> _interviewsHub;
        private readonly IHubContext<AvailableInterviewersHub> _interviewersHub;
        private readonly ILogger<ManageInterviewsService> _logger;

        public ManageInterviewsService(ISignupDbServiceFactory dbServiceFactory,
            UserService userService,
            ISendGridClient sendGridClient,
            IHubContext<AssignInterviewsHub> interviewsHub,
            IHubContext<AvailableInterviewersHub> interviewersHub,
            ILogger<ManageInterviewsService> logger)
        {
            _signupDb = dbServiceFactory;
            _userService = userService;
            _sendGridClient = sendGridClient;
            _interviewsHub = interviewsHub;
            _interviewersHub = interviewersHub;
            _logger = logger;
        }

        public async Task AssignStudentsToInterviewers(Dictionary<int, string> keyValuePairs)
        { 
            var filteredDict = keyValuePairs.Where(x => x.Value != "0").ToDictionary(x => x.Key, x => x.Value);

            var interviews = await _signupDb.Interviews.GetAllActiveInterviewsByIds(keyValuePairs.Keys.ToList());
            var interviewers = await _signupDb.InterviewerTimeslots.GetAllActiveInterviewersByIds(filteredDict.Values.ToList());

            var interviewsToUpdate = new List<Interview>();

            
            foreach(var item in keyValuePairs)
            {
                var interview = interviews.Where(x => x.Id == item.Key).FirstOrDefault();
                
                if(item.Value != "0")
                {
                    var interviewerTimeslot = interviewers.Where(x => x.InterviewerSignup.InterviewerId == item.Value && interview.TimeslotId == x.TimeslotId).FirstOrDefault();

                    if(interviewerTimeslot != null)
                    {
                        interview.InterviewerTimeslot = interviewerTimeslot;
                        interview.InterviewerTimeslotId = interviewerTimeslot.Id;
                        interviewsToUpdate.Add(interview);
                    }
                }
            }

            var studentIds = interviewsToUpdate.Select(x => x.StudentId).ToList();
            var students = await _userService.GetUsersByIds(studentIds);

            if(interviewsToUpdate.Count > 0)
            {
                await _signupDb.Interviews.UpdateRangeAsync(interviewsToUpdate);

                foreach(var interview in interviewsToUpdate)
                {
                    var studentName = students.Where(x => x.Key == interview.StudentId).FirstOrDefault().Value.GetFullName();
                    var studentClass = students.Where(x => x.Key == interview.StudentId).FirstOrDefault().Value.GetClass();
                    await _interviewsHub.Clients.All.SendAsync("ReceiveInterviewEventUpdate", interview, studentName, studentClass, interview.InterviewerTimeslot.InterviewerSignup.InterviewerId, interview.InterviewerTimeslot.InterviewerSignup.GetInterviewerName(), interview.Timeslot.Time, interview.Timeslot.Event.Date);
                }
            }
        }

        public async Task<List<InterviewEventManageViewModel>> ListOfAssignedStudents()
        {
            var allInterviewers = (await _signupDb.InterviewerTimeslots.GetAllActiveInterviewers()).ToList();
            var allInterviews = (await _signupDb.Interviews.GetAllActiveInterviews()).ToList();

            var studentIds = allInterviews.Select(x => x.StudentId).ToList();
            var students = await _userService.GetUsersByIds(studentIds);

            var preassignments = new List<InterviewEventManageViewModel>();
            foreach(var interview in allInterviews)
            {
                var student = students.Where(x => x.Key == interview.StudentId).FirstOrDefault();
                var defaultItem = new SelectListItem {
                    Value = "0",
                    Text = "--Unassigned--"
                };

                var firstItem = new SelectListItem();

                if(interview.InterviewerTimeslotId != null && interview.InterviewerTimeslotId != 0)
                {
                    firstItem.Text = interview.InterviewerTimeslot.InterviewerSignup.GetInterviewerName();
                    firstItem.Value = interview.InterviewerTimeslot.InterviewerSignup.InterviewerId.ToString();
                }

                var availableInterviewers = allInterviewers
                    .Where(x => x.Timeslot.Event.Date == interview.Timeslot.Event.Date
                        && x.TimeslotId == interview.TimeslotId) 
                    .Select(x => new SelectListItem {
                        Text = x.InterviewerSignup.GetInterviewerName(),
                        Value = x.InterviewerSignup.InterviewerId
                    })
                    .ToList();
                
                if(availableInterviewers.Count == 0)
                {
                    preassignments.Add(new () {
                        InterviewEvent = interview,
                        RequestedInterviewers = new List<SelectListItem> {
                            new() {
                                Value = "0",
                                Text = "--None Available--"
                            }
                        },
                        StudentName = student.Value.GetFullName(),
                        StudentClass = student.Value.GetClass()
                    });
                }
                else 
                {
                    availableInterviewers = availableInterviewers.OrderBy(x => x.Text).ToList();

                    availableInterviewers.Insert(0, defaultItem);

                    if(!string.IsNullOrEmpty(firstItem.Value))
                    {
                        var interviewer = availableInterviewers.Where(x => x.Value == firstItem.Value).FirstOrDefault();
                        if(interviewer != null)
                        {
                            availableInterviewers.Remove(interviewer);
                        }

                        availableInterviewers.Insert(0, firstItem);
                    }                    

                    preassignments.Add(new () {
                        InterviewEvent = interview,
                        RequestedInterviewers = availableInterviewers,
                        StudentName = student.Value.GetFullName(),
                        StudentClass = student.Value.GetClass()
                    });
                }
            }

            return preassignments;
        }
    }
}