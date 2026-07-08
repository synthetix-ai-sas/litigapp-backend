using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LitigApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLegalAcceptances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "legal_acceptances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    document_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    document_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_legal_acceptances", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_legal_acceptances_user_id",
                table: "legal_acceptances",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "legal_acceptances");
        }
    }
}
