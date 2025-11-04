using lmsbox.domain.Models;
using lmsbox.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace lmsBox.Server.Controllers;

[ApiController]
[Route("api/learner/progress")]
[Authorize] // Requires authenticated user (learners)
public class LearnerProgressController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LearnerProgressController> _logger;

    public LearnerProgressController(ApplicationDbContext context, ILogger<LearnerProgressController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Initialize or update course progress when a learner launches a course
    /// This creates both course-level and lesson-level progress records
    /// </summary>
    [HttpPost("courses/{courseId}/start")]
    public async Task<ActionResult<CourseProgressResponse>> StartCourse(string courseId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Verify user has access to this course
            var hasAccess = await _context.LearnerGroups
                .Where(lg => lg.UserId == userId && lg.IsActive)
                .Join(_context.GroupCourses, lg => lg.LearningGroupId, gc => gc.LearningGroupId, (lg, gc) => gc)
                .AnyAsync(gc => gc.CourseId == courseId);

            if (!hasAccess)
            {
                return Forbid("You don't have access to this course");
            }

            // Get course with lessons
            var course = await _context.Courses
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
            {
                return NotFound(new { message = "Course not found" });
            }

            // Check if course progress exists
            var courseProgress = await _context.LearnerProgresses
                .FirstOrDefaultAsync(lp => lp.UserId == userId && lp.CourseId == courseId && lp.LessonId == null);

            if (courseProgress == null)
            {
                // Create course-level progress record
                courseProgress = new LearnerProgress
                {
                    UserId = userId,
                    CourseId = courseId,
                    LessonId = null,
                    ProgressPercent = 0,
                    Completed = false,
                    CompletedAt = null
                };
                _context.LearnerProgresses.Add(courseProgress);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created course progress for user {UserId} on course {CourseId}", userId, courseId);
            }

            // Initialize lesson progress records if they don't exist
            var existingLessonProgress = await _context.LearnerProgresses
                .Where(lp => lp.UserId == userId && lp.CourseId == courseId && lp.LessonId != null)
                .Select(lp => lp.LessonId)
                .ToListAsync();

            var lessonsToInitialize = course.Lessons
                .Where(l => !existingLessonProgress.Contains(l.Id))
                .ToList();

            if (lessonsToInitialize.Any())
            {
                foreach (var lesson in lessonsToInitialize)
                {
                    _context.LearnerProgresses.Add(new LearnerProgress
                    {
                        UserId = userId,
                        CourseId = courseId,
                        LessonId = lesson.Id,
                        ProgressPercent = 0,
                        Completed = false,
                        CompletedAt = null
                    });
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("Initialized {Count} lesson progress records for user {UserId} on course {CourseId}", 
                    lessonsToInitialize.Count, userId, courseId);
            }

            return Ok(new CourseProgressResponse
            {
                CourseId = courseId,
                ProgressPercent = courseProgress.ProgressPercent,
                Completed = courseProgress.Completed,
                TotalLessons = course.Lessons.Count,
                CompletedLessons = await _context.LearnerProgresses
                    .CountAsync(lp => lp.UserId == userId && lp.CourseId == courseId && lp.LessonId != null && lp.Completed)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting course {CourseId} for user", courseId);
            return StatusCode(500, new { message = "An error occurred while starting the course" });
        }
    }

    /// <summary>
    /// Update lesson progress when a learner progresses through a lesson
    /// </summary>
    [HttpPut("lessons/{lessonId}")]
    public async Task<ActionResult<LessonProgressResponse>> UpdateLessonProgress(
        long lessonId,
        [FromBody] UpdateLessonProgressRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Get lesson with course info
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
            {
                return NotFound(new { message = "Lesson not found" });
            }

            // Verify user has access to this course
            var hasAccess = await _context.LearnerGroups
                .Where(lg => lg.UserId == userId && lg.IsActive)
                .Join(_context.GroupCourses, lg => lg.LearningGroupId, gc => gc.LearningGroupId, (lg, gc) => gc)
                .AnyAsync(gc => gc.CourseId == lesson.CourseId);

            if (!hasAccess)
            {
                return Forbid("You don't have access to this lesson");
            }

            // Get or create lesson progress
            var lessonProgress = await _context.LearnerProgresses
                .FirstOrDefaultAsync(lp => lp.UserId == userId && lp.LessonId == lessonId);

            if (lessonProgress == null)
            {
                lessonProgress = new LearnerProgress
                {
                    UserId = userId,
                    CourseId = lesson.CourseId,
                    LessonId = lessonId,
                    ProgressPercent = 0,
                    Completed = false
                };
                _context.LearnerProgresses.Add(lessonProgress);
            }

            // Update lesson progress
            lessonProgress.ProgressPercent = Math.Clamp(request.ProgressPercent, 0, 100);
            
            if (request.ProgressPercent >= 100 && !lessonProgress.Completed)
            {
                lessonProgress.Completed = true;
                lessonProgress.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Recalculate course progress
            await UpdateCourseProgress(userId, lesson.CourseId);

            return Ok(new LessonProgressResponse
            {
                LessonId = lessonId,
                ProgressPercent = lessonProgress.ProgressPercent,
                Completed = lessonProgress.Completed,
                CompletedAt = lessonProgress.CompletedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lesson progress for lesson {LessonId}", lessonId);
            return StatusCode(500, new { message = "An error occurred while updating lesson progress" });
        }
    }

    /// <summary>
    /// Mark a lesson as completed
    /// </summary>
    [HttpPost("lessons/{lessonId}/complete")]
    public async Task<ActionResult<LessonProgressResponse>> CompleteLesson(long lessonId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Get lesson with course info
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
            {
                return NotFound(new { message = "Lesson not found" });
            }

            // Verify user has access
            var hasAccess = await _context.LearnerGroups
                .Where(lg => lg.UserId == userId && lg.IsActive)
                .Join(_context.GroupCourses, lg => lg.LearningGroupId, gc => gc.LearningGroupId, (lg, gc) => gc)
                .AnyAsync(gc => gc.CourseId == lesson.CourseId);

            if (!hasAccess)
            {
                return Forbid("You don't have access to this lesson");
            }

            // Get or create lesson progress
            var lessonProgress = await _context.LearnerProgresses
                .FirstOrDefaultAsync(lp => lp.UserId == userId && lp.LessonId == lessonId);

            if (lessonProgress == null)
            {
                lessonProgress = new LearnerProgress
                {
                    UserId = userId,
                    CourseId = lesson.CourseId,
                    LessonId = lessonId,
                    ProgressPercent = 100,
                    Completed = true,
                    CompletedAt = DateTime.UtcNow
                };
                _context.LearnerProgresses.Add(lessonProgress);
            }
            else if (!lessonProgress.Completed)
            {
                lessonProgress.ProgressPercent = 100;
                lessonProgress.Completed = true;
                lessonProgress.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Recalculate course progress
            await UpdateCourseProgress(userId, lesson.CourseId);

            return Ok(new LessonProgressResponse
            {
                LessonId = lessonId,
                ProgressPercent = 100,
                Completed = true,
                CompletedAt = lessonProgress.CompletedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing lesson {LessonId}", lessonId);
            return StatusCode(500, new { message = "An error occurred while completing the lesson" });
        }
    }

    /// <summary>
    /// Get progress for a specific course
    /// </summary>
    [HttpGet("courses/{courseId}")]
    public async Task<ActionResult<CourseProgressDetailResponse>> GetCourseProgress(string courseId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Get course with lessons
            var course = await _context.Courses
                .Include(c => c.Lessons.OrderBy(l => l.Ordinal))
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
            {
                return NotFound(new { message = "Course not found" });
            }

            // Get all progress records for this course
            var allProgress = await _context.LearnerProgresses
                .Where(lp => lp.UserId == userId && lp.CourseId == courseId)
                .ToListAsync();

            var courseProgress = allProgress.FirstOrDefault(lp => lp.LessonId == null);
            var lessonProgress = allProgress.Where(lp => lp.LessonId != null).ToList();

            var response = new CourseProgressDetailResponse
            {
                CourseId = courseId,
                CourseTitle = course.Title,
                ProgressPercent = courseProgress?.ProgressPercent ?? 0,
                Completed = courseProgress?.Completed ?? false,
                CompletedAt = courseProgress?.CompletedAt,
                TotalLessons = course.Lessons.Count,
                CompletedLessons = lessonProgress.Count(lp => lp.Completed),
                Lessons = course.Lessons.Select(lesson =>
                {
                    var progress = lessonProgress.FirstOrDefault(lp => lp.LessonId == lesson.Id);
                    return new LessonProgressInfo
                    {
                        LessonId = lesson.Id,
                        LessonTitle = lesson.Title,
                        Ordinal = lesson.Ordinal,
                        ProgressPercent = progress?.ProgressPercent ?? 0,
                        Completed = progress?.Completed ?? false,
                        CompletedAt = progress?.CompletedAt
                    };
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting course progress for course {CourseId}", courseId);
            return StatusCode(500, new { message = "An error occurred while fetching course progress" });
        }
    }

    /// <summary>
    /// Helper method to recalculate and update course-level progress based on lesson completion
    /// </summary>
    private async Task UpdateCourseProgress(string userId, string courseId)
    {
        // Get total lessons count
        var totalLessons = await _context.Lessons
            .CountAsync(l => l.CourseId == courseId);

        if (totalLessons == 0)
        {
            return; // No lessons to track
        }

        // Get completed lessons count
        var completedLessons = await _context.LearnerProgresses
            .CountAsync(lp => lp.UserId == userId && lp.CourseId == courseId && lp.LessonId != null && lp.Completed);

        // Calculate progress percentage
        var progressPercent = (int)Math.Round((double)completedLessons / totalLessons * 100);

        // Get or create course progress
        var courseProgress = await _context.LearnerProgresses
            .FirstOrDefaultAsync(lp => lp.UserId == userId && lp.CourseId == courseId && lp.LessonId == null);

        if (courseProgress == null)
        {
            courseProgress = new LearnerProgress
            {
                UserId = userId,
                CourseId = courseId,
                LessonId = null,
                ProgressPercent = progressPercent,
                Completed = progressPercent >= 100,
                CompletedAt = progressPercent >= 100 ? DateTime.UtcNow : null
            };
            _context.LearnerProgresses.Add(courseProgress);
        }
        else
        {
            courseProgress.ProgressPercent = progressPercent;
            
            if (progressPercent >= 100 && !courseProgress.Completed)
            {
                courseProgress.Completed = true;
                courseProgress.CompletedAt = DateTime.UtcNow;
            }
            else if (progressPercent < 100 && courseProgress.Completed)
            {
                // Handle case where completion status might need to be reverted
                courseProgress.Completed = false;
                courseProgress.CompletedAt = null;
            }
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Updated course progress for user {UserId} on course {CourseId}: {Progress}% ({Completed}/{Total} lessons)", 
            userId, courseId, progressPercent, completedLessons, totalLessons);
    }
}

// Request/Response DTOs
public class UpdateLessonProgressRequest
{
    public int ProgressPercent { get; set; }
}

public class CourseProgressResponse
{
    public string CourseId { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
    public bool Completed { get; set; }
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
}

public class LessonProgressResponse
{
    public long LessonId { get; set; }
    public int ProgressPercent { get; set; }
    public bool Completed { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class CourseProgressDetailResponse
{
    public string CourseId { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
    public bool Completed { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
    public List<LessonProgressInfo> Lessons { get; set; } = new();
}

public class LessonProgressInfo
{
    public long LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public int Ordinal { get; set; }
    public int ProgressPercent { get; set; }
    public bool Completed { get; set; }
    public DateTime? CompletedAt { get; set; }
}
