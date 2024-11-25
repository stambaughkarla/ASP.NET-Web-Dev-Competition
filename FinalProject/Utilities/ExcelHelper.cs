using OfficeOpenXml;
using FinalProject.Models;
using System.Collections.Generic;
using System.IO;
using FinalProject.Utilities;
using Microsoft.AspNetCore.Identity;

public class ExcelHelper

{

    private readonly IWebHostEnvironment _environment;

    // Constructor with IWebHostEnvironment injected
    public ExcelHelper(IWebHostEnvironment environment)
    {
        _environment = environment;
    }
    public List<AddUserModel> ReadAdminDataFromExcel(string filePath)
    {
        var addUserModels = new List<AddUserModel>();

        using (var package = new ExcelPackage(new FileInfo(filePath)))
        {
            var worksheet = package.Workbook.Worksheets[0];  // Assuming data is in the first worksheet
            var rowCount = worksheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++) // Assuming the first row is headers
            {
                var birthdayText = worksheet.Cells[row, 11].Text;
                birthdayText = birthdayText.Trim('"', ' ');
                


                var addUserModel = new AddUserModel
                {
                    User = new AppUser
                    {
                        FirstName = worksheet.Cells[row, 2].Text,
                        MI = worksheet.Cells[row, 3].Text,
                        LastName = worksheet.Cells[row, 4].Text,
                        UserName = worksheet.Cells[row,5].Text,
                        Email = worksheet.Cells[row, 5].Text,
                        PhoneNumber = worksheet.Cells[row, 10].Text,  // Phone number
                        SSN = worksheet.Cells[row, 7].Text,
                        Address = worksheet.Cells[row, 8].Text,
                        ZipCode = worksheet.Cells[row, 9].Text,
         
                        Birthday = DateTime.Parse(birthdayText) // Parse the Birthday field
                    },
                    Password = worksheet.Cells[row, 6].Text,  // Assuming the Excel password column is the 5th column
                    RoleName = worksheet.Cells[row, 12].Text  // You can assign the role here or from Excel if you have a role column
                };

                addUserModels.Add(addUserModel);
            }
        }

        return addUserModels;
    }
    public static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roleNames = { "Admin", "Host", "Customer" };  // Add other roles here
        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    public async Task AddUsersToDatabase(string filePath, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        // Ensure roles exist before adding users
        await EnsureRolesAsync(roleManager);

        var excelHelper = new ExcelHelper(_environment);
        var addUserModels = excelHelper.ReadAdminDataFromExcel(filePath);

        foreach (var model in addUserModels)
        {
            var result = await AddUser.AddUserWithRoleAsync(model, userManager);

            if (result.Succeeded)
            {
                Console.WriteLine($"User {model.User.FirstName} {model.User.LastName} added successfully.");
            }
            else
            {
                Console.WriteLine($"Failed to add {model.User.FirstName} {model.User.LastName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }

}
