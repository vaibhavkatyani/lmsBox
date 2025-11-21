using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsbox.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if BrandName column exists before adding it
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE object_id = OBJECT_ID('Organisations') 
                    AND name = 'BrandName'
                )
                BEGIN
                    ALTER TABLE Organisations ADD BrandName nvarchar(max) NULL
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE object_id = OBJECT_ID('Organisations') 
                    AND name = 'BrandName'
                )
                BEGIN
                    ALTER TABLE Organisations DROP COLUMN BrandName
                END
            ");
        }
    }
}
