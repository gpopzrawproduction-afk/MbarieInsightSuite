using Microsoft.Extensions.Logging;
using Moq;
using MIC.Core.Application.Users.Commands.UpdateUserProfile;
using MIC.Core.Application.Users.Commands.ChangePassword;
using MIC.Core.Application.Users.Commands.UpdateNotificationPreferences;
using MIC.Core.Application.Common.Interfaces;
using ErrorOr;

namespace MIC.Tests.Unit.Features.Users;

public class UpdateUserProfileCommandValidatorTests
{
    private readonly UpdateUserProfileCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldSucceed()
    {
        var command = new UpdateUserProfileCommand
        {
            UserId = "user-1",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };
        var result = _validator.Validate(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyFirstName_ShouldFail()
    {
        var command = new UpdateUserProfileCommand
        {
            UserId = "user-1",
            FirstName = "",
            LastName = "Doe",
            Email = "john@example.com"
        };
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithInvalidEmail_ShouldFail()
    {
        var command = new UpdateUserProfileCommand
        {
            UserId = "user-1",
            FirstName = "John",
            LastName = "Doe",
            Email = "not-an-email"
        };
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
    }
}

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidPassword_ShouldSucceed()
    {
        var command = new ChangePasswordCommand
        {
            UserId = "user-1",
            CurrentPassword = "OldPass123!",
            NewPassword = "NewPass123!@#",
            ConfirmPassword = "NewPass123!@#"
        };
        var result = _validator.Validate(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithWeakPassword_ShouldFail()
    {
        var command = new ChangePasswordCommand
        {
            UserId = "user-1",
            CurrentPassword = "OldPass123!",
            NewPassword = "weak",
            ConfirmPassword = "weak"
        };
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithMismatchedPasswords_ShouldFail()
    {
        var command = new ChangePasswordCommand
        {
            UserId = "user-1",
            CurrentPassword = "OldPass123!",
            NewPassword = "NewPass123!@#",
            ConfirmPassword = "Different123!@#"
        };
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
    }
}

public class UpdateUserProfileCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ILogger<UpdateUserProfileCommandHandler>> _loggerMock = new();
    private readonly UpdateUserProfileCommandHandler _handler;

    public UpdateUserProfileCommandHandlerTests()
    {
        _handler = new UpdateUserProfileCommandHandler(_userRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithInvalidGuid_ShouldReturnValidationError()
    {
        var command = new UpdateUserProfileCommand
        {
            UserId = "invalid-guid",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldReturnError()
    {
        var command = new UpdateUserProfileCommand
        {
            UserId = "00000000-0000-0000-0000-000000000001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((MIC.Core.Domain.Entities.User?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsError);
    }
}
