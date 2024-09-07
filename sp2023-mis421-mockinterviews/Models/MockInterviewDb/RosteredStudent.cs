﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    [Table("Roster")]
    public class RosteredStudent
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Microsoft ID")]
        public string? MicrosoftId { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [DefaultValue(false)]
        [Display(Name = "In 221?")]
        public bool In221 { get; set; }

        [Required]
        [DefaultValue(false)]
        [Display(Name = "In MSMIS?")]
        public bool InMasters { get; set; }

        public override string ToString()
        {
            return $"[Roster] Id: {Id}, Microsoft Id: {MicrosoftId}, Email: {Email}, Name: {Name}";
        }
    }
}
