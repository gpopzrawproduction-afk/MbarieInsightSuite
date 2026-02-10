using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Common.Interfaces;
using MIC.Desktop.Avalonia.Services;
using Moq;
using Xunit;

namespace MIC.Tests.Unit.Services;

/// <summary>
/// Tests for NavigationService.
/// Tests view navigation, service provider integration, and error handling.
/// Target: 6 additional tests for navigation functionality
/// </summary>
public class NavigationServiceTests
{
    #region Constructor Tests (2 tests)

    [Fact]
    public void Constructor_WithValidServiceProvider_CreatesInstance()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>();

        // Act
        var service = new NavigationService(serviceProvider.Object);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<INavigationService>();
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new NavigationService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    #endregion

    #region NavigateTo Tests (2 tests)

    [Fact]
    public void NavigateTo_WithMissingViewModel_DoesNotThrow()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .BuildServiceProvider();

        var service = new NavigationService(serviceProvider);

        // Act
        Action act = () => service.NavigateTo("Dashboard");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void NavigateTo_WithNullViewName_DoesNotThrow()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .BuildServiceProvider();

        var service = new NavigationService(serviceProvider);

        // Act
        Action act = () => service.NavigateTo(null!);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region NavigateToAsync Tests (2 tests)

    [Fact]
    public async Task NavigateToAsync_WithMissingViewModel_ReturnsCompletedTask()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .BuildServiceProvider();

        var service = new NavigationService(serviceProvider);

        // Act
        Func<Task> act = async () => await service.NavigateToAsync("Settings");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task NavigateToAsync_ReturnsCompletedTask()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .BuildServiceProvider();

        var service = new NavigationService(serviceProvider);

        // Act
        var task = service.NavigateToAsync("Alerts");

        // Assert
        task.Should().NotBeNull();
        task.IsCompleted.Should().BeTrue();
        await task; // Should complete without exception
    }

    #endregion
}
