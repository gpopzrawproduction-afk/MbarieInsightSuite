using System;
using FluentAssertions;
using MIC.Core.Domain.Entities;
using Xunit;

namespace MIC.Tests.Unit.Domain;

/// <summary>
/// Tests for Setting entity covering UpdateValue and sync status.
/// </summary>
public class SettingTests
{
    [Fact]
    public void DefaultProperties_AreInitialized()
    {
        var setting = new Setting();

        setting.Category.Should().BeEmpty();
        setting.Key.Should().BeEmpty();
        setting.Value.Should().BeEmpty();
        setting.ValueType.Should().Be("String");
        setting.SyncStatus.Should().Be(Setting.SyncStatuses.Local);
    }

    [Fact]
    public void UpdateValue_SetsValueAndType()
    {
        var setting = new Setting
        {
            Category = "Appearance",
            Key = "Theme",
            Value = "Light"
        };

        setting.UpdateValue("Dark", "String", "admin");

        setting.Value.Should().Be("Dark");
        setting.ValueType.Should().Be("String");
    }

    [Fact]
    public void UpdateValue_SetsSyncStatusToPending()
    {
        var setting = new Setting { SyncStatus = Setting.SyncStatuses.Synced };

        setting.UpdateValue("newVal", "Boolean", "admin");

        setting.SyncStatus.Should().Be(Setting.SyncStatuses.Pending);
    }

    [Fact]
    public void UpdateValue_UpdatesLastModified()
    {
        var setting = new Setting();
        var before = setting.LastModified;

        setting.UpdateValue("val", "String", "admin");

        setting.LastModified.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void SyncStatuses_HasExpectedConstants()
    {
        Setting.SyncStatuses.Local.Should().Be("Local");
        Setting.SyncStatuses.Pending.Should().Be("Pending");
        Setting.SyncStatuses.Synced.Should().Be("Synced");
    }

    [Fact]
    public void Properties_CanBeSetDirectly()
    {
        var userId = Guid.NewGuid();
        var setting = new Setting
        {
            UserId = userId,
            Category = "Email",
            Key = "SyncInterval",
            Value = "300",
            ValueType = "Integer"
        };

        setting.UserId.Should().Be(userId);
        setting.Category.Should().Be("Email");
        setting.Key.Should().Be("SyncInterval");
        setting.Value.Should().Be("300");
        setting.ValueType.Should().Be("Integer");
    }
}
