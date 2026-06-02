using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LitigApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"pg_trgm\";");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    whats_app_phone = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_departments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "entities",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    code = table.Column<string>(type: "char(2)", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_entities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "import_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    total_rows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    processed_rows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    success_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    error_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    column_mapping = table.Column<string>(type: "jsonb", nullable: true),
                    errors = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    channel = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications_outbox", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "specialties",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    code = table.Column<string>(type: "char(2)", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_specialties", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sync_state",
                columns: table => new
                {
                    key = table.Column<string>(type: "text", nullable: false),
                    value_text = table.Column<string>(type: "text", nullable: true),
                    value_timestamp = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_state", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "user_notification_preferences",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    email_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    whats_app_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    quiet_hours_start = table.Column<TimeOnly>(type: "time", nullable: true),
                    quiet_hours_end = table.Column<TimeOnly>(type: "time", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_notification_preferences", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_user_claims_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_asp_net_user_logins_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    department_id = table.Column<short>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cities", x => x.id);
                    table.ForeignKey(
                        name: "fk_cities_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outbox_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    channel = table.Column<string>(type: "text", nullable: false),
                    process_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    provider_message_id = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    raw_response = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_logs_outbox_messages_outbox_id",
                        column: x => x.outbox_id,
                        principalTable: "notifications_outbox",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "courts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    official_code = table.Column<string>(type: "char(12)", nullable: false),
                    city_id = table.Column<int>(type: "integer", nullable: false),
                    entity_id = table.Column<short>(type: "smallint", nullable: true),
                    specialty_id = table.Column<short>(type: "smallint", nullable: true),
                    court_number = table.Column<short>(type: "smallint", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    raw_payload = table.Column<string>(type: "jsonb", nullable: true),
                    last_synced_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_courts", x => x.id);
                    table.ForeignKey(
                        name: "fk_courts_cities_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_courts_entities_entity_id",
                        column: x => x.entity_id,
                        principalTable: "entities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_courts_specialties_specialty_id",
                        column: x => x.specialty_id,
                        principalTable: "specialties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "processes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    file_number = table.Column<string>(type: "char(23)", nullable: false),
                    external_process_id = table.Column<long>(type: "bigint", nullable: true),
                    external_connection_id = table.Column<int>(type: "integer", nullable: true),
                    court_id = table.Column<Guid>(type: "uuid", nullable: true),
                    filing_year = table.Column<short>(type: "smallint", nullable: true),
                    process_type = table.Column<string>(type: "text", nullable: true),
                    process_class = table.Column<string>(type: "text", nullable: true),
                    process_subclass = table.Column<string>(type: "text", nullable: true),
                    resource = table.Column<string>(type: "text", nullable: true),
                    judge_name = table.Column<string>(type: "text", nullable: true),
                    filing_content = table.Column<string>(type: "text", nullable: true),
                    is_private = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    custom_alias = table.Column<string>(type: "text", nullable: true),
                    current_status = table.Column<string>(type: "text", nullable: true),
                    last_court_action_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    last_synced_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    last_sync_attempt_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    last_external_consecutive = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    sync_status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    sync_phase = table.Column<string>(type: "text", nullable: false, defaultValue: "pending_initial_full"),
                    sync_error = table.Column<string>(type: "text", nullable: true),
                    sync_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    attended = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processes", x => x.id);
                    table.CheckConstraint("chk_processes_file_length", "length(file_number) = 23");
                    table.ForeignKey(
                        name: "fk_processes_courts_court_id",
                        column: x => x.court_id,
                        principalTable: "courts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "process_actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    process_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_action_id = table.Column<long>(type: "bigint", nullable: false),
                    consecutive_number = table.Column<int>(type: "integer", nullable: false),
                    action_date = table.Column<DateOnly>(type: "date", nullable: true),
                    action = table.Column<string>(type: "text", nullable: true),
                    annotation = table.Column<string>(type: "text", nullable: true),
                    term_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    term_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    recorded_at = table.Column<DateOnly>(type: "date", nullable: true),
                    has_documents = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    rule_code = table.Column<string>(type: "text", nullable: true),
                    grouped_with_id = table.Column<Guid>(type: "uuid", nullable: true),
                    raw_payload = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_process_actions", x => x.id);
                    table.ForeignKey(
                        name: "fk_process_actions_process_actions_grouped_with_id",
                        column: x => x.grouped_with_id,
                        principalTable: "process_actions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_process_actions_processes_process_id",
                        column: x => x.process_id,
                        principalTable: "processes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "process_subjects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    process_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_subject_id = table.Column<long>(type: "bigint", nullable: true),
                    subject_type = table.Column<string>(type: "text", nullable: false),
                    is_summoned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    identification = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false, defaultValue: "api"),
                    raw_payload = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_process_subjects", x => x.id);
                    table.ForeignKey(
                        name: "fk_process_subjects_processes_process_id",
                        column: x => x.process_id,
                        principalTable: "processes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_role_claims_role_id",
                table: "AspNetRoleClaims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_claims_user_id",
                table: "AspNetUserClaims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_logins_user_id",
                table: "AspNetUserLogins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_roles_role_id",
                table: "AspNetUserRoles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_cities_dept",
                table: "cities",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "idx_courts_city_spec",
                table: "courts",
                columns: new[] { "city_id", "specialty_id" });

            migrationBuilder.CreateIndex(
                name: "idx_courts_name_trgm",
                table: "courts",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_courts_entity_id",
                table: "courts",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_courts_official_code",
                table: "courts",
                column: "official_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_courts_specialty_id",
                table: "courts",
                column: "specialty_id");

            migrationBuilder.CreateIndex(
                name: "ix_entities_code",
                table: "entities",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_imports_user_created",
                table: "import_jobs",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_notif_logs_user_sent",
                table: "notification_logs",
                columns: new[] { "user_id", "sent_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_notification_logs_outbox_id",
                table: "notification_logs",
                column: "outbox_id");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_status_created",
                table: "notifications_outbox",
                columns: new[] { "status", "created_at" },
                filter: "status IN ('pending', 'processing')");

            migrationBuilder.CreateIndex(
                name: "idx_actions_process_consec",
                table: "process_actions",
                columns: new[] { "process_id", "consecutive_number" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_actions_process_recorded",
                table: "process_actions",
                columns: new[] { "process_id", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "ix_process_actions_grouped_with_id",
                table: "process_actions",
                column: "grouped_with_id");

            migrationBuilder.CreateIndex(
                name: "uq_actions_process_external",
                table: "process_actions",
                columns: new[] { "process_id", "external_action_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_subjects_process_type",
                table: "process_subjects",
                columns: new[] { "process_id", "subject_type" });

            migrationBuilder.CreateIndex(
                name: "idx_processes_external",
                table: "processes",
                column: "external_process_id");

            migrationBuilder.Sql(
                "CREATE INDEX idx_processes_sync_phase ON processes (sync_phase, last_sync_attempt_at NULLS FIRST) WHERE is_active = true;");

            migrationBuilder.CreateIndex(
                name: "idx_processes_user_active",
                table: "processes",
                columns: new[] { "user_id", "is_active", "last_court_action_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "idx_processes_user_attended",
                table: "processes",
                columns: new[] { "user_id", "attended", "last_court_action_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_processes_court_id",
                table: "processes",
                column: "court_id");

            migrationBuilder.CreateIndex(
                name: "uq_processes_user_file",
                table: "processes",
                columns: new[] { "user_id", "file_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_specialties_code",
                table: "specialties",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "import_jobs");

            migrationBuilder.DropTable(
                name: "notification_logs");

            migrationBuilder.DropTable(
                name: "process_actions");

            migrationBuilder.DropTable(
                name: "process_subjects");

            migrationBuilder.DropTable(
                name: "sync_state");

            migrationBuilder.DropTable(
                name: "user_notification_preferences");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "notifications_outbox");

            migrationBuilder.DropTable(
                name: "processes");

            migrationBuilder.DropTable(
                name: "courts");

            migrationBuilder.DropTable(
                name: "cities");

            migrationBuilder.DropTable(
                name: "entities");

            migrationBuilder.DropTable(
                name: "specialties");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.Sql("DROP EXTENSION IF EXISTS \"pg_trgm\";");
            migrationBuilder.Sql("DROP EXTENSION IF EXISTS \"uuid-ossp\";");
        }
    }
}
