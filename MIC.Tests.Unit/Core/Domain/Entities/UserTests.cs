using System;
using FluentAssertions;
using MIC.Core.Domain.Entities;
using Xunit;

namespace MIC.Tests.Unit.Core.Domain.Entities;

/// <summary>
/// Tests for User entity covering credentials, roles, profile updates, and state management.
/// Target: 25 tests for comprehensive User entity coverage
/// </summary>
public class UserTests
{
    #region SetCredentials Tests

    [Fact]
    public void SetCredentials_WithValidInputs_SetsUsernameAndEmail()
    {
        // Arrange
        var user = new User();

        // Act
        user.SetCredentials("john_doe", "john@example.com");

        // Assert
        user.Username.Should().Be("john_doe");
        user.Email.Should().Be("john@example.com");
    }

    [Fact]
    public void SetCredentials_TrimsWhitespace()
    {
        // Arrange
        var user = new User();

        // Act
        user.SetCredentials("  john_doe  ", "  john@example.com  ");

        // Assert
        user.Username.Should().Be("john_doe");
        user.Email.Should().Be("john@example.com");
    }

    [Fact]
    public void SetCredentials_WithNullUsername_ThrowsArgumentException()
    {
        // Arrange
        var user = new User();

        // Act
        Action act = () => user.SetCredentials(null!, "john@example.com");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetCredentials_WithEmptyUsername_ThrowsArgumentException()
    {
        // Arrange
        var user = new User();

        // Act
        Action act = () => user.SetCredentials("", "john@example.com");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetCredentials_WithWhitespaceUsername_ThrowsArgumentException()
    {
        // Arrange
        var user = new User();

        // Act
        Action act = () => user.SetCredentials("   ", "john@example.com");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetCredentials_WithNullEmail_ThrowsArgumentException()
    {
        // Arrange
        var user = new User();

        // Act
        Action act = () => user.SetCredentials("john_doe", null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetCredentials_WithEmptyEmail_ThrowsArgumentException()
    {
        // Arrange
        var user = new User();

        // Act
        Action act = () => user.SetCredentials("john_doe", "");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region SetPasswordHash Tests

    [Fact]
    public void SetPasswordHash_WithValidInputs_SetsHashAndSalt()
    {
        // Arrange
        var user = new User();
        var hash = "hashed_password_value";
        var salt = "salt_value";

        // Act
        user.SetPasswordHash(hash, salt);

        // Assert
        user.PasswordHash.Should().Be(hash);
        user.Salt.Should().Be(salt);
    }

    [Fact]
    public void SetPasswordHash_WithNullHash_ThrowsArgumentException()
    {
        // Arrange
        var user = new User();

        // Act
        Action act = () => user.SetPasswordHash(null!, "salt");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetPasswordHash_WithEmptyHash_ThrowsArgumentException()
    {
        // Arrange
        var user = new User();

        // Act
        Action act = () => user.SetPasswordHash("", "salt");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetPasswordHash_WithNullSalt_ThrowsArgumentException()
    {
        // Arrange
        var user = new User();

        // Act
        Action act = () => user.SetPasswordHash("hash", null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetPasswordHash_WithEmptySalt_ThrowsArgumentException()
    {
        // Arrange
        var user = new User();

        // Act
        Action act = () => user.SetPasswordHash("hash", "");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Role Tests

    [Fact]
    public void SetRole_WithAdminRole_SetsRole()
    {
        // Arrange
        var user = new User();

        // Act
        user.SetRole(UserRole.Admin);

        // Assert
        user.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public void SetRole_WithUserRole_SetsRole()
    {
        // Arrange
        var user = new User();

        // Act
        user.SetRole(UserRole.User);

        // Assert
        user.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public void SetRole_WithGuestRole_SetsRole()
    {
        // Arrange
        var user = new User();

        // Act
        user.SetRole(UserRole.Guest);

        // Assert
        user.Role.Should().Be(UserRole.Guest);
    }

    [Fact]
    public void User_DefaultRole_IsUser()
    {
        // Act
        var user = new User();

        // Assert
        user.Role.Should().Be(UserRole.User);
    }

    #endregion

    #region Language Tests

    [Fact]
    public void SetLanguage_WithValidLanguage_SetsLanguage()
    {
        // Arrange
        var user = new User();

        // Act
        user.SetLanguage(UserLanguage.French);

        // Assert
        user.Language.Should().Be(UserLanguage.French);
    }

    [Fact]
    public void User_DefaultLanguage_IsEnglish()
    {
        // Act
        var user = new User();

        // Assert
        user.Language.Should().Be(UserLanguage.English);
    }

    [Theory]
    [InlineData(UserLanguage.English)]
    [InlineData(UserLanguage.French)]
    [InlineData(UserLanguage.Spanish)]
    [InlineData(UserLanguage.Arabic)]
    [InlineData(UserLanguage.Chinese)]
    public void SetLanguage_WithAllSupportedLanguages_SetsCorrectly(UserLanguage language)
    {
        // Arrange
        var user = new User();

        // Act
        user.SetLanguage(language);

        // Assert
        user.Language.Should().Be(language);
    }

    #endregion

    #region UpdateProfile Tests

    [Fact]
    public void UpdateProfile_WithFullName_SetsFullName()
    {
        // Arrange
        var user = new User();

        // Act
        user.UpdateProfile(fullName: "John Doe");

        // Assert
        user.FullName.Should().Be("John Doe");
    }

    [Fact]
    public void UpdateProfile_WithAllFields_SetsAllFields()
    {
        // Arrange
        var user = new User();

        // Act
        user.UpdateProfile(
            fullName: "John Doe",
            jobPosition: "Senior Engineer",
            department: "Engineering",
            seniorityLevel: "Senior");

        // Assert
        user.FullName.Should().Be("John Doe");
        user.JobPosition.Should().Be("Senior Engineer");
        user.Department.Should().Be("Engineering");
        user.SeniorityLevel.Should().Be("Senior");
    }

    [Fact]
    public void UpdateProfile_TrimsWhitespace()
    {
        // Arrange
        var user = new User();

        // Act
        user.UpdateProfile(
            fullName: "  John Doe  ",
            jobPosition: "  Engineer  ",
            department: "  Tech  ");

        // Assert
        user.FullName.Should().Be("John Doe");
        user.JobPosition.Should().Be("Engineer");
        user.Department.Should().Be("Tech");
    }

    [Fact]
    public void UpdateProfile_WithNullFullName_SetsFullNameToNull()
    {
        // Arrange
        var user = new User();
        user.UpdateProfile(fullName: "John Doe");

        // Act
        user.UpdateProfile(fullName: null);

        // Assert
        user.FullName.Should().BeNull();
    }

    [Fact]
    public void UpdateProfile_WithEmptyString_SetsToNull()
    {
        // Arrange
        var user = new User();

        // Act
        user.UpdateProfile(fullName: "", jobPosition: "   ");

        // Assert
        user.FullName.Should().BeNull();
        user.JobPosition.Should().BeNull();
    }

    #endregion

    #region Activation Tests

    [Fact]
    public void User_DefaultState_IsActive()
    {
        // Act
        var user = new User();

        // Assert
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_SetsIsActiveToTrue()
    {
        // Arrange
        var user = new User();
        user.Deactivate();

        // Act
        user.Activate();

        // Assert
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        // Arrange
        var user = new User();

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
    }

    #endregion

    #region RecordLogin Tests

    [Fact]
    public void RecordLogin_SetsLastLoginAt()
    {
        // Arrange
        var user = new User();
        var loginTime = DateTimeOffset.UtcNow;

        // Act
        user.RecordLogin(loginTime);

        // Assert
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().Be(loginTime);
    }

    [Fact]
    public void RecordLogin_UpdatesLastLoginOnSubsequentCalls()
    {
        // Arrange
        var user = new User();
        var firstLogin = DateTimeOffset.UtcNow;
        user.RecordLogin(firstLogin);

        var secondLogin = firstLogin.AddMinutes(10);

        // Act
        user.RecordLogin(secondLogin);

        // Assert
        user.LastLoginAt.Should().Be(secondLogin);
        user.LastLoginAt.Should().BeAfter(firstLogin);
    }

    #endregion
}
