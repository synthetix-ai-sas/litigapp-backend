namespace LitigApp.Application.Common.Models;

/// <summary>Normalizes pagination inputs to safe bounds.</summary>
public static class Paging
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        var p = page < 1 ? 1 : page;
        var size = pageSize switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => pageSize
        };
        return (p, size);
    }
}
