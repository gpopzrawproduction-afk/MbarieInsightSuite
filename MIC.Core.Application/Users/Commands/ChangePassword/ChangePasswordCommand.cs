using MIC.Core.Application.Common.Interfaces;
using ErrorOr;

namespace MIC.Core.Application.Users.Commands.ChangePassword;

/// <summary>
/// Command to change the current user's password
/// </summary>
public record ChangePasswordCommand : ICommand<bool>
{
    public string UserId { get; init; } = string.Empty;
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
}
