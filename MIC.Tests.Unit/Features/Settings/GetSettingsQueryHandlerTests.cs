using System;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Settings.Queries.GetSettings;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Features.Settings;

public class GetSettingsQueryHandlerTests
{
    private readonly GetSettingsQueryHandler _sut;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<GetSettingsQueryHandler> _logger;

    public GetSettingsQueryHandlerTests()
    {
        _settingsService = Substitute.For<ISettingsService>();
        _logger = Substitute.For<ILogger<GetSettingsQueryHandler>>();
        _sut = new GetSettingsQueryHandler(_settingsService, _logger);
    }

    [Fact]
    public async Task Handle_WhenSettingsExist_ReturnsPersistedSettings()
    {
        var query = new GetSettingsQuery { UserId = Guid.NewGuid() };
        var savedSettings = new AppSettings
        {
            General = new GeneralSettings { AutoStart = true }
        };

        _settingsService
            .LoadUserSettingsAsync(query.UserId)
            .Returns(Task.FromResult(savedSettings));

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeSameAs(savedSettings);
        await _settingsService.Received(1).LoadUserSettingsAsync(query.UserId);
    }

    [Fact]
    public async Task Handle_WhenSettingsMissing_ReturnsDefaultSettings()
    {
        var query = new GetSettingsQuery { UserId = Guid.NewGuid() };

        _settingsService
            .LoadUserSettingsAsync(query.UserId)
            .Returns(Task.FromResult<AppSettings>(null!));

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEquivalentTo(new AppSettings());
    }

    [Fact]
    public async Task Handle_WhenLoadingFails_ReturnsFailureError()
    {
        var query = new GetSettingsQuery { UserId = Guid.NewGuid() };

        _settingsService
            .LoadUserSettingsAsync(query.UserId)
            .Returns<Task<AppSettings>>(_ => throw new InvalidOperationException("load failure"));

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Settings.LoadFailed");
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Description.Should().Contain("load failure");
    }
}
