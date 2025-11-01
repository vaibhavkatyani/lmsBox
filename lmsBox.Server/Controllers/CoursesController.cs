using lmsbox.domain.Models;
using lmsbox.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace lmsBox.Server.Controllers;

[ApiController]
[Route("api/learner/courses")]
[Authorize] // Requires authenticated user
public class CoursesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CoursesController> _logger;

    public CoursesController(ApplicationDbContext context, ILogger<CoursesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get courses for the current learner with their progress
    /// </summary>
    /// <param name="search">Optional search query</param>
    /// <param name="progress">Filter by progress: all, not_started, in_progress, completed</param>
    /// <returns>List of courses with user progress</returns>
    [HttpGet]
    public async Task<ActionResult<CourseListResponse>> GetMyCourses(
        [FromQuery] string? search = null,
        [FromQuery] string? progress = "all")
    {
        try
        {
            // Get current user ID from claims
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Get user's learning groups
            var userGroupIds = await _context.LearnerGroups
                .Where(lg => lg.UserId == userId && lg.IsActive)
                .Select(lg => lg.LearningGroupId)
                .ToListAsync();

            // Get courses mapped to these groups
            var courseQuery = _context.Courses
                .Include(c => c.GroupCourses)
                .Where(c => c.GroupCourses.Any(gc => userGroupIds.Contains(gc.LearningGroupId)))
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                courseQuery = courseQuery.Where(c =>
                    c.Title.ToLower().Contains(searchLower) ||
                    (c.Description != null && c.Description.ToLower().Contains(searchLower))
                );
            }

            var courses = await courseQuery
                .Select(c => new
                {
                    Course = c,
                    UserProgress = _context.LearnerProgresses
                        .Where(lp => lp.UserId == userId && lp.CourseId == c.Id && lp.LessonId == null)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // Map to DTOs
            var courseDtos = courses.Select(r => new CourseItemDto
            {
                Id = r.Course.Id.ToString(),
                Title = r.Course.Title,
                Banner = "/assets/default-course-banner.png", // TODO: Add banner field to Course model
                Progress = r.UserProgress?.ProgressPercent ?? 0,
                EnrolledDate = r.UserProgress != null ? r.Course.CreatedAt : null,
                LastAccessedDate = null, // TODO: Add LastAccessedDate to LearnerProgress model
                IsCompleted = r.UserProgress?.Completed ?? false,
                CertificateEligible = r.UserProgress?.Completed ?? false // TODO: Add certificate logic
            }).ToList();

            // Apply progress filter
            courseDtos = progress switch
            {
                "not_started" => courseDtos.Where(c => c.Progress == 0).ToList(),
                "in_progress" => courseDtos.Where(c => c.Progress > 0 && c.Progress < 100).ToList(),
                "completed" => courseDtos.Where(c => c.Progress >= 100).ToList(),
                _ => courseDtos
            };

            return Ok(new CourseListResponse
            {
                Items = courseDtos,
                Total = courseDtos.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching learner courses for user");
            return StatusCode(500, new { message = "An error occurred while fetching courses" });
        }
    }

    /// <summary>
    /// Get courses with certificates (completed courses)
    /// </summary>
    [HttpGet("certificates")]
    public async Task<ActionResult<CourseListResponse>> GetCertificates()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var certificates = await _context.LearnerProgresses
                .Include(lp => lp.Course)
                .Where(lp => lp.UserId == userId
                    && lp.CourseId != null
                    && lp.LessonId == null
                    && lp.Completed
                    && lp.CompletedAt != null)
                .Select(lp => new CourseItemDto
                {
                    Id = lp.Course!.Id.ToString(),
                    Title = lp.Course.Title,
                    Banner = "/assets/default-course-banner.png",
                    Progress = 100,
                    EnrolledDate = lp.Course.CreatedAt,
                    LastAccessedDate = null,
                    IsCompleted = true,
                    CertificateEligible = true,
                    CertificateIssuedDate = lp.CompletedAt,
                    CertificateUrl = null // TODO: Generate certificate URL
                })
                .ToListAsync();

            return Ok(new CourseListResponse
            {
                Items = certificates,
                Total = certificates.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching certificates");
            return StatusCode(500, new { message = "An error occurred while fetching certificates" });
        }
    }
}

// DTOs
public class CourseListResponse
{
    public List<CourseItemDto> Items { get; set; } = new();
    public int Total { get; set; }
}

public class CourseItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Banner { get; set; } = string.Empty;
    public int Progress { get; set; }
    public DateTime? EnrolledDate { get; set; }
    public DateTime? LastAccessedDate { get; set; }
    public bool IsCompleted { get; set; }
    public bool CertificateEligible { get; set; }
    public DateTime? CertificateIssuedDate { get; set; }
    public string? CertificateUrl { get; set; }
}
