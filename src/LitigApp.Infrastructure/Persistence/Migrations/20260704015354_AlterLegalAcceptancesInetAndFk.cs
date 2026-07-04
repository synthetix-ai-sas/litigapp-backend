using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LitigApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlterLegalAcceptancesInetAndFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // varchar→inet has no implicit cast in PostgreSQL; drop+add is safe
            // because the column was introduced in the previous migration with no data.
            migrationBuilder.DropColumn(name: "ip_address", table: "legal_acceptances");

            migrationBuilder.AddColumn<IPAddress>(
                name: "ip_address",
                table: "legal_acceptances",
                type: "inet",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_legal_acceptances_asp_net_users_user_id",
                table: "legal_acceptances",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_legal_acceptances_asp_net_users_user_id",
                table: "legal_acceptances");

            migrationBuilder.DropColumn(name: "ip_address", table: "legal_acceptances");

            migrationBuilder.AddColumn<string>(
                name: "ip_address",
                table: "legal_acceptances",
                type: "character varying(45)",
                nullable: true);
        }
    }
}
