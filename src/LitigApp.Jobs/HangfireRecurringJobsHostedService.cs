using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LitigApp.Jobs;

internal sealed class HangfireRecurringJobsHostedService(
    IServiceProvider services,
    ILogger<HangfireRecurringJobsHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = services.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
            var sweepOpts = scope.ServiceProvider
                .GetRequiredService<IOptions<SweepOptions>>().Value;

            HangfireConfiguration.RegisterRecurringJobs(manager, sweepOpts);

            logger.LogInformation("Hangfire recurring jobs registered successfully.");
        }
        catch (Exception ex)
        {
            // Log but don't crash the host — jobs will register on next restart.
            logger.LogError(ex, "Failed to register Hangfire recurring jobs at startup.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
