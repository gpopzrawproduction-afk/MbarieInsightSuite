using System;
using System.Collections.Generic;

namespace MIC.Core.Domain.Exceptions;

/// <summary>
/// Base exception type for the Mbarie Intelligence Console domain.
/// </summary>
public abstract class MICException : Exception
{
    protected MICException(
        string message,
        string errorCode,
        string? userMessage = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? "MIC_ERROR" : errorCode;
        UserMessage = userMessage;
    }

    /// <summary>
    /// Stable machine-parsable error code for logging and telemetry.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Optional human-friendly message safe for end user display.
    /// </summary>
    public string? UserMessage { get; }

    /// <summary>
    /// Arbitrary metadata aiding diagnostics.
    /// </summary>
    public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    public MICException AddMetadata(string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            Metadata[key] = value;
        }

        return this;
    }
}
