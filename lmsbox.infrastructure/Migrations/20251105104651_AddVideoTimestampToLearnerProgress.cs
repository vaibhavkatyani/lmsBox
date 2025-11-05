using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsbox.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoTimestampToLearnerProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VideoTimestamp",
                table: "LearnerProgresses",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoTimestamp",
                table: "LearnerProgresses");
        }
    }
}
