using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using lmsbox.domain.Models;
using lmsbox.infrastructure.Data;

namespace lmsBox.Server.Data;
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        var db = provider.GetRequiredService<ApplicationDbContext>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure simple lookups exist
        if (!db.Organisations.Any())
        {
            var org = new Organisation { Name = "lmsBox-DevOrg", Description = "Development organisation" };
            db.Organisations.Add(org);
            await db.SaveChangesAsync();
        }

        var organisation = db.Organisations.First();

        // Ensure Identity roles
        var roles = new[] { "SuperAdmin", "OrgAdmin", "Learner" };
        foreach (var r in roles)
        {
            if (!await roleManager.RoleExistsAsync(r))
            {
                var res = await roleManager.CreateAsync(new IdentityRole(r));
                if (!res.Succeeded) logger.LogWarning("Failed to create role {Role}: {Errors}", r, string.Join(",", res.Errors.Select(e => e.Description)));
            }
        }

        // Create admin user
        var adminEmail = "admin@dev.local";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = "admin@dev.local",
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Org",
                LastName = "Admin",
                OrganisationID = organisation.Id,
                CreatedBy = "system",
                ActivatedBy = "system",
                DeactivatedBy = "system"
            };
            var createAdmin = await userManager.CreateAsync(admin, "P@ssw0rd1!");
            if (!createAdmin.Succeeded) logger.LogWarning("Admin creation failed: {Errors}", string.Join(",", createAdmin.Errors.Select(e => e.Description)));
            else await userManager.AddToRoleAsync(admin, "OrgAdmin");
        }

        // Create a learner user
        var learnerEmail = "19vaibhav90@gmail.com";
        var learner = await userManager.FindByEmailAsync(learnerEmail);
        if (learner == null)
        {
            learner = new ApplicationUser
            {
                UserName = learnerEmail,
                Email = learnerEmail,
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "Learner",
                OrganisationID = organisation.Id,
                CreatedBy = admin.Id,
                ActivatedBy = admin.Id,
                DeactivatedBy = admin.Id
            };
            var createLearner = await userManager.CreateAsync(learner, "P@ssw0rd1!");
            if (!createLearner.Succeeded) logger.LogWarning("Learner creation failed: {Errors}", string.Join(",", createLearner.Errors.Select(e => e.Description)));
            else await userManager.AddToRoleAsync(learner, "Learner");
        }

        // Create a Course (if not exists)
        if (!db.Courses.Any(c => c.Title == "Getting Started"))
        {
            var course = new Course
            {
                Title = "Getting Started",
                Description = "Introductory course",
                OrganisationId = organisation.Id,
                CreatedByUserId = admin.Id,
                CreatedAt = DateTime.UtcNow
            };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            // Add a lesson
            var lesson = new Lesson
            {
                CourseId = course.Id,
                Title = "Welcome",
                Content = "Welcome to the course",
                Ordinal = 1,
                CreatedByUserId = admin.Id,
                CreatedAt = DateTime.UtcNow
            };
            db.Lessons.Add(lesson);
            await db.SaveChangesAsync();
        }

        var courseEntity = db.Courses.First(c => c.Title == "Getting Started");

        // Create a LearningGroup and map it to the course
        if (!db.LearningGroups.Any(g => g.Name == "Cohort A"))
        {
            var group = new LearningGroup
            {
                Name = "Cohort A",
                Description = "Sample learning group",
                OrganisationId = organisation.Id,
                CreatedByUserId = admin.Id,
                CreatedAt = DateTime.UtcNow
            };
            db.LearningGroups.Add(group);
            await db.SaveChangesAsync();

            db.GroupCourses.Add(new GroupCourse
            {
                LearningGroupId = group.Id,
                CourseId = courseEntity.Id,
                AssignedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            // Add learner to group
            db.LearnerGroups.Add(new LearnerGroup
            {
                UserId = learner.Id,
                LearningGroupId = group.Id,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        logger.LogInformation("Seeding completed.");
    }
}