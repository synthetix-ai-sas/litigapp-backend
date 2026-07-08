using LitigApp.Domain.Auth;
using LitigApp.Domain.Catalog;
using LitigApp.Domain.Common;
using LitigApp.Domain.Imports;
using LitigApp.Domain.Notifications;
using LitigApp.Domain.Processes;
using LitigApp.Domain.Users;
using LitigApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LitigApp.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Processes
    public DbSet<Process> Processes { get; set; } = null!;
    public DbSet<ProcessAction> ProcessActions { get; set; } = null!;
    public DbSet<ProcessSubject> ProcessSubjects { get; set; } = null!;

    // Catalog
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<City> Cities { get; set; } = null!;
    public DbSet<Entity> Entities { get; set; } = null!;
    public DbSet<Specialty> Specialties { get; set; } = null!;
    public DbSet<Court> Courts { get; set; } = null!;

    // Notifications
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
    public DbSet<NotificationLog> NotificationLogs { get; set; } = null!;

    // Imports
    public DbSet<ImportJob> ImportJobs { get; set; } = null!;

    // Users
    public DbSet<UserNotificationPreferences> UserNotificationPreferences { get; set; } = null!;

    // Auth
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<LegalAcceptance> LegalAcceptances { get; set; } = null!;

    // Sync
    public DbSet<SyncState> SyncStates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
