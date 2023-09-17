using Microsoft.AspNetCore.Mvc.Rendering;

namespace sp2023_mis421_mockinterviews.Data.Constants
{
    public class ClassConstants
    {
        public const string PreMIS = "Pre-MIS";
        public const string FirstSemester = "1st Semester (MIS 221)";
        public const string SecondSemester = "2nd Semester (MIS 321 / MIS 330)";
        public const string ThirdSemester = "3rd Semester (MIS 405 / MIS 430)";
        public const string FourthSemesterOn = "4th Semester + (Capstone / AMP)";
        public const string Cybersecurity = "Cybersecurity";
        //public const string MBA = "Stem to MBA/Traditional MBA";

        public static List<SelectListItem> GetClassOptions()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = PreMIS, Value = PreMIS },
                new SelectListItem { Text = FirstSemester, Value = FirstSemester },
                new SelectListItem { Text = SecondSemester, Value = SecondSemester },
                new SelectListItem { Text = ThirdSemester, Value = ThirdSemester },
                new SelectListItem { Text = FourthSemesterOn, Value = FourthSemesterOn },
                //new SelectListItem { Text = Cybersecurity, Value = Cybersecurity }
                //new SelectListItem { Text = MBA, Value = MBA }
            };
        }
    }
}
