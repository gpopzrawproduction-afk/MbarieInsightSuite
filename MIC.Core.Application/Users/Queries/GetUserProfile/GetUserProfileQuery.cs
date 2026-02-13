using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Users.Common;
using ErrorOr;

namespace MIC.Core.Application.Users.Queries.GetUserProfile;

/// <summary>
/// Query to retrieve the current user's profile
/// </summary>
public record GetUserProfileQuery : IQuery<UserProfileDto>
{
    public string UserId { get; init; } = string.Empty;
}
