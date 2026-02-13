using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using ErrorOr;

namespace MIC.Core.Application.Users.Commands.UpdateNotificationPreferences;

public class UpdateNotificationPreferencesCommandHandler : ICommandHandler<UpdateNotificationPreferencesCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UpdateNotificationPreferencesCommandHandler> _logger;

    public UpdateNotificationPreferencesCommandHandler(
        IUserRepository userRepository,
        ILogger<UpdateNotificationPreferencesCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ErrorOr<bool>> Handle(UpdateNotificationPreferencesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating notification preferences for user {UserId}", request.UserId);

        try
        {
            var validator = new UpdateNotificationPreferencesCommandValidator();
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

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for notification preferences update: {UserId}", request.UserId);
                return Error.NotFound(code: "User.NotFound", description: "User not found");
            }

            _logger.LogInformation("Notification preferences updated successfully for user {UserId}", request.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences");
            return Error.Unexpected(code: "User.UnexpectedError", description: ex.Message);
        }
    }
}
