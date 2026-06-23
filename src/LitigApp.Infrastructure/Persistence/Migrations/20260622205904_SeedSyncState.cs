using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LitigApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedSyncState : Migration
    {
        // Fixed seed timestamp — migrations must be deterministic (no DateTime.UtcNow).
        private static readonly DateTimeOffset SeededAt =
            new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Global sync-engine control rows (blueprint §10 / sync_state seed).
            // Mutated at runtime by the sweep jobs — seeded once here, NOT via HasData.
            migrationBuilder.InsertData(
                table: "sync_state",
                columns: ["key", "value_text", "value_timestamp", "reason", "updated_at"],
                values: new object[,]
                {
                    { "waf_blocked_until", null, null, "WAF cooldown deadline (null = not blocked)", SeededAt },
                    { "current_overview_throttle_seconds", "3", null, "adaptive overview throttle (seconds)", SeededAt },
                    { "current_actions_throttle_seconds", "3", null, "adaptive actions throttle (seconds)", SeededAt },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "sync_state",
                keyColumn: "key",
                keyValues: ["waf_blocked_until", "current_overview_throttle_seconds", "current_actions_throttle_seconds"]);
        }
    }
}
