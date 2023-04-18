using Microsoft.AspNetCore.Mvc.Rendering;

namespace sp2023_mis421_mockinterviews.Data
{
    public class InterviewLocationConstants
    {
        public const string InPerson = "In Person";
        public const string Virtual = "Virtual";

        public static List<SelectListItem> GetInterviewLocationOptions()
        {
            return new List<SelectListItem>
            {
            new SelectListItem { Text = InPerson, Value = InPerson },
            new SelectListItem { Text = Virtual, Value = Virtual },
            };
        }
    }
}
