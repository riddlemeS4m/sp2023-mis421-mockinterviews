using System.ComponentModel.DataAnnotations;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class UploadSiteContentViewModel
    {
        [Display(Name = "Mock Interview Manual")]
        public byte[]? Manual { get; set; }

        [Display(Name = "Guest Parking Pass")]
        public byte[]? ParkingPass { get; set; }
    }
}
