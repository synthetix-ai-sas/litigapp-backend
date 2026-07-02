using Hangfire;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Jobs.ProcessSyncJobs;

namespace LitigApp.Jobs;

/// <summary>Hangfire-backed <see cref="ISyncJobScheduler"/>.</summary>
internal sealed class HangfireSyncJobScheduler(IBackgroundJobClient client) : ISyncJobScheduler
{
    public void EnqueueActionsSweep() =>
        client.Enqueue<ActionsSweepJob>(j => j.RunAsync(CancellationToken.None));

    public void ScheduleActionsSweep(TimeSpan delay) =>
        client.Schedule<ActionsSweepJob>(j => j.RunAsync(CancellationToken.None), delay);

    public void EnqueueUserNotifications(string userId) =>
        client.Enqueue<DispatchUserNotificationsJob>(j => j.RunAsync(userId, CancellationToken.None));

    public void EnqueuePartialFetch(Guid processId) =>
        client.Enqueue<CompletePartialFetchJob>(j => j.RunAsync(processId, CancellationToken.None));

    public void SchedulePartialFetch(Guid processId, TimeSpan delay) =>
        client.Schedule<CompletePartialFetchJob>(j => j.RunAsync(processId, CancellationToken.None), delay);

    public void EnqueueBulkImport(Guid importJobId) =>
        client.Enqueue<BulkImportJob>(j => j.RunAsync(importJobId, CancellationToken.None));

    public void ScheduleBulkImport(Guid importJobId, TimeSpan delay) =>
        client.Schedule<BulkImportJob>(j => j.RunAsync(importJobId, CancellationToken.None), delay);

    public void EnqueueImportComplete(Guid importJobId) =>
        client.Enqueue<DispatchImportCompleteJob>(j => j.RunAsync(importJobId, CancellationToken.None));
}
