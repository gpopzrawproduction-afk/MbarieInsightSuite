using MIC.Core.Application.Common.Interfaces;
using ErrorOr;

namespace MIC.Core.Application.Users.Commands.Logout;

/// <summary>
/// Command to log out the current user
/// </summary>
public record LogoutCommand : ICommand<bool>
{
    public string UserId { get; init; } = string.Empty;
}
