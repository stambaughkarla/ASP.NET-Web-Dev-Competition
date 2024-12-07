using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    public class Unavailability
    {
        public int UnavailabilityID { get; set; }

        [Required]
        [Display(Name = "Unavailable Date")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        // Foreign key and navigation property
        [Required]
        [ForeignKey("Property")]
        public int PropertyID { get; set; }
        public virtual Property Property { get; set; }
    }
}