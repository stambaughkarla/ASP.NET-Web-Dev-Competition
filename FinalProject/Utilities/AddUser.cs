using FinalProject.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Text;
using System.Threading.Tasks;

namespace FinalProject.Utilities
{
    public static class AddUser
    {
        // Method to add or update a user, and assign roles
        public async static Task<IdentityResult> AddUserWithRoleAsync(AddUserModel aum, UserManager<AppUser> userManager)
        {
            // Check if the user already exists in the database
            AppUser dbUser = await userManager.FindByEmailAsync(aum.User.Email);

            IdentityResult result;

            // If user doesn't exist, create them
            if (dbUser == null)
            {
                try
                {
                    // Attempt to create the user using the UserManager
                    result = await userManager.CreateAsync(aum.User, aum.Password);
                }
                catch (Exception ex)
                {
                    // Handle exceptions if there are issues with creating the user
                    StringBuilder msg = new StringBuilder();
                    msg.Append("Error adding the user with the email ");
                    msg.Append(aum.User.Email);
                    msg.Append(". This may happen if a required field is missing from AppUser.");
                    throw new Exception(msg.ToString(), ex);
                }

                // Check if creation succeeded
                if (!result.Succeeded)
                {
                    // Collect errors if creation failed
                    StringBuilder msg = new StringBuilder();
                    foreach (var error in result.Errors)
                    {
                        msg.AppendLine(error.Description);
                    }

                    throw new Exception("Failed to add user: " + msg.ToString());
                }

                // Fetch the user after creation
                dbUser = await userManager.FindByEmailAsync(aum.User.Email);
            }
            else
            {
                // If user exists, update the fields (only Identity-related fields)
                dbUser.PhoneNumber = aum.User.PhoneNumber;
                dbUser.FirstName = aum.User.FirstName;
                dbUser.LastName = aum.User.LastName;

                // Use UserManager to update the user
                result = await userManager.UpdateAsync(dbUser);

                if (!result.Succeeded)
                {
                    StringBuilder msg = new StringBuilder();
                    foreach (var error in result.Errors)
                    {
                        msg.AppendLine(error.Description);
                    }
                    throw new Exception("Error updating user: " + msg.ToString());
                }

                // Reset the password (if required) using UserManager
                var token = await userManager.GeneratePasswordResetTokenAsync(dbUser);
                result = await userManager.ResetPasswordAsync(dbUser, token, aum.Password);
            }

            // Add user to the specified role if not already in the role
            if (!await userManager.IsInRoleAsync(dbUser, aum.RoleName))
            {
                await userManager.AddToRoleAsync(dbUser, aum.RoleName);
            }

            return result;
        }
    }

    // AddUserModel class to handle the user details and password
    public class AddUserModel
    {
        public AppUser User { get; set; }
        public string Password { get; set; }
        public string RoleName { get; set; }
    }
}
