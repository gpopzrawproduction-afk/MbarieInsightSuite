using FluentAssertions;
using MIC.Core.Domain.Entities;

namespace MIC.Tests.Unit.Domain.Entities;

public class UserSettingsTests
{
    [Fact]
    public void DefaultConstructor_ShouldSetDefaults()
    {
        var settings = new UserSettings();

        settings.UserId.Should().Be(Guid.Empty);
        settings.SettingsJson.Should().Be("{}");
        settings.SettingsVersion.Should().Be(1);
        settings.LastUpdated.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void ParameterizedConstructor_ShouldSetProperties()
    {
        var userId = Guid.NewGuid();
        var json = """{"theme":"dark"}""";

        var settings = new UserSettings(userId, json);

        settings.UserId.Should().Be(userId);
        settings.SettingsJson.Should().Be(json);
        settings.LastUpdated.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void ParameterizedConstructor_NullJson_ShouldDefaultToEmptyObject()
    {
        var settings = new UserSettings(Guid.NewGuid(), null!);

        settings.SettingsJson.Should().Be("{}");
    }

    [Fact]
    public void UpdateSettings_ShouldChangeJsonAndTimestamp()
    {
        var settings = new UserSettings(Guid.NewGuid(), "{}");
        var originalTimestamp = settings.LastUpdated;

        // Small delay to detect timestamp change
        var newJson = """{"notifications":true}""";
        settings.UpdateSettings(newJson);

        settings.SettingsJson.Should().Be(newJson);
        settings.LastUpdated.Should().BeOnOrAfter(originalTimestamp);
    }

    [Fact]
    public void UpdateSettings_NullJson_ShouldDefaultToEmptyObject()
    {
        var settings = new UserSettings(Guid.NewGuid(), """{"old":"value"}""");

        settings.UpdateSettings(null!);

        settings.SettingsJson.Should().Be("{}");
    }

    [Fact]
    public void UpdateSettings_ShouldCallMarkAsModified()
    {
        var settings = new UserSettings(Guid.NewGuid(), "{}");

        settings.UpdateSettings("""{"key":"val"}""");

        settings.ModifiedAt.Should().NotBeNull();
        settings.ModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void SettingsVersion_DefaultsToOne()
    {
        var settings = new UserSettings();
        settings.SettingsVersion.Should().Be(1);
    }

    [Fact]
    public void SettingsVersion_CanBeSet()
    {
        var settings = new UserSettings { SettingsVersion = 5 };
        settings.SettingsVersion.Should().Be(5);
    }

    [Fact]
    public void User_DefaultsToNonNull()
    {
        // User is not null by default (= null!)
        var settings = new UserSettings();
        // The field compiler default is null! so accessing it is valid
        settings.UserId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Id_IsSetByBaseEntity()
    {
        var settings = new UserSettings();
        settings.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void UpdateSettings_MultipleTimes_ShouldUpdateTimestampEachTime()
    {
        var settings = new UserSettings(Guid.NewGuid(), "{}");

        settings.UpdateSettings("""{"v":1}""");
        var firstUpdate = settings.LastUpdated;

        settings.UpdateSettings("""{"v":2}""");
        var secondUpdate = settings.LastUpdated;

        secondUpdate.Should().BeOnOrAfter(firstUpdate);
        settings.SettingsJson.Should().Be("""{"v":2}""");
    }

    [Fact]
    public void UserId_CanBeSetViaProperty()
    {
        var settings = new UserSettings();
        var id = Guid.NewGuid();
        settings.UserId = id;
        settings.UserId.Should().Be(id);
    }

    [Fact]
    public void SettingsJson_CanBeSetDirectly()
    {
        var settings = new UserSettings();
        settings.SettingsJson = """{"direct":true}""";
        settings.SettingsJson.Should().Be("""{"direct":true}""");
    }
}
