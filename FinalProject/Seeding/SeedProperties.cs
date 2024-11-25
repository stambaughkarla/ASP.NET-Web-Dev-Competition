using FinalProject.Models;
using FinalProject.DAL;

namespace FinalProject.Seeding
{
    public static class SeedProperties
    {
        public static void SeedAllProperties(AppDbContext context)
        {
            if (context.Properties.Any()) return;

            List<Property> AllProperties = new List<Property>
            {
                new Property
                {
                    PropertyNumber = 3001,
                    Street = "8714 Mann Plaza",
                    City = "Lisaside",
                    State = "PA",
                    Zip = "72227",
                    Bedrooms = 5,
                    Bathrooms = 6,
                    GuestsAllowed = 9,
                    PetsAllowed = false,
                    FreeParking = false,
                    WeekdayPrice = 152.68m,
                    WeekendPrice = 171.57m,
                    CleaningFee = 8.88m,
                    AdminApproved = true,
                    CategoryID = 1,
                    UnavailableDates = "2024-12-24,2024-12-25,2024-12-26", 
                    PropertyStatus = true
                },
                new Property
                {
                    PropertyNumber = 3002,
                    Street = "96593 White View Apt. 094",
                    City = "Jonesberg",
                    State = "FL",
                    Zip = "5565",
                    Bedrooms = 7,
                    Bathrooms = 8,
                    GuestsAllowed = 8,
                    PetsAllowed = false,
                    FreeParking = true,
                    WeekdayPrice = 120.81m,
                    WeekendPrice = 148.15m,
                    CleaningFee = 8.02m,
                    AdminApproved = true,
                    CategoryID = 3,
                    UnavailableDates = "", // No unavailable dates
                    PropertyStatus = true
                },
                new Property
                {
                    PropertyNumber = 3003,
                    Street = "848 Melissa Springs Suite 947",
                    City = "Kellerstad",
                    State = "MD",
                    Zip = "80819",
                    Bedrooms = 5,
                    Bathrooms = 7,
                    GuestsAllowed = 8,
                    PetsAllowed = false,
                    FreeParking = true,
                    WeekdayPrice = 127.96m,
                    WeekendPrice = 132.99m,
                    CleaningFee = 13.37m,
                    AdminApproved = false,
                    CategoryID = 4,
                    UnavailableDates = "", // No unavailable dates
                    PropertyStatus = true
                }
            };

            context.Properties.AddRange(AllProperties);
            context.SaveChanges();
        }
    }
}