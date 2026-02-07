using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MIC.Core.Application.Authentication;
using MIC.Core.Application.Authentication.Commands.RegisterUserCommand;
using MIC.Core.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Features.Auth;

public class RegisterUserCommandHandlerTests
{
    private readonly RegisterUserCommandHandler _sut;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandlerTests()
    {
        _authenticationService = Substitute.For<IAuthenticationService>();
        _logger = Substitute.For<ILogger<RegisterUserCommandHandler>>();
        _sut = new RegisterUserCommandHandler(_authenticationService, _logger);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ReturnsAuthenticationResult()
    {
        var command = CreateValidCommand();
        var expectedResult = new AuthenticationResult
        {
            Success = true,
            Token = "token",
            User = null
        };

        _authenticationService.RegisterAsync(command.Username, command.Email, command.Password, command.FullName!)
            .Returns(expectedResult);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expectedResult);
        await _authenticationService.Received(1).RegisterAsync(command.Username, command.Email, command.Password, command.FullName!);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsFailure_ReturnsValidationError()
    {
        var command = CreateValidCommand();
        var failure = new AuthenticationResult
        {
            Success = false,
            ErrorMessage = "duplicate user"
        };

        _authenticationService.RegisterAsync(command.Username, command.Email, command.Password, command.FullName!)
            .Returns(failure);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Registration.Failed");
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Description.Should().Contain("duplicate user");
    }

    [Fact]
    public async Task Handle_WhenServiceThrows_ReturnsFailureError()
    {
        var command = CreateValidCommand();
        _authenticationService.RegisterAsync(command.Username, command.Email, command.Password, command.FullName!)
            .Returns<Task<AuthenticationResult>>(_ => throw new InvalidOperationException("boom"));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Registration.Failure");
        result.FirstError.Description.Should().Contain("boom");
    }

    [Theory]
    [MemberData(nameof(GetInvalidCommands))]
    public async Task Handle_WithInvalidRequest_ReturnsValidationError(RegisterUserCommand command, string expectedCode)
    {
        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be(expectedCode);
        await _authenticationService.DidNotReceiveWithAnyArgs().RegisterAsync(default!, default!, default!, default!);
    }

    private static RegisterUserCommand CreateValidCommand() => new()
    {
        Username = "user1",
        Email = "user1@example.com",
        Password = "Password123!",
        ConfirmPassword = "Password123!",
        FullName = "User One"
    };

    public static IEnumerable<object[]> GetInvalidCommands()
    {
        yield return new object[]
        {
            CreateValidCommand() with { Username = "" },
            "Registration.Validation.Username"
        };

        yield return new object[]
        {
            CreateValidCommand() with { Email = "" },
            "Registration.Validation.Email"
        };

        yield return new object[]
        {
            CreateValidCommand() with { Password = "" },
            "Registration.Validation.Password"
        };

        yield return new object[]
        {
            CreateValidCommand() with { ConfirmPassword = "" },
            "Registration.Validation.ConfirmPassword"
        };

        yield return new object[]
        {
            CreateValidCommand() with { Password = "short" , ConfirmPassword = "short" },
            "Registration.Validation.PasswordTooShort"
        };

        yield return new object[]
        {
            CreateValidCommand() with { ConfirmPassword = "Mismatch123!" },
            "Registration.Validation.PasswordMismatch"
        };

        yield return new object[]
        {
            CreateValidCommand() with { Email = "invalid-email" },
            "Registration.Validation.InvalidEmail"
        };
    }
}
