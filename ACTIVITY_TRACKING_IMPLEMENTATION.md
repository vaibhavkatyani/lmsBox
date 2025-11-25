# Activity Tracking Implementation

## Overview
This document describes the implementation of comprehensive activity tracking for the admin dashboard, including lesson completions, course completions, and certificate issuance.

## Features Implemented

### 1. Admin Dashboard Recent Activities
**File:** `AdminDashboardController.cs`

The recent activities endpoint now tracks 4 types of activities:

1. **User Registrations** - New learners joining the platform
2. **Lesson Completions** - Individual lesson completions (filtered by `LessonId != null`)
3. **Course Completions** - Full course completions (filtered by `LessonId == null`)
4. **Certificate Issuance** - Certificates issued after course completion (filtered by `CertificateIssuedAt != null`)

**Query Logic:**
```csharp
// User Registrations
var userRegistrations = await usersQuery
    .OrderByDescending(u => u.CreatedDate)
    .Take(10)
    .Select(u => new { Text = $"{u.FirstName} {u.LastName} registered", Date = u.CreatedDate })
    .ToListAsync();

// Lesson Completions (LessonId != null indicates lesson-level progress)
var lessonCompletions = await learnerProgressQuery
    .Where(lp => lp.Completed && lp.CompletedAt.HasValue && lp.LessonId != null)
    .OrderByDescending(lp => lp.CompletedAt)
    .Take(15)
    .Select(lp => new { 
        Text = $"{lp.User.FirstName} {lp.User.LastName} completed lesson: {lp.Lesson.Title}", 
        Date = lp.CompletedAt 
    })
    .ToListAsync();

// Course Completions (LessonId == null indicates course-level progress)
var courseCompletions = await learnerProgressQuery
    .Where(lp => lp.Completed && lp.CompletedAt.HasValue && lp.LessonId == null)
    .OrderByDescending(lp => lp.CompletedAt)
    .Take(15)
    .Select(lp => new { 
        Text = $"{lp.User.FirstName} {lp.User.LastName} completed course: {lp.Course.Title}", 
        Date = lp.CompletedAt 
    })
    .ToListAsync();

// Certificate Issuances
var certificateIssuances = await learnerProgressQuery
    .Where(lp => lp.CertificateIssuedAt.HasValue && !string.IsNullOrEmpty(lp.CertificateId))
    .OrderByDescending(lp => lp.CertificateIssuedAt)
    .Take(10)
    .Select(lp => new { 
        Text = $"Certificate issued to {lp.User.FirstName} {lp.User.LastName} for course: {lp.Course.Title}", 
        Date = lp.CertificateIssuedAt 
    })
    .ToListAsync();
```

All activities are combined and sorted by date, with the top 25 most recent activities displayed.

### 2. Audit Logging

**Files:** 
- `IAuditLogService.cs` - Interface
- `AuditLogService.cs` - Implementation

Added three new audit log methods:

#### LogLessonCompletion
```csharp
Task LogLessonCompletion(string userId, string userName, string lessonId, string lessonTitle, string courseId, string courseTitle);
```
Logs when a learner completes a lesson. Includes user details, lesson title, and parent course information.

#### LogCourseCompletion
```csharp
Task LogCourseCompletion(string userId, string userName, string courseId, string courseTitle);
```
Logs when a learner completes an entire course. Includes user details and course information.

#### LogCertificateIssuance
```csharp
Task LogCertificateIssuance(string userId, string userName, string courseId, string courseTitle, string certificateId);
```
Logs when a certificate is issued. Performed by "System" and includes certificate ID for reference.

### 3. Integration Points

#### CoursesController - UpdateLessonProgress
**Location:** Line ~415

```csharp
if (request.Completed && !progress.Completed)
{
    progress.Completed = true;
    progress.CompletedAt = DateTime.UtcNow;
    
    // Log lesson completion to audit log
    var user = await _context.Users.FindAsync(userId);
    var lesson = await _context.Lessons.FindAsync(lessonId);
    var course = await _context.Courses.FindAsync(courseId);
    if (user != null && lesson != null && course != null)
    {
        await _auditLogService.LogLessonCompletion(
            userId, 
            $"{user.FirstName} {user.LastName}", 
            lessonId.ToString(), 
            lesson.Title, 
            courseId, 
            course.Title
        );
    }
}
```

#### CoursesController - UpdateCourseProgress
**Location:** Line ~539

```csharp
if (allCompleted && !courseProgress.Completed)
{
    courseProgress.Completed = true;
    courseProgress.CompletedAt = DateTime.UtcNow;
    
    _logger.LogInformation("Course {CourseId} marked as completed for user {UserId} at {CompletedAt}", 
        courseId, userId, courseProgress.CompletedAt);
    
    // Log course completion to audit log
    var user = await _context.Users.FindAsync(userId);
    var course = await _context.Courses.FindAsync(courseId);
    if (user != null && course != null)
    {
        await _auditLogService.LogCourseCompletion(
            userId, 
            $"{user.FirstName} {user.LastName}", 
            courseId, 
            course.Title
        );
    }
    
    // Update pathway progress if this course is part of any pathways
    await UpdatePathwayProgress(userId, courseId);
}
```

#### CertificateService - GenerateAndSaveCertificateAsync
**Location:** Line ~307

```csharp
// Save certificate details to database for tracking
progress.CertificateUrl = certificateUrl;
progress.CertificateId = certificateId;
progress.CertificateIssuedAt = DateTime.UtcNow;
progress.CertificateIssuedBy = "System";
await _context.SaveChangesAsync();

// Log certificate issuance to audit log
var user = await _context.Users.FindAsync(userId);
var course = await _context.Courses.FindAsync(courseId);
if (user != null && course != null)
{
    await _auditLogService.LogCertificateIssuance(
        userId, 
        $"{user.FirstName} {user.LastName}", 
        courseId, 
        course.Title, 
        certificateId
    );
}
```

## Duplicate Prevention

### Database Level
**Unique Constraint:** `IX_LearnerProgresses_UserId_CourseId_LessonId`

Prevents duplicate progress records at the database level.

### Application Level

#### Lesson Completion
```csharp
if (request.Completed && !progress.Completed)
```
Only marks as completed if not already completed.

#### Course Completion
```csharp
if (allCompleted && !courseProgress.Completed)
```
Only marks course as completed if all lessons complete AND not already marked complete.

#### Certificate Issuance
```csharp
// Check if certificate already exists
if (!string.IsNullOrEmpty(progress.CertificateUrl))
{
    _logger.LogInformation("Certificate already generated, returning existing URL");
    return progress.CertificateUrl;
}
```
Returns existing certificate URL if already generated, preventing duplicate issuance.

## Testing Checklist

- [ ] Verify lesson completions appear in admin dashboard recent activities
- [ ] Verify course completions appear in admin dashboard recent activities
- [ ] Verify certificate issuances appear in admin dashboard recent activities
- [ ] Verify audit logs are created for lesson completions
- [ ] Verify audit logs are created for course completions
- [ ] Verify audit logs are created for certificate issuances
- [ ] Verify duplicate lesson completion attempts are prevented
- [ ] Verify duplicate course completion attempts are prevented
- [ ] Verify duplicate certificate issuance attempts are prevented
- [ ] Verify activities are sorted by most recent first
- [ ] Verify organisation filtering works for OrgAdmin role

## Database Schema

### LearnerProgress Table
- `UserId` - Learner identifier
- `CourseId` - Course identifier
- `LessonId` - Lesson identifier (NULL for course-level progress)
- `Completed` - Boolean completion flag
- `CompletedAt` - Timestamp of completion
- `CertificateUrl` - Azure Blob Storage URL with SAS token
- `CertificateId` - Unique certificate identifier
- `CertificateIssuedAt` - Timestamp of certificate issuance
- `CertificateIssuedBy` - Who issued the certificate (e.g., "System")

**Unique Constraint:** (UserId, CourseId, LessonId)

### AuditLog Table
- `Id` - Primary key
- `UserId` - User who performed the action
- `PerformedBy` - Name of user or "System"
- `Action` - Description of action (e.g., "Lesson Completed: Introduction to React")
- `EntityType` - Type of entity (e.g., "Lesson", "Course", "Certificate")
- `EntityId` - ID of the entity
- `Details` - JSON details of the action
- `Timestamp` - When the action occurred

## API Response Format

### Admin Dashboard Recent Activities
```json
{
  "recentActivities": [
    {
      "text": "John Doe completed lesson: Introduction to React",
      "date": "2025-01-14T10:30:00Z"
    },
    {
      "text": "Jane Smith completed course: React Fundamentals",
      "date": "2025-01-14T09:15:00Z"
    },
    {
      "text": "Certificate issued to Jane Smith for course: React Fundamentals",
      "date": "2025-01-14T09:16:00Z"
    },
    {
      "text": "Bob Johnson registered",
      "date": "2025-01-14T08:00:00Z"
    }
  ]
}
```

## Notes

1. **Performance:** All queries use `AsNoTracking()` for read-only operations to improve performance.

2. **Scalability:** The system takes only the top 10-15 records from each activity type before combining, preventing excessive data retrieval.

3. **Organization Filtering:** OrgAdmin users only see activities for their organization, while SuperAdmin sees all activities.

4. **Audit Trail:** Every completion and certificate issuance is logged to the audit log table for compliance and reporting.

5. **Idempotency:** All completion operations are idempotent - attempting to complete an already completed item has no effect.

## Future Enhancements

- [ ] Add pagination to recent activities if needed for very high-traffic systems
- [ ] Add activity filtering by date range
- [ ] Add activity filtering by type (lessons only, courses only, etc.)
- [ ] Add export functionality for activity reports
- [ ] Add real-time notifications for new activities (SignalR)
