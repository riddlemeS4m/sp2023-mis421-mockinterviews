﻿using Microsoft.AspNetCore.Mvc.Rendering;

namespace sp2023_mis421_mockinterviews.Data.Constants
{
    //does not need to be update-able
    //database can provide meaning through numbers, app can interpret numbers through enum
    public class For221Constants
    {
        public const string b = "221 and above";
        public const string n = "321 and above";
        public const string y = "Only 221";

        public static List<SelectListItem> GetFor221Options()
        {
            return new List<SelectListItem>
            {
                new() { Text = b, Value = b },
                new() { Text = n, Value = n },
                new() { Text = y, Value = y },
            };
        }

        public static string GetFor221Text(For221? for221)
        {
            return for221 switch
            {
                For221.b => b,
                For221.n => n,
                For221.y => y,
                _ => string.Empty,
            };
        }
    }
    public enum For221
    {
        b,
        n,
        y
    }
}
