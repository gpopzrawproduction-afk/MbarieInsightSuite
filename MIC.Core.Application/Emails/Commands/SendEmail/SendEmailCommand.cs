using MediatR;
using MIC.Core.Application.Common.Interfaces;
using ErrorOr;

namespace MIC.Core.Application.Emails.Commands.SendEmail;

/// <summary>
/// Command to send an email via SMTP through the connected email account.
/// </summary>
public record SendEmailCommand : ICommand<string>
{
    /// <summary>
    /// ID of the email account to send from (Gmail, Outlook, etc.)
    /// </summary>
    public string FromEmailAccountId { get; init; } = string.Empty;

    /// <summary>
    /// Primary recipients (To field)
    /// </summary>
    public List<string> ToAddresses { get; init; } = new();

    /// <summary>
    /// Carbon copy recipients (Cc field)
    /// </summary>
    public List<string> CcAddresses { get; init; } = new();

    /// <summary>
    /// Blind carbon copy recipients (Bcc field)
    /// </summary>
    public List<string> BccAddresses { get; init; } = new();

    /// <summary>
    /// Email subject line
    /// </summary>
    public string Subject { get; init; } = string.Empty;

    /// <summary>
    /// Email body content (can be HTML if IsHtml is true)
    /// </summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>
    /// Whether body should be treated as HTML (true) or plain text (false)
    /// </summary>
    public bool IsHtml { get; init; } = false;

    /// <summary>
    /// Optional file attachments (file paths)
    /// </summary>
    public List<string> AttachmentPaths { get; init; } = new();

    /// <summary>
    /// If true, message will be saved to SentItems folder automatically
    /// </summary>
    public bool SaveToSentItems { get; init; } = true;

    /// <summary>
    /// Optional in-reply-to message ID (for reply/reply-all functionality)
    /// </summary>
    public string? InReplyToMessageId { get; init; }

    /// <summary>
    /// Optional conversation ID (for threading)
    /// </summary>
    public string? ConversationId { get; init; }
}

