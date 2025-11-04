using lmsbox.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace lmsBox.Server.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin,OrgAdmin,SuperAdmin")]
public class AdminReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminReportsController> _logger;

    public AdminReportsController(ApplicationDbContext context, ILogger<AdminReportsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Helper to get org filter
    private async Task<long?> GetOrgIdFilter()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        
        if (User.IsInRole("OrgAdmin") && user != null)
            return user.OrganisationID;
        
        return null;
    }

    #region User Activity Report

    [HttpGet("user-activity")]
    public async Task<IActionResult> GetUserActivityReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int minDaysDormant = 30)
    {
        try
        {
            var orgId = await GetOrgIdFilter();
            var start = startDate ?? DateTime.UtcNow.AddMonths(-3);
            var end = endDate ?? DateTime.UtcNow;

            var usersQuery = _context.Users.AsNoTracking();
            if (orgId.HasValue)
                usersQuery = usersQuery.Where(u => u.OrganisationID == orgId);

            var users = await usersQuery
                .Select(u => new
                {
                    userId = u.Id,
                    name = u.FirstName + " " + u.LastName,
                    email = u.Email ?? "",
                    status = u.ActiveStatus == 1 ? "Active" : u.ActiveStatus == 0 ? "Inactive" : "Suspended",
                    activeStatus = u.ActiveStatus,
                    createdOn = u.CreatedOn,
                    daysSinceCreated = EF.Functions.DateDiffDay(u.CreatedOn, DateTime.UtcNow)
                })
                .ToListAsync();

            // Calculate engagement scores based on course progress
            var userIds = users.Select(u => u.userId).ToList();
            var progressData = await _context.LearnerProgresses
                .AsNoTracking()
                .Where(lp => userIds.Contains(lp.UserId))
                .GroupBy(lp => lp.UserId)
                .Select(g => new
                {
                    userId = g.Key,
                    totalEnrollments = g.Count(),
                    completedCourses = g.Count(lp => lp.Completed),
                    inProgressCourses = g.Count(lp => !lp.Completed && lp.ProgressPercent > 0),
                    avgProgress = g.Average(lp => (double?)lp.ProgressPercent) ?? 0,
                    lastActivityDate = g.Max(lp => lp.CompletedAt) ?? DateTime.UtcNow
                })
                .ToListAsync();

            var result = users.Select(u =>
            {
                var progress = progressData.FirstOrDefault(p => p.userId == u.userId);
                var lastActivity = progress?.lastActivityDate ?? u.createdOn;
                var daysSinceLastActivity = (DateTime.UtcNow - lastActivity).Days;
                
                // Engagement score formula: 
                // - Average progress contributes 0-50 points
                // - Each completion adds 5 points (max 50)
                // - Active enrollments add 10 points (max 50)
                // - Recency bonus: lose 1 point per day inactive (max -50)
                var baseScore = (progress?.avgProgress ?? 0) * 0.5;
                var completionBonus = Math.Min(progress?.completedCourses ?? 0, 10) * 5;
                var enrollmentBonus = Math.Min(progress?.totalEnrollments ?? 0, 5) * 10;
                var recencyPenalty = Math.Min(daysSinceLastActivity, 50);
                var engagementScore = Math.Max(0, Math.Round(baseScore + completionBonus + enrollmentBonus - recencyPenalty, 2));

                var isDormant = daysSinceLastActivity > minDaysDormant;

                return new
                {
                    u.userId,
                    u.name,
                    u.email,
                    u.status,
                    u.createdOn,
                    lastActivityDate = lastActivity,
                    daysSinceLastActivity,
                    engagementScore,
                    isDormant,
                    enrollments = progress?.totalEnrollments ?? 0,
                    completions = progress?.completedCourses ?? 0,
                    inProgress = progress?.inProgressCourses ?? 0,
                    averageProgress = progress != null ? Math.Round(progress.avgProgress, 2) : 0
                };
            }).OrderByDescending(u => u.engagementScore).ToList();

            // Filter by date range based on creation or last activity
            result = result.Where(u => 
                (u.createdOn >= start && u.createdOn <= end) || 
                (u.lastActivityDate >= start && u.lastActivityDate <= end)
            ).ToList();

            var summary = new
            {
                totalUsers = result.Count,
                activeUsers = result.Count(u => u.status == "Active"),
                inactiveUsers = result.Count(u => u.status == "Inactive"),
                suspendedUsers = result.Count(u => u.status == "Suspended"),
                dormantUsers = result.Count(u => u.isDormant),
                averageEngagementScore = result.Any() ? Math.Round(result.Average(u => u.engagementScore), 2) : 0,
                highlyEngagedUsers = result.Count(u => u.engagementScore >= 70),
                moderatelyEngagedUsers = result.Count(u => u.engagementScore >= 40 && u.engagementScore < 70),
                lowEngagementUsers = result.Count(u => u.engagementScore < 40)
            };

            return Ok(new { users = result, summary });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating user activity report");
            return StatusCode(500, new { error = "Failed to generate user activity report", details = ex.Message });
        }
    }

    #endregion

    #region User Progress Report

    [HttpGet("user-progress")]
    public async Task<IActionResult> GetUserProgressReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var orgId = await GetOrgIdFilter();
            var start = startDate ?? DateTime.UtcNow.AddMonths(-3);
            var end = endDate ?? DateTime.UtcNow;

            var usersQuery = _context.Users.AsNoTracking();
            if (orgId.HasValue)
                usersQuery = usersQuery.Where(u => u.OrganisationID == orgId);

            var users = await usersQuery
                .Select(u => new
                {
                    userId = u.Id,
                    name = u.FirstName + " " + u.LastName,
                    email = u.Email,
                    createdOn = u.CreatedOn
                })
                .ToListAsync();

            var userIds = users.Select(u => u.userId).ToList();

            // Get progress data - fetch raw learner progress records grouped by user
            var learnerProgressByUser = await _context.LearnerProgresses
                .AsNoTracking()
                .Where(lp => userIds.Contains(lp.UserId))
                .ToListAsync();

            // Group and calculate metrics in memory
            var progressData = learnerProgressByUser
                .GroupBy(lp => lp.UserId)
                .Select(g => new
                {
                    userId = g.Key,
                    coursesEnrolled = g.Count(),
                    coursesCompleted = g.Count(lp => lp.Completed),
                    coursesInProgress = g.Count(lp => !lp.Completed && lp.ProgressPercent > 0),
                    overallProgress = g.Any() ? g.Average(lp => (double)lp.ProgressPercent) : 0,
                    completedCourses = g.Where(lp => lp.Completed && lp.CompletedAt.HasValue).ToList()
                })
                .ToList();

            var result = users.Select(u =>
            {
                var progress = progressData.FirstOrDefault(p => p.userId == u.userId);
                var monthsSinceCreated = Math.Max(1, (DateTime.UtcNow - u.createdOn).Days / 30.0);
                var learningVelocity = progress != null
                    ? Math.Round(progress.coursesCompleted / monthsSinceCreated, 2)
                    : 0;

                // Calculate average completion time from completed courses
                var avgCompletionTime = 0.0;
                if (progress?.completedCourses != null && progress.completedCourses.Any())
                {
                    var completionTimes = progress.completedCourses
                        .Where(c => c.CompletedAt.HasValue)
                        .Select(c => (DateTime.UtcNow - c.CompletedAt!.Value).Days)
                        .ToList();
                    
                    if (completionTimes.Any())
                    {
                        avgCompletionTime = Math.Abs(Math.Round(completionTimes.Average(), 1));
                    }
                }

                return new
                {
                    u.userId,
                    u.name,
                    u.email,
                    coursesEnrolled = progress?.coursesEnrolled ?? 0,
                    coursesCompleted = progress?.coursesCompleted ?? 0,
                    coursesInProgress = progress?.coursesInProgress ?? 0,
                    overallProgress = progress != null ? Math.Round(progress.overallProgress, 2) : 0,
                    averageCompletionTime = avgCompletionTime,
                    learningVelocity
                };
            }).ToList();

            var summary = new
            {
                totalLearners = result.Count,
                averageProgress = result.Any() ? Math.Round(result.Average(r => r.overallProgress), 2) : 0,
                averageCompletionTime = result.Any() ? Math.Round(result.Average(r => r.averageCompletionTime), 2) : 0,
                averageLearningVelocity = result.Any() ? Math.Round(result.Average(r => r.learningVelocity), 2) : 0,
                totalEnrollments = result.Sum(r => r.coursesEnrolled),
                totalCompletions = result.Sum(r => r.coursesCompleted)
            };

            return Ok(new { users = result, summary });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating user progress report");
            return StatusCode(500, new { error = "Failed to generate user progress report", details = ex.Message });
        }
    }

    #endregion

    #region Course Enrollment Report

    [HttpGet("course-enrollment")]
    public async Task<IActionResult> GetCourseEnrollmentReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var orgId = await GetOrgIdFilter();
            var start = startDate ?? DateTime.UtcNow.AddMonths(-3);
            var end = endDate ?? DateTime.UtcNow;

            var coursesQuery = _context.Courses.AsNoTracking();
            if (orgId.HasValue)
                coursesQuery = coursesQuery.Where(c => c.OrganisationId == orgId);

            var courses = await coursesQuery.ToListAsync();
            var courseIds = courses.Select(c => c.Id).ToList();

            // Get all learner progress records
            var allProgress = await _context.LearnerProgresses
                .AsNoTracking()
                .Where(lp => courseIds.Contains(lp.CourseId!) && lp.LessonId == null)
                .ToListAsync();

            // Group and calculate in memory
            var enrollmentData = allProgress
                .GroupBy(lp => lp.CourseId)
                .Select(g => new
                {
                    courseId = g.Key,
                    totalEnrollments = g.Count(),
                    activeEnrollments = g.Count(lp => !lp.Completed && lp.ProgressPercent > 0),
                    completedEnrollments = g.Count(lp => lp.Completed),
                    droppedEnrollments = g.Count(lp => !lp.Completed && lp.ProgressPercent == 0),
                    completionRate = g.Any() ? Math.Round((g.Count(lp => lp.Completed) / (double)g.Count()) * 100, 2) : 0
                })
                .ToList();

            var result = courses.Select(c =>
            {
                var enrollment = enrollmentData.FirstOrDefault(e => e.courseId == c.Id);
                var total = enrollment?.totalEnrollments ?? 0;
                var dropoffRate = total > 0 && enrollment != null
                    ? Math.Round((enrollment.droppedEnrollments / (double)total) * 100, 2)
                    : 0;

                return new
                {
                    courseId = c.Id,
                    courseTitle = c.Title,
                    category = c.Category,
                    status = c.Status,
                    createdAt = c.CreatedAt,
                    totalEnrollments = total,
                    activeEnrollments = enrollment?.activeEnrollments ?? 0,
                    completedEnrollments = enrollment?.completedEnrollments ?? 0,
                    completionRate = enrollment?.completionRate ?? 0,
                    dropoffRate,
                    popularity = total > 50 ? "High" : total > 20 ? "Medium" : "Low"
                };
            }).OrderByDescending(c => c.totalEnrollments).ToList();

            // Category-based enrollment breakdown
            var categoryBreakdown = result
                .GroupBy(c => c.category ?? "Uncategorized")
                .Select(g => new
                {
                    category = g.Key,
                    courses = g.Count(),
                    totalEnrollments = g.Sum(c => c.totalEnrollments)
                })
                .OrderByDescending(c => c.totalEnrollments)
                .ToList();

            var summary = new
            {
                totalCourses = result.Count,
                totalEnrollments = result.Sum(c => c.totalEnrollments),
                activeEnrollments = result.Sum(c => c.activeEnrollments),
                completedEnrollments = result.Sum(c => c.completedEnrollments),
                averageEnrollmentPerCourse = result.Any() ? Math.Round(result.Average(c => c.totalEnrollments), 2) : 0,
                averageDropoffRate = result.Any() ? Math.Round(result.Average(c => c.dropoffRate), 2) : 0,
                averageCompletionRate = result.Any() ? Math.Round(result.Average(c => c.completionRate), 2) : 0,
                mostPopularCourse = result.FirstOrDefault()?.courseTitle ?? "N/A",
                leastPopularCourse = result.LastOrDefault()?.courseTitle ?? "N/A"
            };

            return Ok(new { courses = result, summary, categoryBreakdown });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating course enrollment report");
            return StatusCode(500, new { error = "Failed to generate course enrollment report", details = ex.Message });
        }
    }

    #endregion

    #region Course Completion Report

    [HttpGet("course-completion")]
    public async Task<IActionResult> GetCourseCompletionReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var orgId = await GetOrgIdFilter();
            var start = startDate ?? DateTime.UtcNow.AddMonths(-3);
            var end = endDate ?? DateTime.UtcNow;

            var coursesQuery = _context.Courses.AsNoTracking();
            if (orgId.HasValue)
                coursesQuery = coursesQuery.Where(c => c.OrganisationId == orgId);

            var courses = await coursesQuery.ToListAsync();
            var courseIds = courses.Select(c => c.Id).ToList();

            // Get completion data
            var completionData = await _context.LearnerProgresses
                .AsNoTracking()
                .Where(lp => courseIds.Contains(lp.CourseId!))
                .GroupBy(lp => lp.CourseId)
                .Select(g => new
                {
                    courseId = g.Key,
                    totalEnrollments = g.Count(),
                    completedCount = g.Count(lp => lp.Completed),
                    incompleteCount = g.Count(lp => !lp.Completed),
                    avgCompletionTime = g
                        .Where(lp => lp.Completed && lp.CompletedAt.HasValue)
                        .Select(lp => (lp.CompletedAt!.Value - DateTime.UtcNow).Days)
                        .DefaultIfEmpty(0)
                        .Average()
                })
                .ToListAsync();

            var result = courses.Select(c =>
            {
                var completion = completionData.FirstOrDefault(cd => cd.courseId == c.Id);
                var total = completion?.totalEnrollments ?? 0;
                var completionRate = total > 0
                    ? Math.Round((completion!.completedCount / (double)total) * 100, 2)
                    : 0;

                return new
                {
                    courseId = c.Id,
                    courseTitle = c.Title,
                    category = c.Category,
                    totalEnrollments = total,
                    completedCount = completion?.completedCount ?? 0,
                    incompleteCount = completion?.incompleteCount ?? 0,
                    completionRate,
                    averageCompletionTime = completion != null ? Math.Abs(Math.Round(completion.avgCompletionTime, 1)) : 0
                };
            }).OrderByDescending(c => c.completionRate).ToList();

            // Completion trends (last 30 days)
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var completionTrends = await _context.LearnerProgresses
                .AsNoTracking()
                .Where(lp => courseIds.Contains(lp.CourseId!) &&
                             lp.Completed &&
                             lp.CompletedAt.HasValue &&
                             lp.CompletedAt.Value >= thirtyDaysAgo)
                .GroupBy(lp => lp.CompletedAt!.Value.Date)
                .Select(g => new { date = g.Key.ToString("MMM dd"), count = g.Count() })
                .ToListAsync();

            var summary = new
            {
                totalCourses = result.Count,
                averageCompletionRate = result.Any() ? Math.Round(result.Average(c => c.completionRate), 2) : 0,
                averageCompletionTime = result.Any() ? Math.Round(result.Average(c => c.averageCompletionTime), 2) : 0,
                totalCompletions = result.Sum(c => c.completedCount),
                totalIncomplete = result.Sum(c => c.incompleteCount)
            };

            return Ok(new { courses = result, summary, completionTrends });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating course completion report");
            return StatusCode(500, new { error = "Failed to generate course completion report", details = ex.Message });
        }
    }

    #endregion

    #region Lesson Analytics Report

    [HttpGet("lesson-analytics")]
    public async Task<IActionResult> GetLessonAnalyticsReport([FromQuery] string? courseId)
    {
        try
        {
            var orgId = await GetOrgIdFilter();

            var lessonsQuery = _context.Lessons.AsNoTracking();
            
            if (!string.IsNullOrEmpty(courseId))
                lessonsQuery = lessonsQuery.Where(l => l.CourseId == courseId);

            if (orgId.HasValue)
                lessonsQuery = lessonsQuery.Where(l => l.Course.OrganisationId == orgId);

            var lessons = await lessonsQuery.Include(l => l.Course).ToListAsync();
            var lessonIds = lessons.Select(l => l.Id).ToList();

            // Get lesson progress data
            var lessonProgressData = await _context.LearnerProgresses
                .AsNoTracking()
                .Where(lp => lp.LessonId.HasValue && lessonIds.Contains(lp.LessonId.Value))
                .GroupBy(lp => lp.LessonId)
                .Select(g => new
                {
                    lessonId = g.Key,
                    totalAccesses = g.Count(),
                    completions = g.Count(lp => lp.Completed),
                    avgProgress = g.Average(lp => lp.ProgressPercent)
                })
                .ToListAsync();

            var result = lessons.Select(l =>
            {
                var progress = lessonProgressData.FirstOrDefault(p => p.lessonId == l.Id);
                var totalAccesses = progress?.totalAccesses ?? 0;
                var completionRate = totalAccesses > 0
                    ? Math.Round((progress!.completions / (double)totalAccesses) * 100, 2)
                    : 0;

                return new
                {
                    lessonId = l.Id,
                    lessonTitle = l.Title,
                    lessonType = l.Type,
                    courseTitle = l.Course?.Title,
                    courseId = l.CourseId,
                    totalAccesses,
                    completions = progress?.completions ?? 0,
                    completionRate,
                    averageProgress = progress != null ? Math.Round(progress.avgProgress, 2) : 0
                };
            }).OrderByDescending(l => l.totalAccesses).ToList();

            var summary = new
            {
                totalLessons = result.Count,
                totalAccesses = result.Sum(l => l.totalAccesses),
                averageCompletionRate = result.Any() ? Math.Round(result.Average(l => l.completionRate), 2) : 0,
                mostAccessedLesson = result.FirstOrDefault()?.lessonTitle ?? "N/A",
                leastAccessedLesson = result.LastOrDefault()?.lessonTitle ?? "N/A",
                lessonsByType = result.GroupBy(l => l.lessonType)
                    .Select(g => new { type = g.Key, count = g.Count() })
                    .ToList()
            };

            return Ok(new { lessons = result, summary });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating lesson analytics report");
            return StatusCode(500, new { error = "Failed to generate lesson analytics report", details = ex.Message });
        }
    }

    #endregion

    #region Learning Pathway Reports

    [HttpGet("pathway-progress")]
    public async Task<IActionResult> GetPathwayProgressReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var orgId = await GetOrgIdFilter();
            var start = startDate ?? DateTime.UtcNow.AddMonths(-3);
            var end = endDate ?? DateTime.UtcNow;

            var pathwaysQuery = _context.LearningPathways.AsNoTracking();
            if (orgId.HasValue)
                pathwaysQuery = pathwaysQuery.Where(p => p.OrganisationId == orgId);

            var pathways = await pathwaysQuery.ToListAsync();
            var pathwayIds = pathways.Select(p => p.Id).ToList();

            // Get pathway progress data
            var pathwayProgressData = await _context.LearnerPathwayProgresses
                .AsNoTracking()
                .Where(lpp => pathwayIds.Contains(lpp.LearningPathwayId))
                .GroupBy(lpp => lpp.LearningPathwayId)
                .Select(g => new
                {
                    pathwayId = g.Key,
                    totalEnrollments = g.Count(),
                    completions = g.Count(lpp => lpp.IsCompleted),
                    avgProgress = g.Average(lpp => (double?)lpp.ProgressPercent) ?? 0,
                    avgCompletionTime = g
                        .Where(lpp => lpp.IsCompleted && lpp.CompletedAt.HasValue)
                        .Select(lpp => (lpp.CompletedAt!.Value - lpp.EnrolledAt).Days)
                        .DefaultIfEmpty(0)
                        .Average()
                })
                .ToListAsync();

            var result = pathways.Select(p =>
            {
                var progress = pathwayProgressData.FirstOrDefault(ppd => ppd.pathwayId == p.Id);
                var totalEnrollments = progress?.totalEnrollments ?? 0;
                var completionRate = totalEnrollments > 0
                    ? Math.Round((progress!.completions / (double)totalEnrollments) * 100, 2)
                    : 0;

                return new
                {
                    pathwayId = p.Id,
                    pathwayTitle = p.Title,
                    totalEnrollments,
                    completions = progress?.completions ?? 0,
                    completionRate,
                    averageProgress = progress != null ? Math.Round(progress.avgProgress, 2) : 0,
                    averageCompletionTime = progress != null ? Math.Round(progress.avgCompletionTime, 1) : 0,
                    dropoutRate = totalEnrollments > 0 ? Math.Round(100 - completionRate, 2) : 0
                };
            }).OrderByDescending(p => p.completionRate).ToList();

            var summary = new
            {
                totalPathways = result.Count,
                averageCompletionRate = result.Any() ? Math.Round(result.Average(p => p.completionRate), 2) : 0,
                averageCompletionTime = result.Any() ? Math.Round(result.Average(p => p.averageCompletionTime), 2) : 0,
                mostSuccessfulPathway = result.FirstOrDefault()?.pathwayTitle ?? "N/A",
                totalEnrollments = result.Sum(p => p.totalEnrollments),
                totalCompletions = result.Sum(p => p.completions)
            };

            return Ok(new { pathways = result, summary });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pathway progress report");
            return StatusCode(500, new { error = "Failed to generate pathway progress report", details = ex.Message });
        }
    }

    [HttpGet("pathway-assignments")]
    public async Task<IActionResult> GetPathwayAssignmentsReport()
    {
        try
        {
            var orgId = await GetOrgIdFilter();

            var pathwaysQuery = _context.LearningPathways.AsNoTracking();
            if (orgId.HasValue)
                pathwaysQuery = pathwaysQuery.Where(p => p.OrganisationId == orgId);

            var pathways = await pathwaysQuery.ToListAsync();
            var pathwayIds = pathways.Select(p => p.Id).ToList();

            // Get assignment data
            var assignmentData = await _context.LearnerPathwayProgresses
                .AsNoTracking()
                .Where(lpp => pathwayIds.Contains(lpp.LearningPathwayId))
                .GroupBy(lpp => lpp.LearningPathwayId)
                .Select(g => new
                {
                    pathwayId = g.Key,
                    totalUsers = g.Select(lpp => lpp.UserId).Distinct().Count(),
                    recentAssignments = g.Where(lpp => lpp.EnrolledAt >= DateTime.UtcNow.AddDays(-30)).Count()
                })
                .ToListAsync();

            var result = pathways.Select(p =>
            {
                var assignment = assignmentData.FirstOrDefault(ad => ad.pathwayId == p.Id);

                return new
                {
                    pathwayId = p.Id,
                    pathwayTitle = p.Title,
                    totalUsers = assignment?.totalUsers ?? 0,
                    recentAssignments = assignment?.recentAssignments ?? 0,
                    isActive = p.IsActive
                };
            }).OrderByDescending(p => p.totalUsers).ToList();

            var summary = new
            {
                totalPathways = result.Count,
                totalAssignments = result.Sum(p => p.totalUsers),
                recentAssignments = result.Sum(p => p.recentAssignments),
                mostAssignedPathway = result.FirstOrDefault()?.pathwayTitle ?? "N/A"
            };

            return Ok(new { pathways = result, summary });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pathway assignments report");
            return StatusCode(500, new { error = "Failed to generate pathway assignments report", details = ex.Message });
        }
    }

    #endregion

    #region Content Usage Report

    [HttpGet("content-usage")]
    public async Task<IActionResult> GetContentUsageReport()
    {
        try
        {
            var orgId = await GetOrgIdFilter();

            var coursesQuery = _context.Courses.AsNoTracking();
            if (orgId.HasValue)
                coursesQuery = coursesQuery.Where(c => c.OrganisationId == orgId);

            var courses = await coursesQuery.ToListAsync();
            var courseIds = courses.Select(c => c.Id).ToList();

            // Get content usage from progress
            var contentUsage = await _context.LearnerProgresses
                .AsNoTracking()
                .Where(lp => courseIds.Contains(lp.CourseId!))
                .GroupBy(lp => lp.CourseId)
                .Select(g => new
                {
                    courseId = g.Key,
                    accessCount = g.Count(),
                    uniqueUsers = g.Select(lp => lp.UserId).Distinct().Count()
                })
                .ToListAsync();

            var result = courses.Select(c =>
            {
                var usage = contentUsage.FirstOrDefault(cu => cu.courseId == c.Id);
                var accessCount = usage?.accessCount ?? 0;

                return new
                {
                    contentId = c.Id,
                    contentTitle = c.Title,
                    contentType = "Course",
                    category = c.Category,
                    accessCount,
                    uniqueUsers = usage?.uniqueUsers ?? 0,
                    engagementLevel = accessCount > 100 ? "High" : accessCount > 30 ? "Medium" : accessCount > 0 ? "Low" : "None",
                    isUnused = accessCount == 0
                };
            }).OrderByDescending(c => c.accessCount).ToList();

            var summary = new
            {
                totalContent = result.Count,
                totalAccesses = result.Sum(c => c.accessCount),
                unusedContent = result.Count(c => c.isUnused),
                highEngagement = result.Count(c => c.engagementLevel == "High"),
                mediumEngagement = result.Count(c => c.engagementLevel == "Medium"),
                lowEngagement = result.Count(c => c.engagementLevel == "Low"),
                mostAccessedContent = result.FirstOrDefault()?.contentTitle ?? "N/A"
            };

            return Ok(new { content = result, summary });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating content usage report");
            return StatusCode(500, new { error = "Failed to generate content usage report", details = ex.Message });
        }
    }

    #endregion
}
