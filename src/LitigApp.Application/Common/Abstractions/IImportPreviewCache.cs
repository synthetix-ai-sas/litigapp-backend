using LitigApp.Application.Features.Imports;

namespace LitigApp.Application.Common.Abstractions;

/// <summary>
/// Short-lived in-memory store for a parsed preview between /preview and /imports (execute).
/// API-local (the execute call runs in the same process and extracts the rows for the job).
/// </summary>
public interface IImportPreviewCache
{
    void Set(Guid previewId, ExcelPreview preview);

    ExcelPreview? Get(Guid previewId);
}
