using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Users.Common;
using ErrorOr;

namespace MIC.Core.Application.Users.Commands.UpdateUserProfile;

/// <summary>
/// Command to update the current user's profile information
/// </summary>
public record UpdateUserProfileCommand : ICommand<UserProfileDto>
{
    public string UserId { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Department { get; init; }
}
