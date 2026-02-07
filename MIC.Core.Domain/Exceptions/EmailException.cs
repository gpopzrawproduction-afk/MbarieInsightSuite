using System;

namespace MIC.Core.Domain.Exceptions;

/// <summary>
/// Base exception for email related operations.
/// </summary>
public class EmailException : MICException
{
    public EmailException(string message, string errorCode = "EMAIL_ERROR", string? userMessage = null, Exception? innerException = null)
        : base(message, errorCode, userMessage, innerException)
    {
    }
}

public sealed class EmailAuthException : EmailException
{
    public EmailAuthException(string message, string? userMessage = null, Exception? innerException = null)
        : base(message, "EMAIL_AUTH_FAILED", userMessage ?? "Failed to authenticate with the email provider.", innerException)
    {
    }
}

public sealed class EmailSyncException : EmailException
{
    public EmailSyncException(string message, string? userMessage = null, Exception? innerException = null)
        : base(message, "EMAIL_SYNC_FAILED", userMessage ?? "Email synchronization failed. The system will retry shortly.", innerException)
    {
    }
}
