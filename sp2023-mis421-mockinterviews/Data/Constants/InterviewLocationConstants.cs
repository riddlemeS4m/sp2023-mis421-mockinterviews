using Microsoft.AspNetCore.Mvc.Rendering;

namespace sp2023_mis421_mockinterviews.Data.Constants
{
    public class InterviewLocationConstants
    {
        public const string InPerson = "In Person";
        public const string IsVirtual = "Virtual";

        public static List<SelectListItem> GetInterviewLocationOptions()
        {
            return new List<SelectListItem>
            {
            new SelectListItem { Text = InPerson, Value = InPerson },
            new SelectListItem { Text = IsVirtual, Value = IsVirtual },
            };
        }
    }
}
