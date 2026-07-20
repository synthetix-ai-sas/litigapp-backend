using FluentAssertions;
using LitigApp.Application.Features.Imports;
using LitigApp.Infrastructure.Imports;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LitigApp.Infrastructure.UnitTests.Imports;

public class ImportPreviewCacheTests
{
    private static readonly ExcelPreview Sample = new(
        "p.xlsx",
        [new ExcelColumn("A", "Radicado")],
        [new Dictionary<string, string?> { ["A"] = "11001310300120230001200" }]);

    private static ImportPreviewCache BuildCache() =>
        new(new MemoryCache(new MemoryCacheOptions()), Microsoft.Extensions.Options.Options.Create(new ImportOptions()));

    [Fact]
    public void Set_ThenGet_ReturnsSamePreview()
    {
        var cache = BuildCache();
        var id = Guid.NewGuid();

        cache.Set(id, Sample);

        cache.Get(id).Should().BeSameAs(Sample);
    }

    [Fact]
    public void Get_UnknownId_ReturnsNull()
    {
        var cache = BuildCache();

        cache.Get(Guid.NewGuid()).Should().BeNull();
    }

    [Fact]
    public void Entries_AreIsolatedByPreviewId()
    {
        var cache = BuildCache();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        cache.Set(first, Sample);

        cache.Get(first).Should().NotBeNull();
        cache.Get(second).Should().BeNull();
    }
}
