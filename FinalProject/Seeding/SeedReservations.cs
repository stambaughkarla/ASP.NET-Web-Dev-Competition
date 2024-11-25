using FinalProject.Models;
using FinalProject.DAL;

namespace FinalProject.Seeding
{
    public static class SeedReservations
    {
        public static void SeedAllReservations(AppDbContext context)
        {
            if (context.Reservations.Any()) return;

            // Get customer IDs
            var customer1 = context.Users.FirstOrDefault(u => u.Email == "sheff44@puppy.com");
            var customer2 = context.Users.FirstOrDefault(u => u.Email == "cbaker@freezing.co.uk");

            if (customer1 == null || customer2 == null)
            {
                throw new Exception("Please seed users before seeding reservations.");
            }

            // Get properties
            var property1 = context.Properties.FirstOrDefault(p => p.PropertyNumber == 3001);
            var property2 = context.Properties.FirstOrDefault(p => p.PropertyNumber == 3002);
            var property3 = context.Properties.FirstOrDefault(p => p.PropertyNumber == 3003);

            if (property1 == null || property2 == null || property3 == null)
            {
                throw new Exception("Please seed properties before seeding reservations.");
            }

            List<Reservation> AllReservations = new List<Reservation>
            {
                new Reservation
                {
                    PropertyID = property1.PropertyID,
                    CustomerID = customer2.Id,  // Using cbaker as the customer
                    CheckIn = new DateTime(2024, 11, 1),
                    CheckOut = new DateTime(2024, 11, 3),
                    NumOfGuests = 1,
                    WeekdayPrice = 134.72m,
                    WeekendPrice = 249.39m,
                    CleaningFee = 19.19m,
                    DiscountRate = 0m,
                    ConfirmationNumber = 21900,
                    ReservationStatus = true
                },
                new Reservation
                {
                    PropertyID = property2.PropertyID,
                    CustomerID = customer2.Id,  // Using cbaker as the customer
                    CheckIn = new DateTime(2024, 11, 4),
                    CheckOut = new DateTime(2024, 11, 6),
                    NumOfGuests = 2,
                    WeekdayPrice = 204.67m,
                    WeekendPrice = 207.51m,
                    CleaningFee = 26.36m,
                    DiscountRate = 0.217m,
                    ConfirmationNumber = 21901,
                    ReservationStatus = true
                },
                new Reservation
                {
                    PropertyID = property3.PropertyID,
                    CustomerID = customer2.Id,  // Using cbaker as the customer
                    CheckIn = new DateTime(2024, 11, 5),
                    CheckOut = new DateTime(2024, 11, 10),
                    NumOfGuests = 14,
                    WeekdayPrice = 163.30m,
                    WeekendPrice = 262.77m,
                    CleaningFee = 13.74m,
                    DiscountRate = 0.158m,
                    ConfirmationNumber = 21901,
                    ReservationStatus = false // Canceled
                }
            };

            context.Reservations.AddRange(AllReservations);
            context.SaveChanges();
        }
    }
}