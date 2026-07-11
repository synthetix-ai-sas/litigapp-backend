using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LitigApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImportJobPreviewPayload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "preview_payload",
                table: "import_jobs",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "preview_payload",
                table: "import_jobs");
        }
    }
}
