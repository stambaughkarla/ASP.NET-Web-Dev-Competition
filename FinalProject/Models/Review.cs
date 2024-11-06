using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

namespace FinalProject.Models
{
    public class Review
    {
        // PK
        [Key]
        public int ReviewId { get; set; }

        // Navigational Property - Foreign Key for Customer ID
        [Required]
        public Customer Customer { get; set; }

        // Navigational Property - Foreign Key for Property ID
        [Required]
        public Property Property { get; set; }

        // Rating, RANGE??
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        // Review Text range??
        [Display(Name = "Host Comments")]
        [StringLength(1000, ErrorMessage = "Review Text cannot exceed 1000 characters.")]
        public string ReviewText { get; set; }

        // Host Comments range??
        [StringLength(500, ErrorMessage = "Host Comments cannot exceed 500 characters.")]
        public string HostComments { get; set; }

        // Dispute Status with required validation
        [Required]
        public bool DisputeStatus { get; set; }
    }
}
