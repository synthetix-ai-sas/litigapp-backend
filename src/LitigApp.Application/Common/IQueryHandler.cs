namespace LitigApp.Application.Common;

public interface IQueryHandler<TQuery, TResult>
{
    Task<Result<TResult>> Handle(TQuery query, CancellationToken ct);
}
