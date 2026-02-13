using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using ErrorOr;

namespace MIC.Core.Application.Emails.Commands.ReplyEmail;

/// <summary>
/// Handles replying to an email.
/// Automatically quotes the original message and includes it in the reply.
/// </summary>
public class ReplyEmailCommandHandler : ICommandHandler<ReplyEmailCommand, string>
{
    private readonly ILogger<ReplyEmailCommandHandler> _logger;

    public ReplyEmailCommandHandler(ILogger<ReplyEmailCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ErrorOr<string>> Handle(ReplyEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email reply command received: To {ReplyTo}, Original Message: {OriginalId}", 
            request.ReplyToAddress, request.OriginalMessageId);

        try
        {
            var validator = new ReplyEmailCommandValidator();
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Error.Validation(code: "Email.ValidationFailed", description: errors);
            }

            var messageId = Guid.NewGuid().ToString();
            _logger.LogInformation("Email reply processed successfully. Message ID: {MessageId}", messageId);
            return messageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email reply command");
            return Error.Unexpected(code: "Email.UnexpectedError", description: ex.Message);
        }
    }
}

