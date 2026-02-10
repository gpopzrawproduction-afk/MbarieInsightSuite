using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Exceptions;
using MIC.Desktop.Avalonia.Services;
using Moq;
using Xunit;

namespace MIC.Tests.Unit.Services;

/// <summary>
/// Comprehensive tests for ErrorHandlingService.
/// Tests exception handling, logging, notifications, and safe execution patterns.
/// Target: 16 tests for critical error handling scenarios
/// </summary>
public class ErrorHandlingServiceTests
{
    private readonly Mock<ILogger<ErrorHandlingService>> _mockLogger;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly ErrorHandlingService _sut;

    public ErrorHandlingServiceTests()
    {
        _mockLogger = new Mock<ILogger<ErrorHandlingService>>();
        _mockNotificationService = new Mock<INotificationService>();
        _sut = new ErrorHandlingService(_mockLogger.Object, _mockNotificationService.Object);
    }

    #region HandleException Tests (2 tests - UI-independent)

    [Fact]
    public void HandleException_WithRegularException_DoesNotThrow()
    {
        // Arrange
        var exception = new Exception("Test error");
        var context = "Test context";

        // Act
        Action act = () => _sut.HandleException(exception, context, isCritical: false);

        // Assert - Just verify it doesn't throw
        act.Should().NotThrow();
    }

    [Fact]
    public void HandleException_WithCriticalException_DoesNotThrow()
    {
        // Arrange
        var exception = new Exception("Critical error");
        var context = "Critical operation failed";

        // Act
        Action act = () => _sut.HandleException(exception, context, isCritical: true);

        // Assert - Just verify it doesn't throw
        act.Should().NotThrow();
    }

    #endregion

    #region SafeExecuteAsync Tests (6 tests)

    [Fact]
    public async Task SafeExecuteAsync_WithSuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var expectedResult = 42;
        Func<Task<int>> operation = () => Task.FromResult(expectedResult);

        // Act
        var result = await _sut.SafeExecuteAsync(operation, "Test operation");

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task SafeExecuteAsync_WithFailingOperation_ReturnsDefaultValue()
    {
        // Arrange
        var defaultValue = 99;
        Func<Task<int>> operation = () => throw new Exception("Operation failed");

        // Act
        var result = await _sut.SafeExecuteAsync(operation, "Test operation", defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public async Task SafeExecuteAsync_WithFailingOperation_LogsError()
    {
        // Arrange
        Func<Task<int>> operation = () => throw new Exception("Test failure");

        // Act
        await _sut.SafeExecuteAsync(operation, "Test context", 0);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task SafeExecuteAsync_WithCancellationRequested_ReturnsDefaultValue()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var defaultValue = 99;
        Func<Task<int>> operation = async () =>
        {
            await Task.Delay(100, cts.Token);
            return 42;
        };

        // Act
        var result = await _sut.SafeExecuteAsync(operation, "Test", defaultValue, cts.Token);

        // Assert - Should return default value when cancelled
        result.Should().Be(defaultValue);
    }

    [Fact]
    public async Task SafeExecuteAsync_WithNullDefaultValue_ReturnsNullOnFailure()
    {
        // Arrange
        Func<Task<string>> operation = () => throw new Exception("Failed");

        // Act
        var result = await _sut.SafeExecuteAsync(operation, "Test", defaultValue: null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SafeExecuteAsync_WithTaskDelay_CompletesSuccessfully()
    {
        // Arrange
        Func<Task<string>> operation = async () =>
        {
            await Task.Delay(10);
            return "Success";
        };

        // Act
        var result = await _sut.SafeExecuteAsync(operation, "Async test");

        // Assert
        result.Should().Be("Success");
    }

    #endregion

    #region SafeExecute Tests (4 tests)

    [Fact]
    public void SafeExecute_WithSuccessfulOperation_ReturnsResult()
    {
        // Arrange
        Func<int> operation = () => 42;

        // Act
        var result = _sut.SafeExecute(operation, "Test operation");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void SafeExecute_WithFailingOperation_ReturnsDefaultValue()
    {
        // Arrange
        var defaultValue = 99;
        Func<int> operation = () => throw new Exception("Failed");

        // Act
        var result = _sut.SafeExecute(operation, "Test operation", defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void SafeExecute_WithFailingOperation_LogsError()
    {
        // Arrange
        Func<int> operation = () => throw new Exception("Test error");

        // Act
        _sut.SafeExecute(operation, "Test context", 0);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public void SafeExecute_WithComplexOperation_HandlesSuccessfully()
    {
        // Arrange
        Func<string> operation = () =>
        {
            var result = "Complex";
            result += " Operation";
            return result;
        };

        // Act
        var result = _sut.SafeExecute(operation, "Complex test");

        // Assert
        result.Should().Be("Complex Operation");
    }

    #endregion
}
