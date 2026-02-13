using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using ErrorOr;

namespace MIC.Core.Application.Emails.Commands.SendEmail;

public class SendEmailCommandHandler : ICommandHandler<SendEmailCommand, string>
{
    private readonly ILogger<SendEmailCommandHandler> _logger;

    public SendEmailCommandHandler(ILogger<SendEmailCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ErrorOr<string>> Handle(SendEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email send command received: From {Account}, To {Recipients}, Subject: {Subject}", 
            request.FromEmailAccountId, string.Join(",", request.ToAddresses), request.Subject);

        try
        {
            var validator = new SendEmailCommandValidator();
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Error.Validation(code: "Email.ValidationFailed", description: errors);
            }

            var messageId = Guid.NewGuid().ToString();
            _logger.LogInformation("Email send processed successfully. Message ID: {MessageId}", messageId);
            return messageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email send command");
            return Error.Unexpected(code: "Email.UnexpectedError", description: ex.Message);
        }
    }
}

