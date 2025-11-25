using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsbox.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PostSurveyCompleted",
                table: "LearnerProgresses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostSurveyCompletedAt",
                table: "LearnerProgresses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PostSurveyResponseId",
                table: "LearnerProgresses",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PreSurveyCompleted",
                table: "LearnerProgresses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreSurveyCompletedAt",
                table: "LearnerProgresses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PreSurveyResponseId",
                table: "LearnerProgresses",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPostSurveyMandatory",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPreSurveyMandatory",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "PostCourseSurveyId",
                table: "Courses",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PreCourseSurveyId",
                table: "Courses",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Survey",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SurveyType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganisationId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Survey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Survey_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Survey_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyQuestion",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SurveyId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Options = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    MinRating = table.Column<int>(type: "int", nullable: true),
                    MaxRating = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyQuestion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyQuestion_Survey_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Survey",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyResponse",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SurveyId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CourseId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SurveyType = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyResponse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyResponse_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SurveyResponse_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SurveyResponse_Survey_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Survey",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyQuestionResponse",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SurveyResponseId = table.Column<long>(type: "bigint", nullable: false),
                    SurveyQuestionId = table.Column<long>(type: "bigint", nullable: false),
                    AnswerText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SelectedOptions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RatingValue = table.Column<int>(type: "int", nullable: true),
                    AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyQuestionResponse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyQuestionResponse_SurveyQuestion_SurveyQuestionId",
                        column: x => x.SurveyQuestionId,
                        principalTable: "SurveyQuestion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SurveyQuestionResponse_SurveyResponse_SurveyResponseId",
                        column: x => x.SurveyResponseId,
                        principalTable: "SurveyResponse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_PostCourseSurveyId",
                table: "Courses",
                column: "PostCourseSurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_PreCourseSurveyId",
                table: "Courses",
                column: "PreCourseSurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_CreatedByUserId",
                table: "Survey",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_OrganisationId",
                table: "Survey",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyQuestion_SurveyId",
                table: "SurveyQuestion",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyQuestionResponse_SurveyQuestionId",
                table: "SurveyQuestionResponse",
                column: "SurveyQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyQuestionResponse_SurveyResponseId",
                table: "SurveyQuestionResponse",
                column: "SurveyResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponse_CourseId",
                table: "SurveyResponse",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponse_SurveyId",
                table: "SurveyResponse",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponse_UserId",
                table: "SurveyResponse",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Survey_PostCourseSurveyId",
                table: "Courses",
                column: "PostCourseSurveyId",
                principalTable: "Survey",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Survey_PreCourseSurveyId",
                table: "Courses",
                column: "PreCourseSurveyId",
                principalTable: "Survey",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Survey_PostCourseSurveyId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Survey_PreCourseSurveyId",
                table: "Courses");

            migrationBuilder.DropTable(
                name: "SurveyQuestionResponse");

            migrationBuilder.DropTable(
                name: "SurveyQuestion");

            migrationBuilder.DropTable(
                name: "SurveyResponse");

            migrationBuilder.DropTable(
                name: "Survey");

            migrationBuilder.DropIndex(
                name: "IX_Courses_PostCourseSurveyId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_PreCourseSurveyId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "PostSurveyCompleted",
                table: "LearnerProgresses");

            migrationBuilder.DropColumn(
                name: "PostSurveyCompletedAt",
                table: "LearnerProgresses");

            migrationBuilder.DropColumn(
                name: "PostSurveyResponseId",
                table: "LearnerProgresses");

            migrationBuilder.DropColumn(
                name: "PreSurveyCompleted",
                table: "LearnerProgresses");

            migrationBuilder.DropColumn(
                name: "PreSurveyCompletedAt",
                table: "LearnerProgresses");

            migrationBuilder.DropColumn(
                name: "PreSurveyResponseId",
                table: "LearnerProgresses");

            migrationBuilder.DropColumn(
                name: "IsPostSurveyMandatory",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "IsPreSurveyMandatory",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "PostCourseSurveyId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "PreCourseSurveyId",
                table: "Courses");
        }
    }
}
