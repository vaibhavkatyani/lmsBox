using System.Security.Claims;
using lmsbox.domain.Models;
using lmsbox.infrastructure.Data;
using lmsBox.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lmsBox.Server.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ProfileController> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly ApplicationDbContext _context;

    public ProfileController(
        UserManager<ApplicationUser> userManager, 
        ILogger<ProfileController> logger,
        IAuditLogService auditLogService,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _logger = logger;
        _auditLogService = auditLogService;
        _context = context;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // Load user with organisation and roles
        var user = await _context.Users
            .Include(u => u.Organisation)
            .Include(u => u.UserUserRoles)
                .ThenInclude(uur => uur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Unauthorized();

        // Get assigned learning pathways (via LearnerPathwayProgress)
        var assignedPathways = await _context.LearnerPathwayProgresses
            .Where(lp => lp.UserId == userId)
            .Include(lp => lp.LearningPathway)
            .Select(lp => new {
                id = lp.LearningPathwayId,
                title = lp.LearningPathway != null ? lp.LearningPathway.Title : null
            })
            .ToListAsync();

        // Get roles
        var roles = user.UserUserRoles.Select(uur => uur.Role != null ? uur.Role.Name : null).Where(r => r != null).ToList();

        return Ok(new
        {
            id = user.Id,
            firstName = user.FirstName,
            lastName = user.LastName,
            email = user.Email,
            organisation = user.Organisation != null ? user.Organisation.Name : null,
            roles,
            assignedPathways
        });
    }

    public class UpdateProfileRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    [HttpPut]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest(new { message = "First name and last name are required" });
        }

        var oldFirstName = user.FirstName;
        var oldLastName = user.LastName;

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = "Failed to update profile", errors = result.Errors });
        }

        // Log the profile update in audit log
        try
        {
            var wasEmpty = string.IsNullOrWhiteSpace(oldFirstName) && string.IsNullOrWhiteSpace(oldLastName);
            var action = wasEmpty 
                ? $"Profile Completed: {user.FirstName} {user.LastName}" 
                : $"Profile Updated: {user.FirstName} {user.LastName}";
            
            var details = wasEmpty
                ? $"User completed their profile. Email: {user.Email}, First Name: {user.FirstName}, Last Name: {user.LastName}"
                : $"User updated their profile. Email: {user.Email}, Old Name: {oldFirstName} {oldLastName}, New Name: {user.FirstName} {user.LastName}";

            await _auditLogService.LogCustomAction(
                action,
                $"{user.FirstName} {user.LastName} ({user.Email})",
                details
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log profile update for user {UserId}", user.Id);
            // Don't fail the request if audit logging fails
        }

        _logger.LogInformation("User {UserId} updated their profile", user.Id);
        return Ok(new { message = "Profile updated" });
    }
}
