using LitigApp.Domain.Processes;

namespace LitigApp.Application.Common.Abstractions;

public interface IProcessRepository
{
    Task<List<Process>> GetEligibleForOverviewSweepAsync(
        int batchSize,
        TimeSpan minimumTimeBetweenSyncs,
        CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
