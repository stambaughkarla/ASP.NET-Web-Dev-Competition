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
        public Property Property { get; set; }

        public int Rating { get; set; }

        public string ReviewText { get; set; }

        public string HostComments { get; set;}

        public bool DisputeStatus { get; set; }




    }
}
