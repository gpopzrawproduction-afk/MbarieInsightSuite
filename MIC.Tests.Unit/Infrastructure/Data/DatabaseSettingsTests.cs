using FluentAssertions;
using MIC.Infrastructure.Data.Configuration;

namespace MIC.Tests.Unit.Infrastructure.Data;

public sealed class DatabaseSettingsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var settings = new DatabaseSettings();

        settings.RunMigrationsOnStartup.Should().BeFalse();
        settings.DeleteDatabaseOnStartup.Should().BeFalse();
        settings.SeedDataOnStartup.Should().BeFalse();
        settings.ConnectionRetryCount.Should().Be(3);
        settings.ConnectionRetryDelaySeconds.Should().Be(5);
        settings.CreateBackupBeforeMigration.Should().BeFalse();
    }

    [Fact]
    public void RunMigrationsOnStartup_CanBeSet()
    {
        var settings = new DatabaseSettings { RunMigrationsOnStartup = true };
        settings.RunMigrationsOnStartup.Should().BeTrue();
    }

    [Fact]
    public void DeleteDatabaseOnStartup_CanBeSet()
    {
        var settings = new DatabaseSettings { DeleteDatabaseOnStartup = true };
        settings.DeleteDatabaseOnStartup.Should().BeTrue();
    }

    [Fact]
    public void SeedDataOnStartup_CanBeSet()
    {
        var settings = new DatabaseSettings { SeedDataOnStartup = true };
        settings.SeedDataOnStartup.Should().BeTrue();
    }

    [Fact]
    public void ConnectionRetryCount_CanBeSet()
    {
        var settings = new DatabaseSettings { ConnectionRetryCount = 10 };
        settings.ConnectionRetryCount.Should().Be(10);
    }

    [Fact]
    public void ConnectionRetryDelaySeconds_CanBeSet()
    {
        var settings = new DatabaseSettings { ConnectionRetryDelaySeconds = 30 };
        settings.ConnectionRetryDelaySeconds.Should().Be(30);
    }

    [Fact]
    public void CreateBackupBeforeMigration_CanBeSet()
    {
        var settings = new DatabaseSettings { CreateBackupBeforeMigration = true };
        settings.CreateBackupBeforeMigration.Should().BeTrue();
    }
}
