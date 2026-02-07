using System;

namespace MIC.Core.Domain.Exceptions;

public sealed class DatabaseException : MICException
{
    public DatabaseException(string message, string? userMessage = null, Exception? innerException = null)
        : base(message, "DATABASE_ERROR", userMessage ?? "A database error occurred. Please retry the operation.", innerException)
    {
    }
}

public sealed class SettingsException : MICException
{
    public SettingsException(string message, string? userMessage = null, Exception? innerException = null)
        : base(message, "SETTINGS_ERROR", userMessage ?? "Settings could not be saved. Please retry.", innerException)
    {
    }
}

public sealed class NotificationException : MICException
{
    public NotificationException(string message, string? userMessage = null, Exception? innerException = null)
        : base(message, "NOTIFICATION_ERROR", userMessage ?? "Notification delivery failed.", innerException)
    {
    }
}
