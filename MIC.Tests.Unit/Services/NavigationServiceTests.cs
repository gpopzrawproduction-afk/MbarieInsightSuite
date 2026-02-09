using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FluentAssertions;
using MIC.Desktop.Avalonia.Services;
using MIC.Desktop.Avalonia.ViewModels;
using MIC.Core.Application.Common.Interfaces;
using Moq;
using Serilog;

namespace MIC.Tests.Unit.Services;

public class NavigationServiceTests
{
    [Fact]
    public void NavigateTo_WhenMainWindowMissing_DoesNotThrow()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(MainWindowViewModel))).Returns((object?)null);
        var navigationService = new NavigationService(serviceProvider.Object);

        Action act = () => navigationService.NavigateTo("Dashboard");

        act.Should().NotThrow();
    }

    [Fact]
    public async Task NavigateToAsync_WhenMainWindowMissing_CompletesImmediately()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(MainWindowViewModel))).Returns((object?)null);
        var navigationService = new NavigationService(serviceProvider.Object);

        var task = navigationService.NavigateToAsync("Dashboard");

        await task;
        task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void NavigateTo_WhenMainWindowPresent_UpdatesViewName()
    {
        var mainWindow = TestMainWindowViewModelFactory.Create();
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(MainWindowViewModel))).Returns(mainWindow);
        var navigationService = new NavigationService(serviceProvider.Object);

        navigationService.NavigateTo("CustomView");

        mainWindow.CurrentViewName.Should().Be("CustomView");
        mainWindow.CurrentView.Should().BeNull();
    }

    [Fact]
    public async Task NavigateToAsync_WhenMainWindowPresent_DelegatesToNavigateTo()
    {
        var mainWindow = TestMainWindowViewModelFactory.Create();
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(MainWindowViewModel))).Returns(mainWindow);
        var navigationService = new NavigationService(serviceProvider.Object);

        await navigationService.NavigateToAsync("AsyncView");

        mainWindow.CurrentViewName.Should().Be("AsyncView");
    }

    private static class TestMainWindowViewModelFactory
    {
        internal static MainWindowViewModel Create()
        {
            // Bypass the heavy constructor wiring so we can control only the fields navigation touches.
            #pragma warning disable SYSLIB0050
            var instance = (MainWindowViewModel)FormatterServices.GetUninitializedObject(typeof(MainWindowViewModel));
            #pragma warning restore SYSLIB0050

            SetField(instance, "_sessionService", Mock.Of<ISessionService>());
            SetField(instance, "_serviceProvider", new Mock<IServiceProvider>().Object);
            SetField(instance, "_logger", Mock.Of<ILogger>());
            SetField(instance, "_notificationService", Mock.Of<INotificationService>());
            SetField(instance, "_connectionStatus", "Connected");
            SetField(instance, "_currentViewName", "Dashboard");
            SetField(instance, "_lastUpdateTime", string.Empty);
            SetAutoProperty(instance, "CommandPalette", new CommandPaletteViewModel());

            return instance;
        }

        private static void SetField(MainWindowViewModel instance, string fieldName, object? value)
        {
            var field = typeof(MainWindowViewModel).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(instance, value);
        }

        private static void SetAutoProperty(MainWindowViewModel instance, string propertyName, object? value)
        {
            var field = typeof(MainWindowViewModel).GetField($"<{propertyName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(instance, value);
        }
    }
}
