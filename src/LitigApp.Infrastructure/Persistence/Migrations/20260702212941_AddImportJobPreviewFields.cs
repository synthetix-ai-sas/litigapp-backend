using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LitigApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImportJobPreviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "preview_id",
                table: "import_jobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "sync_error",
                table: "import_jobs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "preview_id",
                table: "import_jobs");

            migrationBuilder.DropColumn(
                name: "sync_error",
                table: "import_jobs");
        }
    }
}
