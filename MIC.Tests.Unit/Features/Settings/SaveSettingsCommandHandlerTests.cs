using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Settings.Commands.SaveSettings;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Features.Settings;

public class SaveSettingsCommandHandlerTests
{
    private readonly SaveSettingsCommandHandler _sut;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SaveSettingsCommandHandler> _logger;

    public SaveSettingsCommandHandlerTests()
    {
        _settingsService = Substitute.For<ISettingsService>();
        _logger = Substitute.For<ILogger<SaveSettingsCommandHandler>>();
        _sut = new SaveSettingsCommandHandler(_settingsService, _logger);
    }

    [Fact]
    public async Task Handle_WhenSaveSucceeds_PersistsSettingsAndReturnsTrue()
    {
        var command = CreateCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();

        await _settingsService.Received(1).SaveUserSettingsAsync(command.UserId, command.Settings);
        await _settingsService.Received(1).SaveSettingsAsync(command.Settings);
    }

    [Fact]
    public async Task Handle_WhenUserSaveFails_ReturnsFailureError()
    {
        var command = CreateCommand();
        _settingsService
            .SaveUserSettingsAsync(command.UserId, command.Settings)
            .Returns<Task>(_ => throw new InvalidOperationException("storage unavailable"));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Settings.SaveFailed");
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Description.Should().Contain("storage unavailable");

        await _settingsService.DidNotReceive().SaveSettingsAsync(Arg.Any<AppSettings>());
    }

    [Fact]
    public async Task Handle_WhenLocalSaveFails_ReturnsFailureError()
    {
        var command = CreateCommand();
        _settingsService
            .SaveUserSettingsAsync(command.UserId, command.Settings)
            .Returns(Task.CompletedTask);
        _settingsService
            .SaveSettingsAsync(command.Settings)
            .Returns<Task>(_ => throw new IOException("disk full"));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Settings.SaveFailed");
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Description.Should().Contain("disk full");

        await _settingsService.Received(1).SaveUserSettingsAsync(command.UserId, command.Settings);
        await _settingsService.Received(1).SaveSettingsAsync(command.Settings);
    }

    private static SaveSettingsCommand CreateCommand()
    {
        return new SaveSettingsCommand
        {
            UserId = Guid.NewGuid(),
            Settings = new AppSettings()
        };
    }
}
