﻿
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class LocationInterviewerViewModel
    {
        public List<LocationInterviewerWithName> LocationInterviewerWithNames { get; set; }
        public List<Location> Locations { get; set; }
    }
}
