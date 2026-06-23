using LitigApp.Application.Features.Processes.Dtos;

namespace LitigApp.Application.Common.Abstractions;

/// <summary>
/// Renders a process detail into a printable PDF. Implemented in Infrastructure (QuestPDF).
/// </summary>
public interface IProcessPdfGenerator
{
    /// <summary>Generates the PDF bytes for a process report.</summary>
    byte[] Generate(ProcessDetailDto process);
}
