using Microsoft.AspNetCore.Mvc.Rendering;

namespace sp2023_mis421_mockinterviews.Data
{
    public class InterviewTypesConstants
    {
        public const string Behavioral = "Behavioral";
        public const string Technical = "Technical";

        public static List<SelectListItem> GetInterviewTypesOptions()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = Behavioral, Value = Behavioral },
                new SelectListItem { Text = Technical, Value = Technical },
            };
        }
    }
}
