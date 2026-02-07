using System;
using MIC.Core.Domain.Abstractions;

namespace MIC.Core.Domain.Entities;

/// <summary>
/// Tracks modifications applied to a persisted setting.
/// </summary>
public sealed class SettingHistory : BaseEntity
{
    public Guid SettingId { get; set; }

    public string OldValue { get; set; } = string.Empty;

    public string NewValue { get; set; } = string.Empty;

    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

    public string ChangedBy { get; set; } = string.Empty;

    public Setting Setting { get; set; } = null!;
}
