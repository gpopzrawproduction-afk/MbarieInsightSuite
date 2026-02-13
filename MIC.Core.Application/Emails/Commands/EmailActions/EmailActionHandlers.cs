using MediatR;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using ErrorOr;

namespace MIC.Core.Application.Emails.Commands.DeleteEmail;

public class DeleteEmailCommandHandler : ICommandHandler<DeleteEmailCommand, bool>
{
    private readonly ILogger<DeleteEmailCommandHandler> _logger;

    public DeleteEmailCommandHandler(ILogger<DeleteEmailCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ErrorOr<bool>> Handle(DeleteEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email delete command received: EmailId {EmailId}", request.EmailId);
        try
        {
            var validator = new DeleteEmailCommandValidator();
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Error.Validation(code: "Email.ValidationFailed", description: errors);
            }

            _logger.LogInformation("Email deleted successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting email");
            return Error.Unexpected(code: "Email.UnexpectedError", description: ex.Message);
        }
    }
}

public class MoveEmailCommandHandler : ICommandHandler<MoveEmailCommand, bool>
{
    private readonly ILogger<MoveEmailCommandHandler> _logger;

    public MoveEmailCommandHandler(ILogger<MoveEmailCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ErrorOr<bool>> Handle(MoveEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email move command received: EmailId {EmailId} to folder {Folder}", 
            request.EmailId, request.TargetFolderName);
        try
        {
            var validator = new MoveEmailCommandValidator();
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Error.Validation(code: "Email.ValidationFailed", description: errors);
            }

            _logger.LogInformation("Email moved successfully to {Folder}", request.TargetFolderName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving email");
            return Error.Unexpected(code: "Email.UnexpectedError", description: ex.Message);
        }
    }
}

public class MarkEmailReadCommandHandler : ICommandHandler<MarkEmailReadCommand, bool>
{
    private readonly ILogger<MarkEmailReadCommandHandler> _logger;

    public MarkEmailReadCommandHandler(ILogger<MarkEmailReadCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ErrorOr<bool>> Handle(MarkEmailReadCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email mark read command received: EmailId {EmailId}, IsRead {IsRead}", 
            request.EmailId, request.IsRead);
        try
        {
            var validator = new MarkEmailReadCommandValidator();
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Error.Validation(code: "Email.ValidationFailed", description: errors);
            }

            _logger.LogInformation("Email marked as {Status}", request.IsRead ? "read" : "unread");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking email");
            return Error.Unexpected(code: "Email.UnexpectedError", description: ex.Message);
        }
    }
}

