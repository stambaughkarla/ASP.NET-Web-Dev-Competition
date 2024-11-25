using FinalProject.Models;
using FinalProject.DAL;

namespace FinalProject.Seeding
{
    public static class SeedReviews
    {
        public static void SeedAllReviews(AppDbContext context)
        {
            if (context.Reviews.Any()) return;

            // Get the customer's actual Id from AspNetUsers
            var customerEmail = "cbaker@freezing.co.uk";
            var customer = context.Users.FirstOrDefault(u => u.Email == customerEmail);

            if (customer == null)
            {
                throw new Exception("Please seed users before seeding reviews.");
            }

            // Use the actual Id from Identity
            string customerId = customer.Id;

            List<Review> AllReviews = new List<Review>
            {
                new Review
                {
                    PropertyID = context.Properties.First(p => p.PropertyNumber == 3001).PropertyID,
                    CustomerID = customerId,  // Using the actual Identity Id
                    Rating = 4,
                    ReviewText = "Great stay!",
                    HostComments = "",
                    DisputeStatus = false,
                    DisputeDescription = ""
                },
                new Review
                {
                    PropertyID = context.Properties.First(p => p.PropertyNumber == 3002).PropertyID,
                    CustomerID = customerId,  // Using the actual Identity Id
                    Rating = 3,
                    ReviewText = "Average experience.",
                    HostComments = "Guest was polite and clean.",
                    DisputeStatus = true,
                    DisputeDescription = "Review was written without completing stay."
                },
                new Review
                {
                    PropertyID = context.Properties.First(p => p.PropertyNumber == 3003).PropertyID,
                    CustomerID = customerId,  // Using the actual Identity Id
                    Rating = 5,
                    ReviewText = "Amazing property, would stay again!",
                    HostComments = "Thank you for being a great guest!",
                    DisputeStatus = false,
                    DisputeDescription = ""
                }
            };

            context.Reviews.AddRange(AllReviews);
            context.SaveChanges();
        }
    }
}