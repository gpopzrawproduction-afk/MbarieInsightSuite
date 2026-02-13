using MIC.Core.Application.Common.Interfaces;
using ErrorOr;

namespace MIC.Core.Application.Emails.Commands.ReplyEmail;

/// <summary>
/// Command to reply to an email.
/// Automatically includes original message in quoted format.
/// </summary>
public record ReplyEmailCommand : ICommand<string>
{
    /// <summary>
    /// ID of the email account to send from
    /// </summary>
    public string FromEmailAccountId { get; init; } = string.Empty;

    /// <summary>
    /// ID of the message being replied to
    /// </summary>
    public string OriginalMessageId { get; init; } = string.Empty;

    /// <summary>
    /// Email address of the original sender (recipient of reply)
    /// </summary>
    public string ReplyToAddress { get; init; } = string.Empty;

    /// <summary>
    /// Reply body (without the quoted original message)
    /// </summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>
    /// Whether to reply to all recipients (true) or just the sender (false)
    /// </summary>
    public bool ReplyAll { get; init; } = false;

    /// <summary>
    /// Additional recipients for reply-all
    /// </summary>
    public List<string> CcAddresses { get; init; } = new();
}

