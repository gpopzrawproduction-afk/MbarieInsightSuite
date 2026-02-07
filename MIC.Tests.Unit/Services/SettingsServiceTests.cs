using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Authentication.Common;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Persistence;
using MIC.Infrastructure.Data.Services;
using Moq;

namespace MIC.Tests.Unit.Services;

public class SettingsServiceTests
{
    [Fact]
    public async Task SaveUserSettingsAsync_PersistsValuesForAuthenticatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (service, factory, tempFile) = CreateService(userId);

        var settings = new AppSettings
        {
            General =
            {
                AutoStart = true,
                MinimizeToTray = false
            },
            UI =
            {
                Theme = "Light",
                FontSize = 16
            }
        };

        try
        {
            // Act
            await service.SaveUserSettingsAsync(userId, settings);

            await using var verificationContext = await factory.CreateDbContextAsync();
            var stored = await verificationContext.Settings
                .Where(s => s.UserId == userId)
                .ToListAsync();

            // Assert
            stored.Should().NotBeEmpty();
            stored.Should().Contain(x =>
                x.Category == nameof(AppSettings.UI) &&
                x.Key == nameof(UISettings.Theme) &&
                x.Value == "Light");

            File.Exists(tempFile).Should().BeTrue();
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public async Task SetSettingAsync_RaisesEventAndWritesHistory()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (service, factory, tempFile) = CreateService(userId);

        try
        {
            await service.SetSettingAsync(nameof(AppSettings.UI), nameof(UISettings.Theme), "Dark");

            SettingsChangedEventArgs? capturedEvent = null;
            service.SettingsChanged += (_, args) => capturedEvent = args;

            // Act
            await service.SetSettingAsync(nameof(AppSettings.UI), nameof(UISettings.Theme), "Light");

            // Assert
            capturedEvent.Should().NotBeNull();
            capturedEvent!.Category.Should().Be(nameof(AppSettings.UI));
            capturedEvent.Key.Should().Be(nameof(UISettings.Theme));
            capturedEvent.Value.Should().Be("Light");

            await using var verificationContext = await factory.CreateDbContextAsync();
            var stored = await verificationContext.Settings
                .Include(s => s.History)
                .SingleAsync(s => s.UserId == userId && s.Category == nameof(AppSettings.UI) && s.Key == nameof(UISettings.Theme));

            stored.Value.Should().Be("Light");
            stored.History.Should().HaveCount(1);
            stored.History.Single().NewValue.Should().Be("Light");
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public async Task DeleteSettingAsync_RemovesEntry()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (service, factory, tempFile) = CreateService(userId);

        try
        {
            await service.SetSettingAsync(nameof(AppSettings.General), nameof(GeneralSettings.AutoStart), true);

            // Act
            var removed = await service.DeleteSettingAsync(nameof(AppSettings.General), nameof(GeneralSettings.AutoStart));

            // Assert
            removed.Should().BeTrue();

            await using var verificationContext = await factory.CreateDbContextAsync();
            var stored = await verificationContext.Settings
                .Where(s => s.UserId == userId)
                .ToListAsync();

            stored.Should().BeEmpty();
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    private static (SettingsService Service, IDbContextFactory<MicDbContext> Factory, string TempFilePath) CreateService(Guid userId)
    {
        var databaseName = $"settings-tests-{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<MicDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var factory = new InMemoryContextFactory(options);
        var loggerMock = new Mock<ILogger<SettingsService>>();

        var cloudMock = new Mock<ISettingsCloudSyncService>();
        cloudMock.Setup(s => s.SyncSettingsAsync(It.IsAny<IEnumerable<Setting>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        cloudMock.Setup(s => s.GetCurrentStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SettingsSyncStatus(0, 0, null));

        var sessionMock = new Mock<ISessionService>();
        sessionMock.SetupGet(s => s.IsAuthenticated).Returns(true);
        sessionMock.Setup(s => s.GetUser()).Returns(new UserDto
        {
            Id = userId,
            Email = "unit@test.local",
            Username = "unit-test"
        });

        var tempFile = Path.Combine(Path.GetTempPath(), $"mic-settings-{Guid.NewGuid():N}.json");

        var service = new SettingsService(factory, loggerMock.Object, cloudMock.Object, sessionMock.Object, tempFile);
        return (service, factory, tempFile);
    }

    private static void CleanupTempFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private sealed class InMemoryContextFactory : IDbContextFactory<MicDbContext>
    {
        private readonly DbContextOptions<MicDbContext> _options;

        public InMemoryContextFactory(DbContextOptions<MicDbContext> options)
        {
            _options = options;
        }

        public MicDbContext CreateDbContext() => new(_options);

        public ValueTask<MicDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask<MicDbContext>(new MicDbContext(_options));
        }
    }
}
