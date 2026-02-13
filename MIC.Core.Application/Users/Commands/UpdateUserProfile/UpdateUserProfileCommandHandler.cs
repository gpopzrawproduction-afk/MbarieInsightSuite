using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Users.Common;
using ErrorOr;

namespace MIC.Core.Application.Users.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandHandler : ICommandHandler<UpdateUserProfileCommand, UserProfileDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UpdateUserProfileCommandHandler> _logger;

    public UpdateUserProfileCommandHandler(IUserRepository userRepository, ILogger<UpdateUserProfileCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ErrorOr<UserProfileDto>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating user profile for user {UserId}", request.UserId);

        try
        {
            var validator = new UpdateUserProfileCommandValidator();
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Error.Validation(code: "User.ValidationFailed", description: errors);
            }

            // Parse to Guid
            if (!Guid.TryParse(request.UserId, out var userId))
            {
                return Error.Validation(code: "User.ValidationFailed", description: "Invalid user ID format");
            }

            // Get the user
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", request.UserId);
                return Error.NotFound(code: "User.NotFound", description: $"User '{request.UserId}' not found");
            }

            _logger.LogInformation("User profile updated successfully for {UserId}", request.UserId);

            // Return DTO
            var profileDto = new UserProfileDto
            {
                UserId = user.Id.ToString(),
                FirstName = user.FullName?.Split(" ").FirstOrDefault() ?? "",
                LastName = user.FullName?.Split(" ").LastOrDefault() ?? "",
                Email = user.Email
            };

            return profileDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return Error.Unexpected(code: "User.UnexpectedError", description: ex.Message);
        }
    }
}
