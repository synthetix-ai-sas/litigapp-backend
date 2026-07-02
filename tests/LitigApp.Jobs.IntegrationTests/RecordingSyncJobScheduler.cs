using LitigApp.Application.Common.Abstractions;

namespace LitigApp.Jobs.IntegrationTests;

/// <summary>Test double that records what the jobs scheduled, instead of touching Hangfire.</summary>
public sealed class RecordingSyncJobScheduler : ISyncJobScheduler
{
    public int ActionsSweepEnqueued { get; private set; }
    public int ActionsSweepScheduled { get; private set; }
    public TimeSpan? LastScheduleDelay { get; private set; }
    public List<string> NotifiedUserIds { get; } = [];

    public void EnqueueActionsSweep() => ActionsSweepEnqueued++;

    public void ScheduleActionsSweep(TimeSpan delay)
    {
        ActionsSweepScheduled++;
        LastScheduleDelay = delay;
    }

    public void EnqueueUserNotifications(string userId) => NotifiedUserIds.Add(userId);

    public int PartialFetchEnqueued { get; private set; }
    public int PartialFetchScheduled { get; private set; }

    public void EnqueuePartialFetch(Guid processId) => PartialFetchEnqueued++;

    public void SchedulePartialFetch(Guid processId, TimeSpan delay) => PartialFetchScheduled++;
}
