using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models
{
    public class Category
    {
        public Int32 CategoryID { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [Display(Name = "Category Name")]
        public String CategoryName { get; set; }

        //navigational property
        public List<Property> Properties { get; set; }

        public Category()
        {
            if (Properties == null)
            {
                Properties = new List<Property>();
            }
        }
    }
}