using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Imports;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LitigApp.Infrastructure.Imports;

/// <summary>In-memory <see cref="IImportPreviewCache"/> with the configured TTL (blueprint: 10 min).</summary>
internal sealed class ImportPreviewCache(IMemoryCache cache, IOptions<ImportOptions> options) : IImportPreviewCache
{
    private static string Key(Guid previewId) => $"import-preview:{previewId}";

    public void Set(Guid previewId, ExcelPreview preview) =>
        cache.Set(Key(previewId), preview, TimeSpan.FromMinutes(options.Value.PreviewCacheTtlMinutes));

    public ExcelPreview? Get(Guid previewId) =>
        cache.TryGetValue(Key(previewId), out ExcelPreview? preview) ? preview : null;
}
