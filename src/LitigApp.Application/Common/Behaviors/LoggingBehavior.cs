using System.Diagnostics;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Common;
using Microsoft.Extensions.Logging;

namespace LitigApp.Application.Common.Behaviors;

public sealed class LoggingBehavior<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    ILogger<LoggingBehavior<TCommand, TResponse>> logger)
    : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken ct = default)
    {
        var commandName = typeof(TCommand).Name;
        var sw = Stopwatch.StartNew();

        logger.LogDebug("Handling {CommandName}", commandName);

        try
        {
            var result = await inner.HandleAsync(command, ct);

            sw.Stop();

            if (result.IsSuccess)
                logger.LogInformation("Handled {CommandName} in {Elapsed}ms", commandName, sw.ElapsedMilliseconds);
            else
                logger.LogWarning("Handled {CommandName} with error {Error} in {Elapsed}ms",
                    commandName, result.Error, sw.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Exception handling {CommandName} after {Elapsed}ms",
                commandName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
