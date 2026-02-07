using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Authentication;
using MIC.Core.Domain.Entities;
using FluentAssertions;
using MIC.Tests.Integration.Infrastructure;
using Xunit;

namespace MIC.Tests.Integration.Features.Auth;

public sealed class LoginIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnSuccessWithToken()
    {
        // Arrange
        const string username = "admin";
        const string password = "Admin@123";
        await SeedUserAsync(username, password, email: "admin@example.com", role: UserRole.Admin);

        using var scope = CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

        // Act
        var result = await authService.LoginAsync(username, password);

        // Assert
        result.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be(username);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnFailure()
    {
        // Arrange
        const string username = "admin";
        const string correctPassword = "Admin@123";
        const string wrongPassword = "WrongPassword123";
        await SeedUserAsync(username, correctPassword, email: "admin@example.com", role: UserRole.Admin);

        using var scope = CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

        // Act
        var result = await authService.LoginAsync(username, wrongPassword);

        // Assert
        result.Success.Should().BeFalse();
        result.Token.Should().BeNull();
        result.ErrorMessage.Should().NotBeNull();
        result.ErrorMessage.Should().Contain("Invalid");
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        using var scope = CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        const string username = "nonexistent";
        const string password = "SomePassword123";

        // Act
        var result = await authService.LoginAsync(username, password);

        // Assert
        result.Success.Should().BeFalse();
        result.Token.Should().BeNull();
        result.ErrorMessage.Should().NotBeNull();
    }
}
