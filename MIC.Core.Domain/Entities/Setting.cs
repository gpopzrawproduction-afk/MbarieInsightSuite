using System;
using System.Collections.Generic;
using MIC.Core.Domain.Abstractions;

namespace MIC.Core.Domain.Entities;

/// <summary>
/// Represents a single persisted application setting with history and sync metadata.
/// </summary>
public sealed class Setting : BaseEntity
{
    public Guid UserId { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string ValueType { get; set; } = "String";

    public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;

    public string SyncStatus { get; set; } = SyncStatuses.Local;

    public User User { get; set; } = null!;

    public ICollection<SettingHistory> History { get; set; } = new List<SettingHistory>();

    public void UpdateValue(string newValue, string valueType, string modifiedBy)
    {
        Value = newValue;
        ValueType = valueType;
        LastModified = DateTimeOffset.UtcNow;
        SyncStatus = SyncStatuses.Pending;
        MarkAsModified(modifiedBy);
    }

    public static class SyncStatuses
    {
        public const string Local = "Local";
        public const string Pending = "Pending";
        public const string Synced = "Synced";
    }
}
