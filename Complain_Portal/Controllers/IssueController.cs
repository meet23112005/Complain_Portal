using Complain_Portal.Data;
using Complain_Portal.DTOs;
using Complain_Portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Complain_Portal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class IssueController : ControllerBase
    {
        private readonly AppDbContext context;

        public IssueController(AppDbContext context)
        {
            this.context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateIssue([FromForm] CreateIssueDto payload)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // var UserId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            //or
            var UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (UserId == null)
            {
                return Unauthorized("User ID not found in token.");
            }

            // 1. Handle File Upload
            string uniqueFileName = null;
            if (payload.Photo != null && payload.Photo.Length > 0)
            {
                var FolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploads");

                if (!Directory.Exists(FolderPath))
                    Directory.CreateDirectory(FolderPath);
                uniqueFileName = $"{Guid.NewGuid().ToString()}_{payload.Photo.FileName}";
                var FilePath = Path.Combine(FolderPath, uniqueFileName);
                using (var stream = new FileStream(FilePath, FileMode.Create))
                {
                    await payload.Photo.CopyToAsync(stream);
                }
            }

            var issue = new Issue
            {
                Title = payload.Title,
                Description = payload.Description,
                Location = payload.Location,
                PhotoUrl = uniqueFileName, // Photo upload handling can be added later
                DepartmentCategory = payload.DepartmentCategory,
                Status = IssueStatus.Pending,
                DateReported = DateTime.Now,
                ReporterUserId = UserId
            };

            var result = await context.Issues.AddAsync(issue);
            await context.SaveChangesAsync();

            return Ok(new { Message = "Complaint Registered Successfully!", IssueId = issue.Id });
        }


        //slow way
        // [HttpGet]
        //    public async Task<IActionResult> GetIssues()
        //    {
        //        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //        if (userId == null)
        //        {
        //            return Unauthorized("User ID not found in token.");
        //        }
        //        List<Issue> issues;

        //        if (User.IsInRole("Admin"))
        //        {
        //            issues = await context.Issues
        //                .Include(i => i.ReporterUser)
        //                .OrderByDescending(i => i.DateReported)
        //                .ToListAsync();
        //        }
        //        else
        //        {
        //            issues = await context.Issues
        //                .Include(i => i.ReporterUser)
        //                .Where(u => u.ReporterUserId == userId)
        //                .OrderByDescending(i => i.DateReported)
        //                .ToListAsync();
        //        }

        //        var baseurl = $"{Request.Scheme}://{Request.Host}/uploads/";

        //        var result = issues.Select(i => new
        //        {
        //            i.Id,
        //            i.Title,
        //            i.Description,
        //            i.Status,
        //            i.Location,
        //            DateReported = i.DateReported.ToString("yyyy-MM-dd HH-mm-ss"),
        //            PhotoUrl = i.PhotoUrl != null ? baseurl + i.PhotoUrl : null,
        //            ReporterName = i.ReporterUser?.UserName ?? "Unknown"
        //        });
        //        return Ok(result);
        //    }

        //optimized way
        [HttpGet]
        public async Task<IActionResult> GetIssues([FromQuery] IssueStatus? status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var IsAdmin = User.IsInRole("Admin");
            var baseUrl = $"{Request.Scheme}:/{Request.Host}/Uploads/";

            var query = context.Issues.AsQueryable();

            if (User.IsInRole("Official"))
            {
                query = context.Issues.Where(i => i.AssignedOfficialId == userId);
            }
            else if (!IsAdmin)
            {
                query = context.Issues.Where(u => u.ReporterUserId == userId);
            }


            if (status.HasValue)
            {
                query = query.Where(i => i.Status == status);
            }

            var result = await query
                .OrderByDescending(i => i.DateReported)
                .Select(i => new
                {
                    i.Id,
                    i.Title,
                    i.Description,
                    i.Status,
                    i.Location,
                    DateReported = i.DateReported.ToString("yyyy-MM-dd HH-mm-ss"),
                    PhotoUrl = i.PhotoUrl != null ? baseUrl + i.PhotoUrl : null,
                    ReporterName = i.ReporterUser != null ? i.ReporterUser.UserName : "UnKnown"
                }).ToListAsync();
            return Ok(result);
        }

        [HttpPut("{issueId}/assign")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignIssue(int issueId, [FromBody] AssignIssueDto payload)
        {
            var issue = await context.Issues.FindAsync(issueId);
            if (issue == null)
            {
                return NotFound("Issue not found.");
            }

            //verify official exist or not
            var officialExists = await context.Users.AnyAsync(u => u.Id == payload.OfficialUserId);
            if (!officialExists) return BadRequest("Official user not found.");

            issue.AssignedOfficialId = payload.OfficialUserId;
            issue.Status = IssueStatus.Assigned;

            await context.SaveChangesAsync();
            return Ok(new { Message = "Issue assigned successfully!", Status = "Assigned" });
        }

        [HttpPatch("{issueId}/status")]
        [Authorize(Roles = "Admin, Official")]
        public async Task<IActionResult> UpdateStatus(int issueId, [FromBody] UpdateStatusDto payload)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var issue = await context.Issues.FindAsync(issueId);
            if (issue == null)
            {
                return NotFound("Issue not found.");
            }

            if (User.IsInRole("Official"))
            {
                if (issue.AssignedOfficialId != currentUserId)
                {
                    return StatusCode(403, "You can only update issues assigned to you.");
                }
            }

            issue.Status = payload.Status;

            if (issue.Status == IssueStatus.Resolved)
            { issue.DateResolved = DateTime.UtcNow; }
            else { issue.DateResolved = null; }

            await context.SaveChangesAsync();

            return Ok(new { Message = "Work Status Updated!", NewStatus = issue.Status.ToString() });

        }
    }
}
