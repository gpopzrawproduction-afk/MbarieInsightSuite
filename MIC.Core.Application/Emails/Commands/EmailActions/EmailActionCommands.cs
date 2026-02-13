using MIC.Core.Application.Common.Interfaces;
using ErrorOr;

namespace MIC.Core.Application.Emails.Commands.DeleteEmail;

public record DeleteEmailCommand : ICommand<bool>
{
    public string EmailId { get; init; } = string.Empty;
    public string EmailAccountId { get; init; } = string.Empty;
}

public record MoveEmailCommand : ICommand<bool>
{
    public string EmailId { get; init; } = string.Empty;
    public string EmailAccountId { get; init; } = string.Empty;
    public string TargetFolderName { get; init; } = string.Empty;
}

public record MarkEmailReadCommand : ICommand<bool>
{
    public string EmailId { get; init; } = string.Empty;
    public bool IsRead { get; init; } = true;
}

