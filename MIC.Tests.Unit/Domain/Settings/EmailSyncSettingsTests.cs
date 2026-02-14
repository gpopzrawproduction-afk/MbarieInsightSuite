using FluentAssertions;
using MIC.Core.Domain.Settings;

namespace MIC.Tests.Unit.Domain.Settings;

public class EmailSyncSettingsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var settings = new EmailSyncSettings();

        settings.Id.Should().Be(Guid.Empty);
        settings.UserId.Should().Be(Guid.Empty);
        settings.HistoryMonths.Should().Be(6);
        settings.DownloadAttachments.Should().BeTrue();
        settings.IncludeSentFolder.Should().BeTrue();
        settings.IncludeDraftsFolder.Should().BeFalse();
        settings.IncludeArchiveFolder.Should().BeFalse();
        settings.LastSyncDate.Should().BeNull();
        settings.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var created = DateTimeOffset.UtcNow;
        var updated = DateTimeOffset.UtcNow;

        var settings = new EmailSyncSettings
        {
            Id = id,
            UserId = userId,
            HistoryMonths = 12,
            DownloadAttachments = false,
            IncludeSentFolder = false,
            IncludeDraftsFolder = true,
            IncludeArchiveFolder = true,
            LastSyncDate = now,
            CreatedAt = created,
            UpdatedAt = updated
        };

        settings.Id.Should().Be(id);
        settings.UserId.Should().Be(userId);
        settings.HistoryMonths.Should().Be(12);
        settings.DownloadAttachments.Should().BeFalse();
        settings.IncludeSentFolder.Should().BeFalse();
        settings.IncludeDraftsFolder.Should().BeTrue();
        settings.IncludeArchiveFolder.Should().BeTrue();
        settings.LastSyncDate.Should().Be(now);
        settings.CreatedAt.Should().Be(created);
        settings.UpdatedAt.Should().Be(updated);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(12)]
    [InlineData(24)]
    public void HistoryMonths_AcceptsVariousValues(int months)
    {
        var settings = new EmailSyncSettings { HistoryMonths = months };
        settings.HistoryMonths.Should().Be(months);
    }

    [Fact]
    public void LastSyncDate_CanBeSetToSpecificDate()
    {
        var date = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var settings = new EmailSyncSettings { LastSyncDate = date };
        settings.LastSyncDate.Should().Be(date);
    }

    [Fact]
    public void LastSyncDate_CanBeCleared()
    {
        var settings = new EmailSyncSettings { LastSyncDate = DateTime.UtcNow };
        settings.LastSyncDate = null;
        settings.LastSyncDate.Should().BeNull();
    }

    [Fact]
    public void HistoryMonths_DefaultIsSix()
    {
        new EmailSyncSettings().HistoryMonths.Should().Be(6);
    }

    [Fact]
    public void CreatedAt_CanBeSet()
    {
        var ts = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var settings = new EmailSyncSettings { CreatedAt = ts };
        settings.CreatedAt.Should().Be(ts);
    }

    [Fact]
    public void UpdatedAt_CanBeSetAndCleared()
    {
        var settings = new EmailSyncSettings();
        settings.UpdatedAt.Should().BeNull();

        var ts = DateTimeOffset.UtcNow;
        settings.UpdatedAt = ts;
        settings.UpdatedAt.Should().Be(ts);

        settings.UpdatedAt = null;
        settings.UpdatedAt.Should().BeNull();
    }
}
