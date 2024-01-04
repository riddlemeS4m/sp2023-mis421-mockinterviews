using Microsoft.AspNetCore.Mvc.Rendering;

namespace sp2023_mis421_mockinterviews.Data.Constants
{
    //does not need to be update-able
    //database can provide meaning through numbers, app can interpret numbers through enum
    public class InterviewTypeConstants
    {
        public const string Behavioral = "Behavioral";
        public const string Technical = "Technical";
        public const string Case = "Case";

        public static List<SelectListItem> GetInterviewTypesOptions()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = Behavioral, Value = Behavioral },
                new SelectListItem { Text = Technical, Value = Technical },
                new SelectListItem { Text = Case, Value = Case }
            };
        }
    }

    public enum InterviewTypes
    {
        Behavioral,
        Technical,
        Case
    }
}
