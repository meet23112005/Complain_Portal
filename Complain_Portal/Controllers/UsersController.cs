using Complain_Portal.Data;
using Complain_Portal.DTOs;
using Complain_Portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Complain_Portal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext context;
        private readonly UserManager<ApplicationUser> userManager;

        public UsersController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }

        [HttpGet("officials")]
        public async Task<IActionResult> GetOfficials()
        {
            var officials = await userManager.GetUsersInRoleAsync("Official");
            if (officials == null || !officials.Any())
            {
                return Ok(new List<object>()); //official not found
            }
            var result = officials.Select(o => new
            {
                o.Id,
                o.FullName,
                o.Email,
                o.PhoneNumber,
                o.Department
            });
            return Ok(result);
        }

        [HttpPost("create-official")]
        public async Task<IActionResult> CreateOfficial([FromBody]CreateOfficialDto payload)
        {
            var ueserExists = await userManager.FindByEmailAsync(payload.Email);
            if (ueserExists != null)
            {
                return BadRequest("User already exists with this email.");
            }

            ApplicationUser user = new ApplicationUser()
            {
                UserName = payload.Email,
                Email = payload.Email,
                PhoneNumber = payload.PhoneNumber,
                Department = payload.Department,
                FullName = payload.FullName
            };

            var result = await userManager.CreateAsync(user, payload.Password);
            if (!result.Succeeded)
            {
                return BadRequest("User creation failed! Please check user details and try again.");
            }
            await userManager.AddToRoleAsync(user, "Official");
            return Ok("Official user created successfully.");
        }

        [HttpPut("update-official")]
        public async Task<IActionResult> updateOfficial([FromBody]UpdateOfficialDto payload)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await userManager.FindByIdAsync(payload.Id);
            if (user == null)
            {
                return NotFound("User not found.");
            }
            user.Email = payload.Email ?? user.Email;
            user.PhoneNumber = payload.PhoneNumber ?? user.PhoneNumber;
            user.Department = payload.Department ?? user.Department;
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);
            return Ok("Official user Updated successfully.");


        }

        [HttpDelete("delete-official/{officialId}")]
        public async Task<IActionResult> DeleteOfficial(string officialId)
        {
            var official =  await userManager.FindByIdAsync(officialId);
            if (official == null)
            {
                return NotFound("Official user not found.");
            }

            // Safety: Prevent Admin from deleting themselves!
            if (await userManager.IsInRoleAsync(official, "Admin"))
            {
                return BadRequest("You cannot delete an Admin account via API.");
            }
            var result = await userManager.DeleteAsync(official);
            if (!result.Succeeded)
            {
                return BadRequest("Failed to delete the official user.");
            }
            return Ok("Official user deleted successfully.");
        }

    }
}
