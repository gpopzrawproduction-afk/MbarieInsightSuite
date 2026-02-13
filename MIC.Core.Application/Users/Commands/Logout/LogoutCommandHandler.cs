using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using ErrorOr;

namespace MIC.Core.Application.Users.Commands.Logout;

public class LogoutCommandHandler : ICommandHandler<LogoutCommand, bool>
{
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(ILogger<LogoutCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ErrorOr<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User logout requested for user {UserId}", request.UserId);

        try
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                return Error.Validation(code: "User.ValidationFailed", description: "User ID is required");
            }

            _logger.LogInformation("User {UserId} logged out successfully", request.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return Error.Unexpected(code: "User.UnexpectedError", description: ex.Message);
        }
    }
}
