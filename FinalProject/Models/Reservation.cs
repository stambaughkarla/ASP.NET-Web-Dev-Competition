using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    public class Reservation
    {
        public decimal TAX { get; internal set; } = 0.07m;
        public int ReservationID { get; set; }

        // Explicitly define the foreign key for Property
        [Required]
        [ForeignKey("Property")]
        public int PropertyID { get; set; }
        public Property Property { get; set; }

        public AppUser Customer { get; set; }

        [Required]
        [Display(Name = "CheckIn Date")]
        public DateTime CheckIn { get; set; }

        [Required]
        [Display(Name = "CheckOut Date")]
        public DateTime CheckOut { get; set; }

        [Required]
        [Display(Name = "Number of Guests")]
        public int NumOfGuests { get; set; }

        [Required]
        [Display(Name = "Weekday Price")]
        public decimal WeekdayPrice { get; set; }

        [Required]
        [Display(Name = "Weekend Price")]
        public decimal WeekendPrice { get; set; }

        [Required]
        [Display(Name = "Cleaning Fee")]
        public decimal CleaningFee { get; set; }

        [Required]
        [Display(Name = "Discount Rate")]
        public decimal DiscountRate { get; set; }

        [Required]
        [Display(Name = "Confirmation Number")]
        public int ConfirmationNumber { get; set; }

        [Required]
        [Display(Name = "Reserved")]
        public bool ReservationStatus { get; set; }
    }
}
