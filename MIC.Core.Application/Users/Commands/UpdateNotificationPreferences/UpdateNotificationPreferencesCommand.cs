using MIC.Core.Application.Common.Interfaces;
using ErrorOr;

namespace MIC.Core.Application.Users.Commands.UpdateNotificationPreferences;

/// <summary>
/// Command to update user notification preferences
/// </summary>
public record UpdateNotificationPreferencesCommand : ICommand<bool>
{
    public string UserId { get; init; } = string.Empty;
    public bool EmailNotificationsEnabled { get; init; } = true;
    public bool PushNotificationsEnabled { get; init; } = true;
    public bool AlertNotificationsEnabled { get; init; } = true;
    public bool WeeklyDigestEnabled { get; init; } = true;
}
