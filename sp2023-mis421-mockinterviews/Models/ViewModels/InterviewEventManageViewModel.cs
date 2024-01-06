﻿using Microsoft.AspNetCore.Mvc.Rendering;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class InterviewEventManageViewModel
    {
        public InterviewEvent InterviewEvent { get; set; }
        public List<SelectListItem> BehavioralInterviewers { get; set; }
        public List<SelectListItem> TechnicalInterviewers { get; set; }
        public List<SelectListItem> RequestedInterviewers { get; set; }
        public string InterviewerId { get; set; }
        public string StudentName { get; set; }
    }
}
