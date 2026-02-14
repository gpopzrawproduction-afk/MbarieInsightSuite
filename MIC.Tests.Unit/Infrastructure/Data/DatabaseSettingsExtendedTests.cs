using FluentAssertions;
using MIC.Infrastructure.Data.Configuration;

namespace MIC.Tests.Unit.Infrastructure.Data;

/// <summary>
/// Extended tests for DatabaseSettings POCO verification.
/// </summary>
public class DatabaseSettingsExtendedTests
{
    [Fact]
    public void Defaults_RunMigrationsOnStartup_IsFalse()
    {
        new DatabaseSettings().RunMigrationsOnStartup.Should().BeFalse();
    }

    [Fact]
    public void Defaults_DeleteDatabaseOnStartup_IsFalse()
    {
        new DatabaseSettings().DeleteDatabaseOnStartup.Should().BeFalse();
    }

    [Fact]
    public void Defaults_SeedDataOnStartup_IsFalse()
    {
        new DatabaseSettings().SeedDataOnStartup.Should().BeFalse();
    }

    [Fact]
    public void Defaults_ConnectionRetryCount_Is3()
    {
        new DatabaseSettings().ConnectionRetryCount.Should().Be(3);
    }

    [Fact]
    public void Defaults_ConnectionRetryDelaySeconds_Is5()
    {
        new DatabaseSettings().ConnectionRetryDelaySeconds.Should().Be(5);
    }

    [Fact]
    public void Defaults_CreateBackupBeforeMigration_IsFalse()
    {
        new DatabaseSettings().CreateBackupBeforeMigration.Should().BeFalse();
    }

    [Fact]
    public void AllProperties_Settable()
    {
        var settings = new DatabaseSettings
        {
            RunMigrationsOnStartup = true,
            DeleteDatabaseOnStartup = true,
            SeedDataOnStartup = true,
            ConnectionRetryCount = 5,
            ConnectionRetryDelaySeconds = 10,
            CreateBackupBeforeMigration = true
        };

        settings.RunMigrationsOnStartup.Should().BeTrue();
        settings.DeleteDatabaseOnStartup.Should().BeTrue();
        settings.SeedDataOnStartup.Should().BeTrue();
        settings.ConnectionRetryCount.Should().Be(5);
        settings.ConnectionRetryDelaySeconds.Should().Be(10);
        settings.CreateBackupBeforeMigration.Should().BeTrue();
    }

    [Fact]
    public void ConnectionRetryCount_CanBeZero()
    {
        var settings = new DatabaseSettings { ConnectionRetryCount = 0 };
        settings.ConnectionRetryCount.Should().Be(0);
    }

    [Fact]
    public void ConnectionRetryDelaySeconds_CanBeZero()
    {
        var settings = new DatabaseSettings { ConnectionRetryDelaySeconds = 0 };
        settings.ConnectionRetryDelaySeconds.Should().Be(0);
    }
}
