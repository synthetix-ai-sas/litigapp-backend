using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Common.Models;
using LitigApp.Application.Features.Processes.Dtos;
using LitigApp.Application.Features.Processes.Queries.GetProcessById;
using LitigApp.Application.Features.Processes.Queries.ListNovelties;
using LitigApp.Application.Features.Processes.Queries.ListProcesses;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LitigApp.Api.Features.Processes;

public static class ProcessesEndpoints
{
    public static IEndpointRouteBuilder MapProcessesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/processes")
            .RequireAuthorization()
            .WithTags("Processes");

        group.MapGet("/novelties", ListNovelties)
            .WithName("ListNovelties")
            .WithSummary("Lista los procesos con novedades sin atender")
            .Produces<PagedResult<ProcessListItemDto>>();

        group.MapGet("", ListProcesses)
            .WithName("ListProcesses")
            .WithSummary("Lista los procesos del usuario con filtros opcionales")
            .Produces<PagedResult<ProcessListItemDto>>();

        group.MapGet("/{id:guid}", GetProcessById)
            .WithName("GetProcessById")
            .WithSummary("Detalle de un proceso (incluye sujetos y actuaciones)")
            .Produces<ProcessDetailDto>()
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Ok<PagedResult<ProcessListItemDto>>> ListNovelties(
        IQueryHandler<ListNoveltiesQuery, PagedResult<ProcessListItemDto>> handler,
        CancellationToken ct,
        int page = 1,
        int pageSize = 20)
    {
        var result = await handler.HandleAsync(new ListNoveltiesQuery(page, pageSize), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<PagedResult<ProcessListItemDto>>> ListProcesses(
        IQueryHandler<ListProcessesQuery, PagedResult<ProcessListItemDto>> handler,
        CancellationToken ct,
        int page = 1,
        int pageSize = 20,
        string? courtName = null,
        string? fileNumber = null,
        string? subjectName = null,
        string? status = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        bool? attended = null)
    {
        var query = new ListProcessesQuery(
            page, pageSize, courtName, fileNumber, subjectName, status, fromDate, toDate, attended);
        var result = await handler.HandleAsync(query, ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<ProcessDetailDto>, NotFound>> GetProcessById(
        Guid id,
        IQueryHandler<GetProcessByIdQuery, ProcessDetailDto?> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetProcessByIdQuery(id), ct);
        return result is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(result);
    }
}
