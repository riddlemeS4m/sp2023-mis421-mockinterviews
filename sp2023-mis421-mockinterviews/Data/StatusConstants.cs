using Microsoft.AspNetCore.Mvc.Rendering;

namespace sp2023_mis421_mockinterviews.Data
{
    public class StatusConstants
    {
        public const string Default = "Not Arrived";
        public const string CheckedIn = "Checked In";
        public const string Ongoing = "Ongoing";
        public const string Completed = "Completed";
        public const string NoShow = "No Show";

        public static List<SelectListItem> GetStatusOptions()
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
    }
}
