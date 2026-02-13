using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Users.Common;
using ErrorOr;

namespace MIC.Core.Application.Users.Queries.GetUserProfile;

public class GetUserProfileQueryHandler : IQueryHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserProfileQueryHandler> _logger;

    public GetUserProfileQueryHandler(IUserRepository userRepository, ILogger<GetUserProfileQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ErrorOr<UserProfileDto>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving user profile for user {UserId}", request.UserId);

        try
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                return Error.Validation(code: "User.ValidationFailed", description: "User ID is required");
            }

            // Parse to Guid
            if (!Guid.TryParse(request.UserId, out var userId))
            {
                return Error.Validation(code: "User.ValidationFailed", description: "Invalid user ID format");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User profile not found: {UserId}", request.UserId);
                return Error.NotFound(code: "User.NotFound", description: $"User profile '{request.UserId}' not found");
            }

            var profileDto = new UserProfileDto
            {
                UserId = user.Id.ToString(),
                FirstName = user.FullName?.Split(" ").FirstOrDefault() ?? "",
                LastName = user.FullName?.Split(" ").LastOrDefault() ?? "",
                Email = user.Email
            };

            _logger.LogInformation("User profile retrieved successfully for user {UserId}", request.UserId);
            return profileDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return Error.Unexpected(code: "User.UnexpectedError", description: ex.Message);
        }
    }
}
