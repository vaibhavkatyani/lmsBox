namespace lmsbox.domain.Models;

public class LearningPathwayDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public bool IsActive { get; set; } = true;
    public string? BannerUrl { get; set; }
    public int EstimatedDurationHours { get; set; }
    public string DifficultyLevel { get; set; } = "Beginner";
    public long OrganisationId { get; set; }
    public string OrganisationName { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CourseCount { get; set; }
    public List<PathwayCourseDto> Courses { get; set; } = new();
}

public class PathwayCourseDto
{
    public long Id { get; set; }
    public string CourseId { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string? CourseDescription { get; set; }
    public int SequenceOrder { get; set; }
    public bool IsMandatory { get; set; } = true;
    public List<string> PrerequisiteCourseIds { get; set; } = new();
    public DateTime AddedAt { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsAccessible { get; set; } = true;
}

public class LearningPathwayListResponse
{
    public List<LearningPathwayDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class CreateLearningPathwayRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public string? BannerUrl { get; set; }
    public int EstimatedDurationHours { get; set; }
    public string DifficultyLevel { get; set; } = "Beginner";
    public List<CreatePathwayCourseRequest> Courses { get; set; } = new();
}

public class CreatePathwayCourseRequest
{
    public string CourseId { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public bool IsMandatory { get; set; } = true;
    public List<string>? PrerequisiteCourseIds { get; set; }
}

public class UpdateLearningPathwayRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public bool IsActive { get; set; } = true;
    public string? BannerUrl { get; set; }
    public int EstimatedDurationHours { get; set; }
    public string DifficultyLevel { get; set; } = "Beginner";
    public List<CreatePathwayCourseRequest> Courses { get; set; } = new();
}

public class UpdatePathwayCourseRequest : CreatePathwayCourseRequest
{
    public string? Id { get; set; }
}

public class EnrollLearnerRequest
{
    public string LearnerId { get; set; } = string.Empty;
    public DateTime? TargetCompletionDate { get; set; }
    public string? Notes { get; set; }
}

public class BulkEnrollLearnerRequest
{
    public List<string> LearnerIds { get; set; } = new();
    public DateTime? TargetCompletionDate { get; set; }
    public string? Notes { get; set; }
}

public class LearnerProgressDto
{
    public string LearnerId { get; set; } = string.Empty;
    public string LearnerName { get; set; } = string.Empty;
    public string LearnerEmail { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? TargetCompletionDate { get; set; }
    public string Status { get; set; } = "Not Started";
    public decimal ProgressPercentage { get; set; }
    public int CompletedCourses { get; set; }
    public int TotalCourses { get; set; }
    public int MandatoryCoursesCompleted { get; set; }
    public int TotalMandatoryCourses { get; set; }
    public string? CurrentCourse { get; set; }
    public string? Notes { get; set; }
}

public class PathwayProgressResponse
{
    public List<LearnerProgressDto> Learners { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public PathwayProgressStats Stats { get; set; } = new();
}

public class PathwayProgressStats
{
    public int TotalEnrolled { get; set; }
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int Completed { get; set; }
    public decimal AverageProgressPercentage { get; set; }
    public int CompletedOnTime { get; set; }
    public int Overdue { get; set; }
}