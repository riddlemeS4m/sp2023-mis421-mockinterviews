using Microsoft.AspNetCore.Mvc.Rendering;

namespace sp2023_mis421_mockinterviews.Data.Constants
{
    //does not need to be update-able
    //database can provide meaning through numbers, app can interpret numbers through enum
    public class StatusConstants
    {
        //moving a student's interview to completed or no show status removes it from the list of interviews that are displayed
        public const string Default = "Not Arrived";
        public const string CheckedIn = "Checked In";
        public const string Ongoing = "Ongoing";
        public const string Completed = "Completed";
        public const string NoShow = "No Show";

        public static List<SelectListItem> GetCompleteStatusOptions()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = Default, Value = Default },
                new SelectListItem { Text = CheckedIn, Value = CheckedIn },
                new SelectListItem { Text = Ongoing, Value = Ongoing },
                new SelectListItem { Text = Completed, Value = Completed },
                new SelectListItem { Text = NoShow, Value = NoShow },
            };
        }

        public static List<SelectListItem> GetUnassignedStatusOptions()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = Default, Value = Default },
                new SelectListItem { Text = CheckedIn, Value = CheckedIn },
                new SelectListItem { Text = NoShow, Value = NoShow },
            };

        }
        public static List<SelectListItem> GetInterviewerCompleteStatusOptions()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = Completed, Value = Completed },
                new SelectListItem { Text = NoShow, Value = NoShow },
            };
        }

        public enum Statuses
        {
            Default,
            CheckedIn,
            Ongoing,
            Completed,
            NoShow
        }
    }
}