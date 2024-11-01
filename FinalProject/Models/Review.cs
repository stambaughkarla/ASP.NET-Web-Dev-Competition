using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

namespace FinalProject.Models
{
    public class Review
    {
        //Primary Key for Review Class

        public int ReviewId { get; set; }



        //Navigationl Property FK for Customer ID
        public Customer Customer { get; set; }

        //Navigationl Property FK for Property ID
        [Required]
        public Property Property { get; set; }

        [Required]
        public int Rating { get; set; }

        public string ReviewText { get; set; }

        public string HostComments { get; set;}

        [Required]
        public bool DisputeStatus { get; set; }




    }
}
