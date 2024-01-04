using Microsoft.AspNetCore.Mvc.Rendering;

namespace sp2023_mis421_mockinterviews.Data.Constants
{
    //does not need to be update-able
    //database can provide meaning through numbers, app can interpret numbers through enum
    public class InterviewLocationConstants
    {
        public const string InPerson = "In Person";
        public const string IsVirtual = "Virtual";

        public static List<SelectListItem> GetLocationOptions()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = InPerson, Value = InPerson },
                new SelectListItem { Text = IsVirtual, Value = IsVirtual },
            };
        }
    }

    public enum InterviewLocations
    {
        InPerson,
        IsVirtual,
    }
}
