using System;
using FluentAssertions;
using MIC.Core.Application.Authentication.Common;
using MIC.Core.Domain.Entities;
using Xunit;

namespace MIC.Tests.Unit.Application.Authentication;

public class UserDtoTests
{
    private static User CreateFullUser()
    {
        var user = new User();
        user.SetCredentials("admin", "admin@mic.com");
        user.SetPasswordHash("hash", "salt");
        user.SetRole(UserRole.Admin);
        user.UpdateProfile("John Doe", "Engineer", "Engineering", "Senior");
        user.RecordLogin(DateTimeOffset.UtcNow);
        return user;
    }

    [Fact]
    public void FromUser_NullUser_ThrowsArgumentNullException()
    {
        var act = () => UserDto.FromUser(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromUser_MapsAllProperties()
    {
        var user = CreateFullUser();
        var dto = UserDto.FromUser(user);

        dto.Id.Should().Be(user.Id);
        dto.Username.Should().Be("admin");
        dto.Email.Should().Be("admin@mic.com");
        dto.FullName.Should().Be("John Doe");
        dto.Role.Should().Be(UserRole.Admin);
        dto.IsActive.Should().Be(user.IsActive);
        dto.CreatedAt.Should().Be(user.CreatedAt);
        dto.UpdatedAt.Should().Be(user.UpdatedAt);
        dto.JobPosition.Should().Be("Engineer");
        dto.Department.Should().Be("Engineering");
        dto.SeniorityLevel.Should().Be("Senior");
    }

    [Fact]
    public void FromUser_MinimalUser_MapsNullOptionals()
    {
        var user = new User();
        user.SetCredentials("user1", "u@t.com");
        user.SetPasswordHash("h", "s");

        var dto = UserDto.FromUser(user);

        dto.FullName.Should().BeNull();
        dto.JobPosition.Should().BeNull();
        dto.Department.Should().BeNull();
        dto.SeniorityLevel.Should().BeNull();
        dto.IsActive.Should().BeTrue();
        dto.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public void FromUser_DeactivatedUser_MapsIsActiveFalse()
    {
        var user = CreateFullUser();
        user.Deactivate();

        var dto = UserDto.FromUser(user);

        dto.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Record_Equality_ByValue()
    {
        var user = CreateFullUser();
        var dto1 = UserDto.FromUser(user);
        var dto2 = UserDto.FromUser(user);

        dto1.Should().Be(dto2);
    }
}
