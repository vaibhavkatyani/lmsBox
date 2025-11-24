using SkiaSharp;
using lmsbox.infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using lmsbox.domain.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace lmsBox.Server.Services
{
    public interface ICertificateService
    {
        Task<string> GenerateAndSaveCertificateAsync(string userId, string courseId);
        Task<string?> GetCertificateUrlAsync(string userId, string courseId);
        Task<byte[]> GenerateCertificatePdfAsync(string userId, string courseId);
        Task<string> GetCertificateIdAsync(string userId, string courseId);
    }

    public class CertificateService : ICertificateService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<CertificateService> _logger;
        private readonly IAzureBlobService _blobService;

        public CertificateService(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            ILogger<CertificateService> logger,
            IAzureBlobService blobService)
        {
            _context = context;
            _env = env;
            _logger = logger;
            _blobService = blobService;
        }

        public async Task<string> GetCertificateIdAsync(string userId, string courseId)
        {
            // Generate a unique certificate ID based on user, course, and completion
            var progress = await _context.LearnerProgresses
                .FirstOrDefaultAsync(lp => lp.UserId == userId && lp.CourseId == courseId && lp.Completed);

            if (progress == null || progress.CompletedAt == null)
            {
                throw new InvalidOperationException("Course not completed");
            }

            // Format: C{CourseId}{UserId-Short}{CompletedAt-Timestamp}
            var userShort = userId.Substring(0, Math.Min(8, userId.Length));
            var timestamp = progress.CompletedAt.Value.ToString("yyMMddHHmmss");
            var courseShort = courseId.Substring(0, Math.Min(8, courseId.Length));
            return $"C-{courseShort}-{userShort}-{timestamp}";
        }

        private async Task<(SKBitmap bitmap, string certificateId, ApplicationUser user, Course course, LearnerProgress progress, Organisation organization)> BuildCertificateBitmapAsync(string userId, string courseId)
        {
            _logger.LogInformation("Building certificate bitmap for user {UserId}, course {CourseId}", userId, courseId);
            
            // Get user, course, and organization details
            var user = await _context.Users
                .Include(u => u.Organisation)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogError("User not found: {UserId}", userId);
                throw new NotFoundException("User not found");
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
            {
                _logger.LogError("Course not found: {CourseId}", courseId);
                throw new NotFoundException("Course not found");
            }

            if (!course.CertificateEnabled)
            {
                _logger.LogWarning("Certificate not enabled for course: {CourseId}", courseId);
                throw new InvalidOperationException("Certificate not enabled for this course");
            }

            var progress = await _context.LearnerProgresses
                .FirstOrDefaultAsync(lp => lp.UserId == userId && lp.CourseId == courseId && lp.LessonId == null && lp.Completed);

            if (progress == null || !progress.Completed || progress.CompletedAt == null)
            {
                _logger.LogWarning("Course not completed for user {UserId}, course {CourseId}. Progress: {Progress}", userId, courseId, progress != null);
                throw new InvalidOperationException("Course not completed");
            }

            var organization = user.Organisation;
            if (organization == null)
            {
                _logger.LogError("User {UserId} has no organization", userId);
                throw new InvalidOperationException("User has no organization");
            }

            var certificateId = await GetCertificateIdAsync(userId, courseId);

            // Load template image
            var templatePath = Path.Combine(_env.ContentRootPath, "Assets", "certificate-template.jpg");
            _logger.LogInformation("Looking for certificate template at: {Path}", templatePath);
            
            if (!File.Exists(templatePath))
            {
                _logger.LogError("Certificate template not found at: {Path}. ContentRootPath: {Root}", templatePath, _env.ContentRootPath);
                throw new FileNotFoundException($"Certificate template not found at {templatePath}");
            }

            using var originalImage = SKBitmap.Decode(templatePath);
            if (originalImage == null)
            {
                _logger.LogError("Failed to decode certificate template image at: {Path}", templatePath);
                throw new InvalidOperationException("Failed to decode certificate template image");
            }
            
            var width = originalImage.Width;
            var height = originalImage.Height;
            _logger.LogInformation("Certificate template loaded: {Width}x{Height}", width, height);

            var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(originalImage, 0, 0);

            // Define fonts - matching template style with Albert Sans font family
            // Using Bold weight to match the template's pre-populated text style
            var orgNameFont = new SKFont(SKTypeface.FromFamilyName("Albert Sans", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 48);
            var learnerNameFont = new SKFont(SKTypeface.FromFamilyName("Albert Sans", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 56);
            var courseNameFont = new SKFont(SKTypeface.FromFamilyName("Albert Sans", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 48);
            var dateFont = new SKFont(SKTypeface.FromFamilyName("Albert Sans", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 48);
            var certIdFont = new SKFont(SKTypeface.FromFamilyName("Albert Sans", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 40);

            // Define paint - navy blue matching "This is to certify that" text
            var navyPaint = new SKPaint { Color = new SKColor(27, 54, 93), IsAntialias = true }; // Navy blue #1b365d

            // Organization name (top right area, next to logo space)
            canvas.DrawText(organization.Name, 110, height - 1800, SKTextAlign.Left, orgNameFont, navyPaint);

            // Learner name (directly below "This is to certify that" line)
            var learnerName = $"{user.FirstName} {user.LastName}";
            canvas.DrawText(learnerName, 110, height - 1400, SKTextAlign.Left, learnerNameFont, navyPaint);

            // Course name (directly below "has successfully completed the course" line)
            var courseName = course.Title;
            canvas.DrawText(courseName, 110, height - 1050, SKTextAlign.Left, courseNameFont, navyPaint);

            // Completion date (directly below "Completed on" line)
            var completionDate = progress.CompletedAt.Value.ToString("dd MMMM yyyy");
            canvas.DrawText(completionDate, 110, height - 700, SKTextAlign.Left, dateFont, navyPaint);

            // Certificate ID (directly below "Certificate ID:" line at bottom)
            canvas.DrawText(certificateId, 110, height - 250, SKTextAlign.Left, certIdFont, navyPaint);

            // Draw company logo if available
            if (!string.IsNullOrEmpty(organization.BannerUrl))
            {
                try
                {
                    using var httpClient = new HttpClient();
                    var logoBytes = await httpClient.GetByteArrayAsync(organization.BannerUrl);
                    using var logoStream = new MemoryStream(logoBytes);
                    using var logoBitmap = SKBitmap.Decode(logoStream);

                    // Draw logo in top right corner (scaled to fit)
                    var logoMaxWidth = 300;
                    var logoMaxHeight = 140;
                    var logoScale = Math.Min((float)logoMaxWidth / logoBitmap.Width, (float)logoMaxHeight / logoBitmap.Height);
                    var logoWidth = (int)(logoBitmap.Width * logoScale);
                    var logoHeight = (int)(logoBitmap.Height * logoScale);

                    var logoRect = new SKRect(width - logoWidth - 80, 80, width - 80, 80 + logoHeight);
                    canvas.DrawBitmap(logoBitmap, logoRect);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load company logo for certificate");
                }
            }
            var snapshot = surface.Snapshot();
            var bitmap = SKBitmap.FromImage(snapshot);
            surface.Dispose();
            snapshot.Dispose();
            return (bitmap, certificateId, user, course, progress, organization);
        }

        public async Task<byte[]> GenerateCertificateImageAsync(string userId, string courseId)
        {
            var (bitmap, certificateId, user, course, progress, organization) = await BuildCertificateBitmapAsync(userId, courseId);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 95);
            return data.ToArray();
        }

        public async Task<byte[]> GenerateCertificatePdfAsync(string userId, string courseId)
        {
            var (bitmap, certificateId, user, course, progress, organization) = await BuildCertificateBitmapAsync(userId, courseId);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 95);
            var pngBytes = data.ToArray();

            // Build PDF using QuestPDF - A4 Portrait
            QuestPDF.Settings.License = LicenseType.Community;
            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4); // Portrait by default
                    page.Margin(0);
                    page.Content().Image(pngBytes).FitArea();
                });
            }).GeneratePdf();

            return pdfBytes;
        }

        public async Task<string?> GetCertificateUrlAsync(string userId, string courseId)
        {
            var progress = await _context.LearnerProgresses
                .FirstOrDefaultAsync(lp => lp.UserId == userId && lp.CourseId == courseId && lp.LessonId == null);

            return progress?.CertificateUrl;
        }

        public async Task<string> GenerateAndSaveCertificateAsync(string userId, string courseId)
        {
            try
            {
                // Check if certificate already exists - prevent duplicates
                var progress = await _context.LearnerProgresses
                    .Include(lp => lp.User)
                    .ThenInclude(u => u!.Organisation)
                    .Include(lp => lp.Course)
                    .FirstOrDefaultAsync(lp => lp.UserId == userId && lp.CourseId == courseId && lp.LessonId == null);

                if (progress == null)
                {
                    _logger.LogWarning("Course progress not found for user {UserId}, course {CourseId}", userId, courseId);
                    throw new InvalidOperationException("Course progress not found");
                }

                if (!progress.Completed)
                {
                    _logger.LogWarning("Course not completed for user {UserId}, course {CourseId}", userId, courseId);
                    throw new InvalidOperationException("Course not completed");
                }
                
                // Check certificate enabled on course
                if (progress.Course != null && !progress.Course.CertificateEnabled)
                {
                    _logger.LogWarning("Certificates not enabled for course {CourseId}", courseId);
                    throw new InvalidOperationException("Certificates are not enabled for this course");
                }

                // Return existing certificate URL if already generated - NO DUPLICATES
                // But regenerate if the URL looks invalid (wrong path structure)
                if (!string.IsNullOrEmpty(progress.CertificateUrl) && 
                    !string.IsNullOrEmpty(progress.CertificateId) &&
                    progress.CertificateUrl.Contains($"/organisations/{progress.User?.OrganisationID}/certificates/"))
                {
                    _logger.LogInformation("Certificate already exists for user {UserId}, course {CourseId}: {CertificateId}", 
                        userId, courseId, progress.CertificateId);
                    return progress.CertificateUrl;
                }

                // Clear old/invalid certificate data if exists
                if (!string.IsNullOrEmpty(progress.CertificateUrl))
                {
                    _logger.LogWarning("Clearing invalid certificate data for user {UserId}, course {CourseId}. Old URL: {OldUrl}", 
                        userId, courseId, progress.CertificateUrl);
                    progress.CertificateUrl = null;
                    progress.CertificateId = null;
                    progress.CertificateIssuedAt = null;
                    progress.CertificateIssuedBy = null;
                }

                // Generate certificate PDF
                _logger.LogInformation("Generating certificate for user {UserId}, course {CourseId}", userId, courseId);
                var pdfBytes = await GenerateCertificatePdfAsync(userId, courseId);
                var certificateId = await GetCertificateIdAsync(userId, courseId);
                var organisationId = progress.User?.OrganisationID;

                if (organisationId == null)
                {
                    _logger.LogError("User {UserId} has no organization", userId);
                    throw new InvalidOperationException("User has no organization");
                }

                // Upload to Azure Blob Storage - same structure as library content
                // Path: organisations/{orgId}/certificates/{fileName}
                var fileName = $"certificate_{certificateId}.pdf";
                var orgIdString = organisationId.ToString()!;
                var folderPath = $"organisations/{orgIdString}";

                _logger.LogInformation("Uploading certificate for organisation {OrgId}, file {FileName}", orgIdString, fileName);
                using var stream = new MemoryStream(pdfBytes);
                var blobUrl = await _blobService.UploadToCustomPathAsync(stream, fileName, folderPath, "application/pdf", "certificates");
                
                // Generate SAS URL with 10 year expiry for certificates (they should be permanently accessible)
                var certificateUrl = await _blobService.GetSasUrlAsync(blobUrl, expiryHours: 87600); // 10 years

                // Save certificate details to database for tracking
                progress.CertificateUrl = certificateUrl;
                progress.CertificateId = certificateId;
                progress.CertificateIssuedAt = DateTime.UtcNow;
                progress.CertificateIssuedBy = "System";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Certificate generated and saved for user {UserId}, course {CourseId}: {Url}", userId, courseId, certificateUrl);

                return certificateUrl;
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw business logic exceptions
            }
            catch (NotFoundException)
            {
                throw; // Re-throw not found exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error generating certificate for user {UserId}, course {CourseId}. Error: {Message}", userId, courseId, ex.Message);
                throw new InvalidOperationException($"Failed to generate certificate: {ex.Message}", ex);
            }
        }
    }

    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}
