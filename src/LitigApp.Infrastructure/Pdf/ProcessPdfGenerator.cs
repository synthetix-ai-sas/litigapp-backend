using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Processes.Dtos;
using QuestPDF.Fluent;

namespace LitigApp.Infrastructure.Pdf;

internal sealed class ProcessPdfGenerator : IProcessPdfGenerator
{
    public byte[] Generate(ProcessDetailDto process) =>
        new ProcessReportDocument(process).GeneratePdf();
}
