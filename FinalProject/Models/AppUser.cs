using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;



namespace FinalProject.Models
{
    /// <summary>
    /// Represents a user in the system. Inherits from IdentityUser to support authentication.
    /// Users can be Customers, Hosts, or Administrators.
    /// </summary>
    public class AppUser : IdentityUser
    {
        // Basic user information - required for all user types
        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        public String FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        public String LastName { get; set; }

        [Required(ErrorMessage = "Birthday is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Birthday")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime Birthday { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [Display(Name = "Address")]
        public String Address { get; set; }

        // Computed property for displaying full name
        [Display(Name = "Full Name")]
        public String FullName
        {
            get { return FirstName + " " + LastName; }
        }

        // Admin-specific property to track employment status
        [Display(Name = "Employment Status")]
        public Boolean? HireStatus { get; set; }

        // Navigational Properties
        // These properties represent relationships to other entities in the database
        // They allow us to "navigate" from one entity to related entities

        // If user is a Host: Contains all properties owned by this host
        public List<Property> Properties { get; set; }

        // If user is a Customer: Contains all reservations made by this customer
        public List<Reservation> Reservations { get; set; }

        // If user is a Customer: Contains all reviews written by this customer
        public List<Review> Reviews { get; set; }

        /// <summary>
        /// Constructor to initialize collections to prevent null reference exceptions
        /// when accessing these properties
        /// </summary>
        public AppUser()
        {
            // Initialize lists to empty collections instead of null
            // This prevents null reference exceptions when adding to or accessing these lists
            Properties = new List<Property>();
            Reservations = new List<Reservation>();
            Reviews = new List<Review>();
        }
    }
}