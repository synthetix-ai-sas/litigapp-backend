using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LitigApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImportJobDuplicateCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "duplicate_count",
                table: "import_jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "duplicate_count",
                table: "import_jobs");
        }
    }
}
