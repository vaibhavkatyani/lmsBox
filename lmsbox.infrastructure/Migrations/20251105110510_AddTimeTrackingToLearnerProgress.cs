using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsbox.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeTrackingToLearnerProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SessionStartTime",
                table: "LearnerProgresses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalTimeSpentSeconds",
                table: "LearnerProgresses",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionStartTime",
                table: "LearnerProgresses");

            migrationBuilder.DropColumn(
                name: "TotalTimeSpentSeconds",
                table: "LearnerProgresses");
        }
    }
}
