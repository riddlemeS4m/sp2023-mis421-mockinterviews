using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class Timeslot
    {
        //all static entries
        //do we need isactive field?

        //LT-yes because we want the dependencies to stay around even after a week of mock interviews is over, that way we can go back in the history and see previous stats
        public int Id { get; set; }
        [DataType(DataType.Time)]
        [DisplayFormat(DataFormatString = "{0:hh:mm tt}")]
        public DateTime Time { get; set; }
        [ForeignKey("EventDate")]
        public int EventDateId { get; set; }
        [ValidateNever]
        public EventDate EventDate { get; set; }
        //should delete this
        [DefaultValue(true)]
        public bool IsActive { get; set; }
        public bool IsVolunteer { get; set; }
        public bool IsInterviewer { get; set; }
        public bool IsStudent { get; set; }
        [Display(Name = "Max Sign Ups")]
        public int MaxSignUps { get; set; }
    }
}
