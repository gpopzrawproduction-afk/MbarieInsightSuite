using System;
using System.Threading;
using System.Threading.Tasks;

namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Abstraction for application-wide error handling orchestration.
/// </summary>
public interface IErrorHandlingService
{
    event Action<ErrorContext>? OnError;
    event Action<ErrorContext>? OnCriticalError;

    void HandleException(Exception exception, string? context = null, bool isCritical = false);

    Task<T?> SafeExecuteAsync<T>(Func<Task<T>> operation, string context, T? defaultValue = default, CancellationToken cancellationToken = default);

    Task SafeExecuteAsync(Func<Task> operation, string context, CancellationToken cancellationToken = default);

    T? SafeExecute<T>(Func<T> operation, string context, T? defaultValue = default);
}

/// <summary>
/// Captures details about a handled error for downstream subscribers.
/// </summary>
public sealed class ErrorContext
{
    public required Exception Exception { get; init; }

    public string Context { get; init; } = string.Empty;

    public bool IsCritical { get; init; }

    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    public string? ErrorCode { get; init; }
}
