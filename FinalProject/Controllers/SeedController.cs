using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using FinalProject.Models;
using FinalProject.Utilities;
using FinalProject.DAL;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class SeedController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IWebHostEnvironment _environment;
    private readonly AppDbContext _context;

    public SeedController(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IWebHostEnvironment environment,
        AppDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _environment = environment;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    // Action method to trigger the user seeding
    [HttpPost]
    public async Task<IActionResult> SeedUsersFromExcel()
    {
        // Ensure roles exist
        await EnsureRolesAsync(_roleManager);

        // Get the file path of the Excel file located in wwwroot/Data/
        string filePath = Path.Combine(_environment.WebRootPath, "Data", "SeedingDataBevoBnB.xlsx");

        // Create an instance of ExcelHelper
        var excelHelper = new ExcelHelper(_environment, _context);

        // Read the data from the Excel file
        var addUserModels = excelHelper.ReadAdminDataFromExcel(filePath);

        // Add users to the database
        foreach (var model in addUserModels)
        {
            var result = await AddUser.AddUserWithRoleAsync(model, _userManager, _context);

            if (result.Succeeded)
            {
                Console.WriteLine($"User {model.User.FirstName} {model.User.LastName} added successfully.");
            }
            else
            {
                Console.WriteLine($"Failed to add {model.User.FirstName} {model.User.LastName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        // Optionally, you can show a success message on the page
        TempData["Message"] = "Users have been successfully seeded!";
        return View("Confirm");
    }

    // Ensure roles exist
    private async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roleNames = { "Admin", "Customer", "Host" };  // Updated roles based on project requirements
        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }
}