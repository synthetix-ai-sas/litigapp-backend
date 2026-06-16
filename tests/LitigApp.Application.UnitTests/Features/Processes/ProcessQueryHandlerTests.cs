using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Common.Models;
using LitigApp.Application.Features.Processes.Dtos;
using LitigApp.Application.Features.Processes.Queries.GetProcessById;
using LitigApp.Application.Features.Processes.Queries.ListNovelties;
using LitigApp.Application.Features.Processes.Queries.ListProcesses;
using NSubstitute;

namespace LitigApp.Application.UnitTests.Features.Processes;

public class ListNoveltiesHandlerTests
{
    private static PagedResult<ProcessListItemDto> Empty(int page = 1, int size = 20) =>
        new(new List<ProcessListItemDto>(), 0, page, size);

    [Fact]
    public async Task HandleAsync_PassesUserIdAndNormalizedPaging()
    {
        var reader = Substitute.For<IProcessReader>();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns("user-1");
        reader.ListNoveltiesAsync("user-1", 1, 20, Arg.Any<CancellationToken>()).Returns(Empty());

        var handler = new ListNoveltiesHandler(reader, currentUser);
        await handler.HandleAsync(new ListNoveltiesQuery());

        await reader.Received(1).ListNoveltiesAsync("user-1", 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ClampsOutOfRangePaging()
    {
        var reader = Substitute.For<IProcessReader>();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns("user-1");
        reader.ListNoveltiesAsync("user-1", 1, 100, Arg.Any<CancellationToken>()).Returns(Empty(1, 100));

        var handler = new ListNoveltiesHandler(reader, currentUser);
        await handler.HandleAsync(new ListNoveltiesQuery(Page: 0, PageSize: 9999));

        // page 0 -> 1, pageSize 9999 -> 100 (MaxPageSize)
        await reader.Received(1).ListNoveltiesAsync("user-1", 1, 100, Arg.Any<CancellationToken>());
    }
}

public class ListProcessesHandlerTests
{
    [Fact]
    public async Task HandleAsync_MapsQueryFiltersToReaderFilter()
    {
        var reader = Substitute.For<IProcessReader>();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns("user-2");
        reader.ListAsync("user-2", 1, 20, Arg.Any<ProcessListFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProcessListItemDto>(new List<ProcessListItemDto>(), 0, 1, 20));

        var handler = new ListProcessesHandler(reader, currentUser);
        await handler.HandleAsync(new ListProcessesQuery(
            CourtName: "civil", FileNumber: "17001", SubjectName: "perez", Attended: false));

        await reader.Received(1).ListAsync(
            "user-2", 1, 20,
            Arg.Is<ProcessListFilter>(f =>
                f.CourtName == "civil" &&
                f.FileNumber == "17001" &&
                f.SubjectName == "perez" &&
                f.Attended == false),
            Arg.Any<CancellationToken>());
    }
}

public class GetProcessByIdHandlerTests
{
    [Fact]
    public async Task HandleAsync_PassesUserIdAndId()
    {
        var reader = Substitute.For<IProcessReader>();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns("user-3");
        var id = Guid.NewGuid();
        reader.GetByIdAsync("user-3", id, Arg.Any<CancellationToken>()).Returns((ProcessDetailDto?)null);

        var handler = new GetProcessByIdHandler(reader, currentUser);
        var result = await handler.HandleAsync(new GetProcessByIdQuery(id));

        Assert.Null(result);
        await reader.Received(1).GetByIdAsync("user-3", id, Arg.Any<CancellationToken>());
    }
}
