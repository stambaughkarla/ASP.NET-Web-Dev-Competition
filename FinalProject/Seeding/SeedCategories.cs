using FinalProject.Models;
using FinalProject.DAL;

namespace FinalProject.Seeding
{
    public static class SeedCategories
    {
        public static void SeedAllCategories(AppDbContext context)
        {
            if (context.Categories.Any()) return;

            List<Category> categories = new List<Category>
            {
                new Category { CategoryName = "House" },
                new Category { CategoryName = "Cabin" },
                new Category { CategoryName = "Apartment" },
                new Category { CategoryName = "Condo" },
                new Category { CategoryName = "Hotel" }
            };

            context.Categories.AddRange(categories);
            context.SaveChanges();
        }
    }
}
