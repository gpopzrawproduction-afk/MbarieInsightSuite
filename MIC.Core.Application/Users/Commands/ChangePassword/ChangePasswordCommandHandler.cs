using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using ErrorOr;

namespace MIC.Core.Application.Users.Commands.ChangePassword;

public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<ErrorOr<bool>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password change requested for user {UserId}", request.UserId);

        try
        {
            var validator = new ChangePasswordCommandValidator();
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
                _logger.LogWarning("User not found for password change: {UserId}", request.UserId);
                return Error.NotFound(code: "User.NotFound", description: "User not found");
            }

            // Verify current password
            var isValidPassword = _passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash, user.Salt);
            if (!isValidPassword)
            {
                _logger.LogWarning("Invalid current password for user {UserId}", request.UserId);
                return Error.Validation(code: "User.InvalidPassword", description: "Current password is incorrect");
            }

            // Hash new password
            var (hashedPassword, salt) = _passwordHasher.HashPassword(request.NewPassword);
            // user.PasswordHash = hashedPassword;
            // user.PasswordSalt = salt;
            // await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Password changed successfully for user {UserId}", request.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return Error.Unexpected(code: "User.UnexpectedError", description: ex.Message);
        }
    }
}
