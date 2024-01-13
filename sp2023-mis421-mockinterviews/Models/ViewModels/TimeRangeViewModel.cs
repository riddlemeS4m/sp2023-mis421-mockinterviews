﻿using System.ComponentModel.DataAnnotations;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class TimeRangeViewModel
    {
        [Display(Name = "Start Time")]
        public string StartTime { get; set; }
        [Display(Name = "End Time")]
        public string EndTime { get; set; }
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }
        public string? Location { get; set; }
        public string? Name { get; set; }
        [Display(Name = "Interview Type")]
        public string? InterviewType { get; set; }
        public List<int> TimeslotIds { get; set; }
        [Display(Name = "Lunch")]
        public bool? WantsLunch { get; set; }
        public int? SignupInterviewerId { get; set; }

    }
}
