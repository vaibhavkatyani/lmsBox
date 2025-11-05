using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using lmsbox.domain.Models;
using lmsbox.infrastructure.Data;
using lmsBox.Server.Services;
using System.ComponentModel.DataAnnotations;

namespace lmsBox.Server.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin,OrgAdmin,SuperAdmin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly ILogger<AdminUsersController> _logger;

        public AdminUsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IConfiguration config,
            ILogger<AdminUsersController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _config = config;
            _logger = logger;
        }

        // GET /api/admin/users
        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            [FromQuery] string? status = null,
            [FromQuery] string sortBy = "lastName",
            [FromQuery] string sortOrder = "asc")
        {
            try
            {
                // Validate pagination parameters
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100; // Max page size limit

                var query = _context.Users.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchTerm = search.ToLower();
                    query = query.Where(u => 
                        (u.FirstName != null && u.FirstName.ToLower().Contains(searchTerm)) ||
                        (u.LastName != null && u.LastName.ToLower().Contains(searchTerm)) ||
                        (u.Email != null && u.Email.ToLower().Contains(searchTerm)));
                }

                // Apply status filter
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (status.ToLower() == "active")
                        query = query.Where(u => u.EmailConfirmed);
                    else if (status.ToLower() == "pending")
                        query = query.Where(u => !u.EmailConfirmed);
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply sorting
                query = sortBy.ToLower() switch
                {
                    "firstname" => sortOrder.ToLower() == "desc" 
                        ? query.OrderByDescending(u => u.FirstName) 
                        : query.OrderBy(u => u.FirstName),
                    "lastname" => sortOrder.ToLower() == "desc" 
                        ? query.OrderByDescending(u => u.LastName) 
                        : query.OrderBy(u => u.LastName),
                    "email" => sortOrder.ToLower() == "desc" 
                        ? query.OrderByDescending(u => u.Email) 
                        : query.OrderBy(u => u.Email),
                    "createdon" => sortOrder.ToLower() == "desc" 
                        ? query.OrderByDescending(u => u.CreatedOn) 
                        : query.OrderBy(u => u.CreatedOn),
                    _ => query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
                };

                // Apply pagination
                var users = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        id = u.Id,
                        firstName = u.FirstName,
                        lastName = u.LastName,
                        email = u.Email,
                        status = u.EmailConfirmed ? "Active" : "Pending",
                        joinedDate = u.CreatedOn.ToString("yyyy-MM-dd"),
                        role = "Learner" // Default, will be updated with actual roles
                    })
                    .ToListAsync();

                // Get roles for each user (optimized with batch processing)
                var userIds = users.Select(u => u.id).ToList();
                var appUsers = await _userManager.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
                
                var usersWithRoles = new List<object>();
                foreach (var user in users)
                {
                    var appUser = appUsers.FirstOrDefault(au => au.Id == user.id);
                    var roles = appUser != null ? await _userManager.GetRolesAsync(appUser) : new List<string>();
                    var primaryRole = roles.FirstOrDefault() ?? "Learner";

                    // Apply role filter if specified
                    if (!string.IsNullOrWhiteSpace(role) && !roles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    usersWithRoles.Add(new
                    {
                        user.id,
                        user.firstName,
                        user.lastName,
                        name = $"{user.firstName} {user.lastName}".Trim(),
                        user.email,
                        user.status,
                        user.joinedDate,
                        role = primaryRole,
                        roles = roles.ToList(),
                        groupNames = new List<string>() // TODO: Implement group lookup if needed
                    });
                }

                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                return Ok(new
                {
                    items = usersWithRoles,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalPages,
                        totalCount,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { message = "Failed to retrieve users" });
            }
        }

        // GET /api/admin/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault() ?? "Learner";

                // Get learning pathway assignments with names
                var learnerGroups = await _context.LearnerGroups
                    .Where(lg => lg.UserId == id && lg.IsActive)
                    .Include(lg => lg.LearningGroup)
                    .Select(lg => new
                    {
                        id = lg.LearningGroupId.ToString(),
                        name = lg.LearningGroup!.Name
                    })
                    .ToListAsync();

                var result = new
                {
                    id = user.Id,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    role = primaryRole,
                    status = user.EmailConfirmed ? "Active" : "Pending",
                    learningPathways = learnerGroups
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                return StatusCode(500, new { message = "Failed to retrieve user" });
            }
        }

        // POST /api/admin/users
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                _logger.LogInformation("CreateUser called with request: {@Request}", request);
                
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model validation failed: {@ModelState}", ModelState);
                    return BadRequest(ModelState);
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("User with email {Email} already exists", request.Email);
                    return BadRequest(new { message = "A user with this email already exists" });
                }

                _logger.LogInformation("Creating new user with email {Email}", request.Email);

                // Get the current admin user to set proper references
                var currentAdminUser = await _userManager.GetUserAsync(User);
                var currentAdminId = currentAdminUser?.Id ?? "system";
                var currentAdminName = User.Identity?.Name ?? "admin";

                // Get the organisation (assuming there's one organisation for now)
                var organisation = await _context.Organisations.FirstOrDefaultAsync();
                if (organisation == null)
                {
                    _logger.LogError("No organisation found in the system");
                    return StatusCode(500, new { message = "System configuration error: No organisation found" });
                }

                // Create new user
                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    EmailConfirmed = true, // Auto-confirm for admin-created users
                    OrganisationID = organisation.Id,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = currentAdminId,
                    // Set required activation fields
                    ActiveStatus = 1, // Assuming 1 = Active
                    ActivatedOn = DateTime.UtcNow,
                    ActivatedBy = currentAdminId,
                    DeactivatedBy = currentAdminId // Set to current admin (following seeder pattern)
                };

                _logger.LogInformation("ApplicationUser object created, generating password");

                // Since we use login links instead of passwords, create user without a usable password
                // We'll create with a temporary password and then clear it
                var tempPassword = GenerateRandomPassword();
                _logger.LogInformation("Creating user with UserManager for {Email} (password will be disabled)", request.Email);
                var result = await _userManager.CreateAsync(user, tempPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to create user {Email}: {Errors}", request.Email, errors);
                    return BadRequest(new { message = "Failed to create user", errors = result.Errors });
                }

                // Clear the password hash since we use login links instead of passwords
                try
                {
                    user.PasswordHash = null;
                    var updateResult = await _userManager.UpdateAsync(user);
                    if (updateResult.Succeeded)
                    {
                        _logger.LogInformation("Password hash cleared for user {UserId} - user will use login links", user.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to clear password hash for user {UserId}, but user was created", user.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Exception while clearing password hash for user {UserId}, but user was created", user.Id);
                }

                _logger.LogInformation("User {UserId} created successfully, assigning role {Role}", user.Id, request.Role);

                // Assign role
                var roleToAssign = request.Role ?? "Learner";
                if (!string.IsNullOrEmpty(roleToAssign))
                {
                    try
                    {
                        var roleResult = await _userManager.AddToRoleAsync(user, roleToAssign);
                        if (!roleResult.Succeeded)
                        {
                            var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                            _logger.LogError("Failed to assign role {Role} to user {UserId}: {Errors}", roleToAssign, user.Id, roleErrors);
                            // Don't fail the user creation if role assignment fails, just log it
                        }
                        else
                        {
                            _logger.LogInformation("Role {Role} assigned successfully to user {UserId}", roleToAssign, user.Id);
                        }
                    }
                    catch (Exception roleEx)
                    {
                        _logger.LogError(roleEx, "Exception occurred while assigning role {Role} to user {UserId}", roleToAssign, user.Id);
                    }
                }

                // Send registration notification email
                string emailStatus = "sent";
                _logger.LogInformation("Attempting to send registration email to {Email}", user.Email);
                try
                {
                    if (_emailService == null)
                    {
                        _logger.LogWarning("EmailService is null, cannot send registration email");
                        emailStatus = "failed";
                    }
                    else
                    {
                        var baseUrl = _config["AppSettings:BaseUrl"] ?? Request.Scheme + "://" + Request.Host;
                        var loginUrl = $"{baseUrl}/login";
                        
                        _logger.LogInformation("Email service available, sending to {Email}", user.Email);
                        
                        await _emailService.SendUserRegistrationNotificationAsync(
                            user.Email,
                            user.FirstName,
                            user.LastName,
                            roleToAssign,
                            loginUrl);
                            
                        _logger.LogInformation("Registration email sent successfully to {Email}", user.Email);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send registration email to {Email}", user.Email);
                    emailStatus = "failed";
                    // Don't fail the user creation if email fails
                }

                _logger.LogInformation("User {UserId} created successfully by {AdminUser}", user.Id, User.Identity?.Name);

                var message = emailStatus == "sent" 
                    ? "User created successfully and registration email sent" 
                    : "User created successfully but registration email failed to send";

                return Ok(new { id = user.Id, message = message, emailStatus = emailStatus });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating user with email {Email}", request?.Email);
                return StatusCode(500, new { message = "Failed to create user" });
            }
        }

        // PUT /api/admin/users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Update user properties
                user.FirstName = request.FirstName;
                user.LastName = request.LastName;
                user.Email = request.Email;
                user.UserName = request.Email; // Keep username in sync with email

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    return BadRequest(new { message = "Failed to update user", errors = updateResult.Errors });
                }

                // Update role if specified and different
                if (!string.IsNullOrEmpty(request.Role))
                {
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    var newRole = request.Role;

                    if (!currentRoles.Contains(newRole))
                    {
                        // Remove existing roles and add new one
                        if (currentRoles.Any())
                        {
                            await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        }
                        await _userManager.AddToRoleAsync(user, newRole);
                    }
                }

                _logger.LogInformation("User {UserId} updated successfully by {AdminUser}", user.Id, User.Identity?.Name);

                return Ok(new { message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new { message = "Failed to update user" });
            }
        }

        // DELETE /api/admin/users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Prevent deletion of the current admin user
                if (user.Id == _userManager.GetUserId(User))
                {
                    return BadRequest(new { message = "Cannot delete your own account" });
                }

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { message = "Failed to delete user", errors = result.Errors });
                }

                _logger.LogInformation("User {UserId} deleted successfully by {AdminUser}", user.Id, User.Identity?.Name);

                return Ok(new { success = true, message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, new { message = "Failed to delete user" });
            }
        }

        private string GenerateRandomPassword()
        {
            // Generate a random password that meets ASP.NET Identity requirements
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}