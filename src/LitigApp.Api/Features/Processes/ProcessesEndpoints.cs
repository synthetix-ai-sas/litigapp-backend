using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Common.Models;
using LitigApp.Application.Features.Processes;
using LitigApp.Application.Features.Processes.Commands.CreateFromFileNumber;
using LitigApp.Application.Features.Processes.Commands.CreateFromWizard;
using LitigApp.Application.Features.Processes.Commands.MarkAttended;
using LitigApp.Application.Features.Processes.Commands.SoftDelete;
using LitigApp.Application.Features.Processes.Dtos;
using LitigApp.Application.Features.Processes.Queries.GetProcessById;
using LitigApp.Application.Features.Processes.Queries.ListNovelties;
using LitigApp.Application.Features.Processes.Queries.ListProcesses;
using LitigApp.Domain.Common;
using LitigApp.Domain.Processes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

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

        group.MapPost("/full-number", CreateFromFileNumber)
            .WithName("CreateProcessFromFileNumber")
            .WithSummary("Crea un proceso con el radicado de 23 dígitos (síncrono)")
            .Produces<ProcessDetailDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/wizard", CreateFromWizard)
            .WithName("CreateProcessFromWizard")
            .WithSummary("Crea un proceso componiendo el radicado (depto/ciudad/despacho/año/consecutivo)")
            .Produces<ProcessDetailDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/mark-attended", MarkAttended)
            .WithName("MarkProcessAttended")
            .WithSummary("Marca un proceso como atendido")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", SoftDelete)
            .WithName("SoftDeleteProcess")
            .WithSummary("Elimina (soft delete) un proceso")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/pdf", DownloadPdf)
            .WithName("DownloadProcessPdf")
            .WithSummary("Descarga el PDF del proceso (409 si sync_status != 'ok')")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

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

    private static async Task<IResult> CreateFromFileNumber(
        [FromBody] CreateProcessFromFileNumberCommand command,
        ICommandHandler<CreateProcessFromFileNumberCommand, ProcessDetailDto> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(command, ct);
        return CreatedOrProblem(result);
    }

    private static async Task<IResult> CreateFromWizard(
        [FromBody] CreateProcessFromWizardCommand command,
        ICommandHandler<CreateProcessFromWizardCommand, ProcessDetailDto> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(command, ct);
        return CreatedOrProblem(result);
    }

    private static async Task<IResult> MarkAttended(
        Guid id,
        ICommandHandler<MarkAttendedCommand, Unit> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new MarkAttendedCommand(id), ct);
        return result.IsSuccess ? TypedResults.NoContent() : ProcessProblem.From(result.Error);
    }

    private static async Task<IResult> SoftDelete(
        Guid id,
        ICommandHandler<SoftDeleteProcessCommand, Unit> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new SoftDeleteProcessCommand(id), ct);
        return result.IsSuccess ? TypedResults.NoContent() : ProcessProblem.From(result.Error);
    }

    private static async Task<IResult> DownloadPdf(
        Guid id,
        IQueryHandler<GetProcessByIdQuery, ProcessDetailDto?> handler,
        IProcessPdfGenerator pdfGenerator,
        CancellationToken ct)
    {
        var process = await handler.HandleAsync(new GetProcessByIdQuery(id), ct);
        if (process is null)
            return TypedResults.NotFound();

        // Guard: the PDF must reflect complete, synced data.
        if (process.SyncStatus != ProcessSyncStatus.Ok)
            return ProcessProblem.From(ProcessErrorCodes.ProcessDataIncomplete);

        var bytes = pdfGenerator.Generate(process);
        return TypedResults.File(bytes, "application/pdf", $"proceso-{process.FileNumber}.pdf");
    }

    private static IResult CreatedOrProblem(Result<ProcessDetailDto> result) =>
        result.IsSuccess
            ? TypedResults.Created((string?)null, result.Value)
            : ProcessProblem.From(result.Error);
}
