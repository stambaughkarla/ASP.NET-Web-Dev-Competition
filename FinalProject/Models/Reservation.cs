using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models
{
    public class Reservation
    {
        public const Decimal TAX = 0.07m;
        public Int32 ReservationId { get; set; }

        public Int32 PropertyId { get; set; }

        public Property Property { get; set; }
        
        public Int32 CustomerId { get; set; }

        public Customer Customer { get; set; }

        [Required]
        [Display(Name = "CheckIn Date")]
        public DateTime CheckIn { get; set; }
        
        [Required]
        [Display(Name = "CheckOut Date")]
        public DateTime CheckOut { get; set; }
        
        [Required]
        [Display(Name = "Number of Guests")]
        public Int32 NumOfGuests { get; set; }

        [Required]
        [Display(Name = "Weekday Price")]
        public Decimal WeekdayPrice { get; set; }

        [Required]
        [Display(Name = "Weekend Price")]
        public Decimal WeekendPrice { get; set; }

        [Required]
        [Display(Name = "Cleaning Fee")]
        public Decimal CleaningFee { get; set; }

        [Required]
        [Display(Name = "Discount Rate")]
        public Decimal DiscountRate { get; set; }

        [Required]
        [Display(Name = "Confirmation Number")]
        public Int32 ConfirmationNumber { get; set; }

        [Required]
        [Display(Name = "Reserved")]
        public Boolean ReservationStatus { get; set; }
    }
}
