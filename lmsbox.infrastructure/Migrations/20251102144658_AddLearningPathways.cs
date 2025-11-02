using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsbox.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningPathways : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearningPathways",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    BannerUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimatedDurationHours = table.Column<int>(type: "int", nullable: false),
                    DifficultyLevel = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Beginner"),
                    OrganisationId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningPathways", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningPathways_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LearningPathways_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LearnerPathwayProgresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LearningPathwayId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompletedCourses = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalCourses = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentCourseId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerPathwayProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearnerPathwayProgresses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LearnerPathwayProgresses_Courses_CurrentCourseId",
                        column: x => x.CurrentCourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LearnerPathwayProgresses_LearningPathways_LearningPathwayId",
                        column: x => x.LearningPathwayId,
                        principalTable: "LearningPathways",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PathwayCourses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LearningPathwayId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CourseId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SequenceOrder = table.Column<int>(type: "int", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PrerequisiteCourseIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PathwayCourses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PathwayCourses_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PathwayCourses_LearningPathways_LearningPathwayId",
                        column: x => x.LearningPathwayId,
                        principalTable: "LearningPathways",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LearnerPathwayProgresses_CurrentCourseId",
                table: "LearnerPathwayProgresses",
                column: "CurrentCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerPathwayProgresses_EnrolledAt",
                table: "LearnerPathwayProgresses",
                column: "EnrolledAt");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerPathwayProgresses_LearningPathwayId",
                table: "LearnerPathwayProgresses",
                column: "LearningPathwayId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerPathwayProgresses_UserId",
                table: "LearnerPathwayProgresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerPathwayProgresses_UserId_LearningPathwayId",
                table: "LearnerPathwayProgresses",
                columns: new[] { "UserId", "LearningPathwayId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningPathways_CreatedAt",
                table: "LearningPathways",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LearningPathways_CreatedByUserId",
                table: "LearningPathways",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningPathways_OrganisationId",
                table: "LearningPathways",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningPathways_OrganisationId_IsActive",
                table: "LearningPathways",
                columns: new[] { "OrganisationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PathwayCourses_CourseId",
                table: "PathwayCourses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_PathwayCourses_LearningPathwayId",
                table: "PathwayCourses",
                column: "LearningPathwayId");

            migrationBuilder.CreateIndex(
                name: "IX_PathwayCourses_LearningPathwayId_CourseId",
                table: "PathwayCourses",
                columns: new[] { "LearningPathwayId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PathwayCourses_LearningPathwayId_SequenceOrder",
                table: "PathwayCourses",
                columns: new[] { "LearningPathwayId", "SequenceOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LearnerPathwayProgresses");

            migrationBuilder.DropTable(
                name: "PathwayCourses");

            migrationBuilder.DropTable(
                name: "LearningPathways");
        }
    }
}
