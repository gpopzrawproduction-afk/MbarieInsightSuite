using System;
using FluentAssertions;
using MIC.Core.Domain.Entities;
using Xunit;

namespace MIC.Tests.Unit.Domain.Entities;

/// <summary>
/// Extended tests for <see cref="User"/> entity covering UpdateProfile, RecordLogin,
/// credential guards, and Touch behavior.
/// </summary>
public class UserEntityExtendedTests
{
    private static User CreateUser()
    {
        var user = new User();
        user.SetCredentials("testuser", "test@test.com");
        user.SetPasswordHash("hash123", "salt123");
        return user;
    }

    #region UpdateProfile

    [Fact]
    public void UpdateProfile_SetsAllFields()
    {
        var user = CreateUser();
        user.UpdateProfile("John Doe", "Engineer", "Engineering", "Senior");

        user.FullName.Should().Be("John Doe");
        user.JobPosition.Should().Be("Engineer");
        user.Department.Should().Be("Engineering");
        user.SeniorityLevel.Should().Be("Senior");
    }

    [Fact]
    public void UpdateProfile_NullFields_SetsToNull()
    {
        var user = CreateUser();
        user.UpdateProfile("John Doe", "Engineer", "Engineering", "Senior");
        user.UpdateProfile(null, null, null, null);

        user.FullName.Should().BeNull();
        user.JobPosition.Should().BeNull();
        user.Department.Should().BeNull();
        user.SeniorityLevel.Should().BeNull();
    }

    [Fact]
    public void UpdateProfile_WhitespaceFields_SetsToNull()
    {
        var user = CreateUser();
        user.UpdateProfile("   ", "  ", " ", "  ");

        user.FullName.Should().BeNull();
        user.JobPosition.Should().BeNull();
        user.Department.Should().BeNull();
        user.SeniorityLevel.Should().BeNull();
    }

    [Fact]
    public void UpdateProfile_TrimsWhitespace()
    {
        var user = CreateUser();
        user.UpdateProfile("  John Doe  ", " Engineer ", " Dept ", " Level ");

        user.FullName.Should().Be("John Doe");
        user.JobPosition.Should().Be("Engineer");
        user.Department.Should().Be("Dept");
        user.SeniorityLevel.Should().Be("Level");
    }

    [Fact]
    public void UpdateProfile_UpdatesTimestamp()
    {
        var user = CreateUser();
        var before = user.UpdatedAt;
        System.Threading.Thread.Sleep(10);

        user.UpdateProfile("Name");

        user.UpdatedAt.Should().BeOnOrAfter(before);
    }

    #endregion

    #region RecordLogin

    [Fact]
    public void RecordLogin_SetsLastLoginAt()
    {
        var user = CreateUser();
        var loginTime = DateTimeOffset.UtcNow;

        user.RecordLogin(loginTime);

        user.LastLoginAt.Should().Be(loginTime);
    }

    [Fact]
    public void RecordLogin_UpdatesUpdatedAt()
    {
        var user = CreateUser();
        var loginTime = DateTimeOffset.UtcNow.AddHours(1);

        user.RecordLogin(loginTime);

        user.UpdatedAt.Should().Be(loginTime);
    }

    #endregion

    #region SetCredentials Guards

    [Fact]
    public void SetCredentials_EmptyEmail_Throws()
    {
        var user = new User();
        var act = () => user.SetCredentials("user", "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetCredentials_WhitespaceUsername_Throws()
    {
        var user = new User();
        var act = () => user.SetCredentials("   ", "test@test.com");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetCredentials_TrimsValues()
    {
        var user = new User();
        user.SetCredentials("  user  ", "  test@test.com  ");

        user.Username.Should().Be("user");
        user.Email.Should().Be("test@test.com");
    }

    #endregion

    #region SetPasswordHash Guards

    [Fact]
    public void SetPasswordHash_EmptyHash_Throws()
    {
        var user = new User();
        var act = () => user.SetPasswordHash("", "salt");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetPasswordHash_EmptySalt_Throws()
    {
        var user = new User();
        var act = () => user.SetPasswordHash("hash", "");
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region SetRole and SetLanguage

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.User)]
    [InlineData(UserRole.Guest)]
    public void SetRole_SetsAllRoles(UserRole role)
    {
        var user = CreateUser();
        user.SetRole(role);
        user.Role.Should().Be(role);
    }

    [Theory]
    [InlineData(UserLanguage.English)]
    [InlineData(UserLanguage.French)]
    [InlineData(UserLanguage.Spanish)]
    [InlineData(UserLanguage.Arabic)]
    [InlineData(UserLanguage.Chinese)]
    public void SetLanguage_SetsAllLanguages(UserLanguage language)
    {
        var user = CreateUser();
        user.SetLanguage(language);
        user.Language.Should().Be(language);
    }

    #endregion

    #region Activate/Deactivate Touch

    [Fact]
    public void Deactivate_UpdatesTimestamp()
    {
        var user = CreateUser();
        var before = user.UpdatedAt;
        System.Threading.Thread.Sleep(10);

        user.Deactivate();

        user.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Activate_UpdatesTimestamp()
    {
        var user = CreateUser();
        user.Deactivate();
        var before = user.UpdatedAt;
        System.Threading.Thread.Sleep(10);

        user.Activate();

        user.UpdatedAt.Should().BeOnOrAfter(before);
    }

    #endregion

    #region Constructor Defaults

    [Fact]
    public void Constructor_DefaultValues()
    {
        var user = new User();

        user.Username.Should().BeEmpty();
        user.Email.Should().BeEmpty();
        user.PasswordHash.Should().BeEmpty();
        user.Salt.Should().BeEmpty();
        user.Role.Should().Be(UserRole.User);
        user.IsActive.Should().BeTrue();
        user.Language.Should().Be(UserLanguage.English);
        user.LastLoginAt.Should().BeNull();
        user.FullName.Should().BeNull();
        user.JobPosition.Should().BeNull();
        user.Department.Should().BeNull();
        user.SeniorityLevel.Should().BeNull();
    }

    #endregion
}
