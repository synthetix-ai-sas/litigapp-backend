using LitigApp.Application.Features.Notifications;
using LitigApp.Application.Features.Notifications.Dtos;

namespace LitigApp.Application.UnitTests.Features.Notifications;

public class DigestPayloadBuilderTests
{
    [Fact]
    public void SevenProcesses_MaxRowsFive_Shows5_Remaining2()
    {
        var changed = MakeProcesses(7);

        var result = DigestPayloadBuilder.Build(changed, maxRows: 5);

        Assert.Equal(5, result.Shown.Count);
        Assert.Equal(2, result.Remaining);
        Assert.Equal(7, result.Total);
    }

    [Fact]
    public void ThreeProcesses_MaxRowsFive_ShowsAll_NoRemaining()
    {
        var changed = MakeProcesses(3);

        var result = DigestPayloadBuilder.Build(changed, maxRows: 5);

        Assert.Equal(3, result.Shown.Count);
        Assert.Equal(0, result.Remaining);
        Assert.Equal(3, result.Total);
    }

    [Fact]
    public void DoesNotReorder_TrustsCallerOrdering()
    {
        var changed = MakeProcesses(3);

        var result = DigestPayloadBuilder.Build(changed, maxRows: 5);

        Assert.Equal(changed, result.Shown);
    }

    [Fact]
    public void ZeroChanged_ShowsNone_RemainingZero()
    {
        var result = DigestPayloadBuilder.Build([], maxRows: 5);

        Assert.Empty(result.Shown);
        Assert.Equal(0, result.Remaining);
        Assert.Equal(0, result.Total);
    }

    private static List<ChangedProcessDto> MakeProcesses(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new ChangedProcessDto(
                Guid.NewGuid(), $"1700140030102024000000{i}",
                DateTimeOffset.UtcNow.AddDays(-i), "Fijacion estado", "nota"))
            .ToList();
}
