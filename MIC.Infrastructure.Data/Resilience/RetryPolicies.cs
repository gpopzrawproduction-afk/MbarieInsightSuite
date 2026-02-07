using System;
using System.IO;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace MIC.Infrastructure.Data.Resilience;

/// <summary>
/// Centralized Polly retry policies for infrastructure services.
/// </summary>
internal static class RetryPolicies
{
    public static AsyncRetryPolicy CreateMailConnectivityPolicy(ILogger logger, string operationName, int retryCount = 3)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        return Policy
            .Handle<IOException>()
            .Or<ServiceNotConnectedException>()
            .Or<ServiceNotAuthenticatedException>()
            .Or<ImapProtocolException>()
            .Or<CommandException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (exception, timespan, attempt, context) =>
                {
                    logger.LogWarning(
                        exception,
                        "Retry {Attempt} for {Operation} after {Delay} seconds",
                        attempt,
                        operationName,
                        timespan.TotalSeconds);
                });
    }

    public static AsyncRetryPolicy CreateStandardPolicy(ILogger logger, string operationName, int retryCount = 3)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        return Policy
            .Handle<IOException>()
            .Or<TimeoutException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount,
                attempt => TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt)),
                (exception, timespan, attempt, context) =>
                {
                    logger.LogWarning(
                        exception,
                        "Retry {Attempt} for {Operation} after {Delay} ms",
                        attempt,
                        operationName,
                        timespan.TotalMilliseconds);
                });
    }
}
