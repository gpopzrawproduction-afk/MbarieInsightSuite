using System;
using FluentAssertions;
using MIC.Core.Domain.Entities;
using Xunit;

namespace MIC.Tests.Unit.Domain.Entities;

/// <summary>
/// Tests for User domain entity covering credential management and activation state.
/// Target: 6 tests validating core behaviors.
/// </summary>
public class UserEntityTests
{
    [Fact]
    public void Constructor_SetsDefaultState()
    {
        // Act
        var user = new User();

        // Assert
        user.Role.Should().Be(UserRole.User);
        user.IsActive.Should().BeTrue();
        user.Language.Should().Be(UserLanguage.English);
        user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        user.UpdatedAt.Should().BeCloseTo(user.CreatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void SetCredentials_WithValidValues_UpdatesProperties()
    {
        // Arrange
        var user = new User();
        var previousUpdated = user.UpdatedAt;

        // Act
        user.SetCredentials("alex", "alex@example.com");

        // Assert
        user.Username.Should().Be("alex");
        user.Email.Should().Be("alex@example.com");
        user.UpdatedAt.Should().BeAfter(previousUpdated);
    }

    [Fact]
    public void SetCredentials_WithEmptyUsername_Throws()
    {
        // Arrange
        var user = new User();

        // Act
        var act = () => user.SetCredentials(string.Empty, "someone@example.com");

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("username");
    }

    [Fact]
    public void SetPasswordHash_WithValidValues_UpdatesSecurityFields()
    {
        // Arrange
        var user = new User();
        user.SetCredentials("jane", "jane@example.com");
        var previousUpdated = user.UpdatedAt;

        // Act
        user.SetPasswordHash("hashed-value", "salt-value");

        // Assert
        user.PasswordHash.Should().Be("hashed-value");
        user.Salt.Should().Be("salt-value");
        user.UpdatedAt.Should().BeAfter(previousUpdated);
    }

    [Fact]
    public void SetRoleAndLanguage_UpdateState()
    {
        // Arrange
        var user = new User();

        // Act
        user.SetRole(UserRole.Admin);
        var updatedAfterRole = user.UpdatedAt;
        user.SetLanguage(UserLanguage.French);

        // Assert
        user.Role.Should().Be(UserRole.Admin);
        user.Language.Should().Be(UserLanguage.French);
        user.UpdatedAt.Should().BeAfter(updatedAfterRole);
    }

    [Fact]
    public void DeactivateAndActivate_TogglesIsActive()
    {
        // Arrange
        var user = new User();
        user.SetCredentials("paul", "paul@example.com");

        // Act
        user.Deactivate();
        var timestampAfterDeactivate = user.UpdatedAt;
        user.IsActive.Should().BeFalse();

        user.Activate();

        // Assert
        user.IsActive.Should().BeTrue();
        user.UpdatedAt.Should().BeAfter(timestampAfterDeactivate);
    }
}
