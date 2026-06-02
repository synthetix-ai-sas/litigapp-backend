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
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    WhatsAppPhone = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    Id = table.Column<short>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "entities",
                columns: table => new
                {
                    Id = table.Column<short>(type: "smallint", nullable: false),
                    Code = table.Column<string>(type: "char(2)", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "import_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    TotalRows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ProcessedRows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SuccessCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ErrorCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    ColumnMapping = table.Column<string>(type: "jsonb", nullable: true),
                    Errors = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notifications_outbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    Attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications_outbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "specialties",
                columns: table => new
                {
                    Id = table.Column<short>(type: "smallint", nullable: false),
                    Code = table.Column<string>(type: "char(2)", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_specialties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sync_state",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    ValueText = table.Column<string>(type: "text", nullable: true),
                    ValueTimestamp = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_state", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "user_notification_preferences",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    WhatsAppEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    QuietHoursStart = table.Column<TimeOnly>(type: "time", nullable: true),
                    QuietHoursEnd = table.Column<TimeOnly>(type: "time", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_notification_preferences", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    DepartmentId = table.Column<short>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cities_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    ProcessIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    ProviderMessageId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    RawResponse = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_logs_notifications_outbox_OutboxId",
                        column: x => x.OutboxId,
                        principalTable: "notifications_outbox",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "courts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OfficialCode = table.Column<string>(type: "char(12)", nullable: false),
                    CityId = table.Column<int>(type: "integer", nullable: false),
                    EntityId = table.Column<short>(type: "smallint", nullable: true),
                    SpecialtyId = table.Column<short>(type: "smallint", nullable: true),
                    CourtNumber = table.Column<short>(type: "smallint", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RawPayload = table.Column<string>(type: "jsonb", nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_courts_cities_CityId",
                        column: x => x.CityId,
                        principalTable: "cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_courts_entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_courts_specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalTable: "specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "processes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    FileNumber = table.Column<string>(type: "char(23)", nullable: false),
                    ExternalProcessId = table.Column<long>(type: "bigint", nullable: true),
                    ExternalConnectionId = table.Column<int>(type: "integer", nullable: true),
                    CourtId = table.Column<Guid>(type: "uuid", nullable: true),
                    FilingYear = table.Column<short>(type: "smallint", nullable: true),
                    ProcessType = table.Column<string>(type: "text", nullable: true),
                    ProcessClass = table.Column<string>(type: "text", nullable: true),
                    ProcessSubclass = table.Column<string>(type: "text", nullable: true),
                    Resource = table.Column<string>(type: "text", nullable: true),
                    JudgeName = table.Column<string>(type: "text", nullable: true),
                    FilingContent = table.Column<string>(type: "text", nullable: true),
                    IsPrivate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CustomAlias = table.Column<string>(type: "text", nullable: true),
                    CurrentStatus = table.Column<string>(type: "text", nullable: true),
                    LastCourtActionAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    LastSyncAttemptAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    LastExternalConsecutive = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SyncStatus = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    SyncPhase = table.Column<string>(type: "text", nullable: false, defaultValue: "pending_initial_full"),
                    SyncError = table.Column<string>(type: "text", nullable: true),
                    SyncAttempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Attended = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processes", x => x.Id);
                    table.CheckConstraint("chk_processes_file_length", "length(file_number) = 23");
                    table.ForeignKey(
                        name: "FK_processes_courts_CourtId",
                        column: x => x.CourtId,
                        principalTable: "courts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "process_actions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalActionId = table.Column<long>(type: "bigint", nullable: false),
                    ConsecutiveNumber = table.Column<int>(type: "integer", nullable: false),
                    ActionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: true),
                    Annotation = table.Column<string>(type: "text", nullable: true),
                    TermStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TermEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RecordedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    HasDocuments = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RuleCode = table.Column<string>(type: "text", nullable: true),
                    GroupedWithId = table.Column<Guid>(type: "uuid", nullable: true),
                    RawPayload = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_process_actions_process_actions_GroupedWithId",
                        column: x => x.GroupedWithId,
                        principalTable: "process_actions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_process_actions_processes_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "process_subjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalSubjectId = table.Column<long>(type: "bigint", nullable: true),
                    SubjectType = table.Column<string>(type: "text", nullable: false),
                    IsSummoned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Identification = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false, defaultValue: "api"),
                    RawPayload = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_subjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_process_subjects_processes_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_cities_dept",
                table: "cities",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "idx_courts_city_spec",
                table: "courts",
                columns: new[] { "CityId", "SpecialtyId" });

            migrationBuilder.CreateIndex(
                name: "idx_courts_name_trgm",
                table: "courts",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_courts_EntityId",
                table: "courts",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "ix_courts_official_code",
                table: "courts",
                column: "OfficialCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_courts_SpecialtyId",
                table: "courts",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "ix_entities_code",
                table: "entities",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_imports_user_created",
                table: "import_jobs",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_notif_logs_user_sent",
                table: "notification_logs",
                columns: new[] { "UserId", "SentAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_OutboxId",
                table: "notification_logs",
                column: "OutboxId");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_status_created",
                table: "notifications_outbox",
                columns: new[] { "Status", "CreatedAt" },
                filter: "status IN ('pending', 'processing')");

            migrationBuilder.CreateIndex(
                name: "idx_actions_process_consec",
                table: "process_actions",
                columns: new[] { "ProcessId", "ConsecutiveNumber" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_actions_process_recorded",
                table: "process_actions",
                columns: new[] { "ProcessId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_process_actions_GroupedWithId",
                table: "process_actions",
                column: "GroupedWithId");

            migrationBuilder.CreateIndex(
                name: "uq_actions_process_external",
                table: "process_actions",
                columns: new[] { "ProcessId", "ExternalActionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_subjects_process_type",
                table: "process_subjects",
                columns: new[] { "ProcessId", "SubjectType" });

            migrationBuilder.CreateIndex(
                name: "idx_processes_external",
                table: "processes",
                column: "ExternalProcessId");

            migrationBuilder.CreateIndex(
                name: "idx_processes_sync_phase",
                table: "processes",
                columns: new[] { "SyncPhase", "LastSyncAttemptAt" },
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "idx_processes_user_active",
                table: "processes",
                columns: new[] { "UserId", "IsActive", "LastCourtActionAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "idx_processes_user_attended",
                table: "processes",
                columns: new[] { "UserId", "Attended", "LastCourtActionAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_processes_CourtId",
                table: "processes",
                column: "CourtId");

            migrationBuilder.CreateIndex(
                name: "uq_processes_user_file",
                table: "processes",
                columns: new[] { "UserId", "FileNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_specialties_code",
                table: "specialties",
                column: "Code",
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
        }
    }
}
