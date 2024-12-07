using FinalProject.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.DAL
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        // Ensure you pass options to the base class constructor
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Your DbSet properties
        public DbSet<Property> Properties { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Review> Reviews { get; set; }
        //public DbSet<ReviewDispute> ReviewDisputes { get; set; }
        public DbSet<Unavailability> Unavailabilities { get; set; }
    }
}
