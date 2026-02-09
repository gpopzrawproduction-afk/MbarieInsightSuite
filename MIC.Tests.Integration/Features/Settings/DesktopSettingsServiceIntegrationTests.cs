using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using MIC.Desktop.Avalonia.Services;
using Xunit;

namespace MIC.Tests.Integration.Features.Settings;

public sealed class DesktopSettingsServiceIntegrationTests : IDisposable
{
    private readonly SettingsFileScope _scope;

    public DesktopSettingsServiceIntegrationTests()
    {
        _scope = new SettingsFileScope();
        ResetSingleton();
    }

    [Fact]
    public void ToggleTheme_PersistsAcrossInstances()
    {
        var service = SettingsService.Instance;
        service.CurrentTheme.Should().Be(AppTheme.Dark);

        service.ToggleTheme();
        service.CurrentTheme.Should().Be(AppTheme.Light);

        File.Exists(_scope.SettingsPath).Should().BeTrue();
        using var document = JsonDocument.Parse(File.ReadAllText(_scope.SettingsPath));
        var themeElement = document.RootElement.TryGetProperty("theme", out var camelCase)
            ? camelCase
            : document.RootElement.GetProperty("Theme");
        themeElement.GetInt32().Should().Be((int)AppTheme.Light);

        ResetSingleton();
        var reloaded = SettingsService.Instance;
        reloaded.CurrentTheme.Should().Be(AppTheme.Light);
    }

    [Fact]
    public void SaveSettings_PersistsCustomPreferences()
    {
        var service = SettingsService.Instance;
        service.Settings.AutoRefreshIntervalSeconds = 45;
        service.Settings.LastViewedPage = "Email";
        service.Settings.EnableAnimations = false;

        service.SaveSettings();

        ResetSingleton();
        var reloaded = SettingsService.Instance;

        reloaded.Settings.AutoRefreshIntervalSeconds.Should().Be(45);
        reloaded.Settings.LastViewedPage.Should().Be("Email");
        reloaded.Settings.EnableAnimations.Should().BeFalse();
    }

    public void Dispose()
    {
        ResetSingleton();
        _scope.Dispose();
    }

    private static void ResetSingleton()
    {
        var field = typeof(SettingsService).GetField("_instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(null, null);
    }

    private sealed class SettingsFileScope : IDisposable
    {
        private readonly string? _backupPath;
        private readonly bool _hadExisting;

        public SettingsFileScope()
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MbarieIntelligenceConsole");
            Directory.CreateDirectory(directory);

            SettingsPath = Path.Combine(directory, "settings.json");

            if (File.Exists(SettingsPath))
            {
                _backupPath = Path.Combine(Path.GetTempPath(), $"mic-settings-backup-{Guid.NewGuid():N}.json");
                File.Copy(SettingsPath, _backupPath, overwrite: true);
                _hadExisting = true;
                File.Delete(SettingsPath);
            }
        }

        public string SettingsPath { get; }

        public void Dispose()
        {
            try
            {
                if (_hadExisting && _backupPath is not null)
                {
                    File.Copy(_backupPath, SettingsPath, overwrite: true);
                    File.Delete(_backupPath);
                }
                else if (File.Exists(SettingsPath))
                {
                    File.Delete(SettingsPath);
                }
                else if (_backupPath is not null && File.Exists(_backupPath))
                {
                    File.Delete(_backupPath);
                }
            }
            catch
            {
                // Ignore cleanup failures to avoid hiding test issues.
            }
        }
    }
}
