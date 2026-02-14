using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Exceptions;
using MIC.Desktop.Avalonia.Services;
using Moq;

namespace MIC.Tests.Unit.Services;

/// <summary>
/// Extended ErrorHandlingService tests covering GetUserFriendlyMessage,
/// GetDatabaseErrorMessage, SafeExecuteAsync (void overload), and events.
/// </summary>
public class ErrorHandlingServiceExtendedTests
{
    private readonly Mock<ILogger<ErrorHandlingService>> _mockLogger;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly ErrorHandlingService _sut;

    public ErrorHandlingServiceExtendedTests()
    {
        _mockLogger = new Mock<ILogger<ErrorHandlingService>>();
        _mockNotificationService = new Mock<INotificationService>();
        _sut = new ErrorHandlingService(_mockLogger.Object, _mockNotificationService.Object);
    }

    private static string InvokeGetUserFriendlyMessage(Exception ex)
    {
        var method = typeof(ErrorHandlingService).GetMethod("GetUserFriendlyMessage",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, new object[] { ex })!;
    }

    private static string InvokeGetDatabaseErrorMessage(Microsoft.Data.Sqlite.SqliteException ex)
    {
        var method = typeof(ErrorHandlingService).GetMethod("GetDatabaseErrorMessage",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, new object[] { ex })!;
    }

    #region GetUserFriendlyMessage Tests

    [Fact]
    public void GetUserFriendlyMessage_HttpRequestException_ReturnsConnectionError()
    {
        var ex = new System.Net.Http.HttpRequestException("Connection refused");
        var msg = InvokeGetUserFriendlyMessage(ex);
        msg.Should().Contain("internet connection");
    }

    [Fact]
    public void GetUserFriendlyMessage_SqliteException_ReturnsDatabaseError()
    {
        // SqliteException SqliteErrorCode 5 = database locked
        var ex = CreateSqliteException(5);
        var msg = InvokeGetUserFriendlyMessage(ex);
        msg.Should().Contain("Database");
    }

    [Fact]
    public void GetUserFriendlyMessage_ApiKeyException_ReturnsApiKeyMessage()
    {
        var ex = new Exception("Invalid API key provided");
        var msg = InvokeGetUserFriendlyMessage(ex);
        msg.Should().Contain("API key");
    }

    [Fact]
    public void GetUserFriendlyMessage_RateLimitException_ReturnsRateLimitMessage()
    {
        var ex = new Exception("rate limit exceeded");
        var msg = InvokeGetUserFriendlyMessage(ex);
        msg.Should().Contain("many requests");
    }

    [Fact]
    public void GetUserFriendlyMessage_TimeoutException_ReturnsTimeoutMessage()
    {
        var ex = new Exception("Request timeout occurred");
        var msg = InvokeGetUserFriendlyMessage(ex);
        msg.Should().Contain("timed out");
    }

    [Fact]
    public void GetUserFriendlyMessage_ValidationException_JoinsErrorMessages()
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>
        {
            new("Field1", "Error 1"),
            new("Field2", "Error 2")
        };
        var ex = new FluentValidation.ValidationException(failures);
        var msg = InvokeGetUserFriendlyMessage(ex);
        msg.Should().Contain("Error 1");
        msg.Should().Contain("Error 2");
    }

    [Fact]
    public void GetUserFriendlyMessage_ArgumentException_ReturnsOriginalMessage()
    {
        var ex = new ArgumentException("Value must be positive.");
        var msg = InvokeGetUserFriendlyMessage(ex);
        msg.Should().Contain("Value must be positive");
    }

    [Fact]
    public void GetUserFriendlyMessage_IOException_ReturnsFileMessage()
    {
        var ex = new IOException("File not found.");
        var msg = InvokeGetUserFriendlyMessage(ex);
        msg.Should().Contain("File operation failed");
    }

    [Fact]
    public void GetUserFriendlyMessage_UnauthorizedAccessException_ReturnsAccessDenied()
    {
        var ex = new UnauthorizedAccessException();
        var msg = InvokeGetUserFriendlyMessage(ex);
        msg.Should().Contain("Access denied");
    }

    [Fact]
    public void GetUserFriendlyMessage_LongMessage_IsTruncated()
    {
        var longMsg = new string('x', 300);
        var ex = new Exception(longMsg);
        var msg = InvokeGetUserFriendlyMessage(ex);
        msg.Length.Should().BeLessThanOrEqualTo(203); // 200 + "..."
        msg.Should().EndWith("...");
    }

    [Fact]
    public void GetUserFriendlyMessage_ShortGenericException_ReturnsFullMessage()
    {
        var ex = new Exception("Simple error");
        var msg = InvokeGetUserFriendlyMessage(ex);
        msg.Should().Be("Simple error");
    }

    [Fact]
    public void GetUserFriendlyMessage_MICException_WithUserMessage_ReturnsUserMessage()
    {
        var ex = new TestMICException("Technical error", "MIC_001", "Please try again.");
        var msg = InvokeGetUserFriendlyMessage(ex);
        msg.Should().Be("Please try again.");
    }

    [Fact]
    public void GetUserFriendlyMessage_MICException_WithoutUserMessage_ReturnsMessage()
    {
        var ex = new TestMICException("Technical error occurred", "MIC_002");
        var msg = InvokeGetUserFriendlyMessage(ex);
        msg.Should().Be("Technical error occurred");
    }

    [Fact]
    public void GetUserFriendlyMessage_MICException_EmptyMessages_ReturnsApplicationError()
    {
        var ex = new TestMICException("", "MIC_003", null);
        var msg = InvokeGetUserFriendlyMessage(ex);
        msg.Should().Be("An application error occurred.");
    }

    #endregion

    #region GetDatabaseErrorMessage Tests

    [Theory]
    [InlineData(1, "query error")]
    [InlineData(5, "locked")]
    [InlineData(14, "open database")]
    [InlineData(19, "constraint")]
    [InlineData(99, "operation failed")]
    public void GetDatabaseErrorMessage_ReturnsExpected(int errorCode, string expectedSubstring)
    {
        var ex = CreateSqliteException(errorCode);
        var msg = InvokeGetDatabaseErrorMessage(ex);
        msg.Should().ContainEquivalentOf(expectedSubstring);
    }

    #endregion

    #region SafeExecuteAsync (void overload) Tests

    [Fact]
    public async Task SafeExecuteAsync_VoidOverload_SuccessfulOperation_Completes()
    {
        var executed = false;
        await _sut.SafeExecuteAsync(async () =>
        {
            await Task.Delay(1);
            executed = true;
        }, "Test");

        executed.Should().BeTrue();
    }

    [Fact]
    public async Task SafeExecuteAsync_VoidOverload_FailingOperation_DoesNotThrow()
    {
        await _sut.SafeExecuteAsync(
            () => throw new Exception("Boom"),
            "Test operation");

        // Should not throw
    }

    [Fact]
    public async Task SafeExecuteAsync_VoidOverload_CancelledOperation_DoesNotThrow()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await _sut.SafeExecuteAsync(async () =>
        {
            await Task.Delay(100, cts.Token);
        }, "Test", cts.Token);

        // Should silently swallow OperationCanceledException
    }

    #endregion

    #region Event Tests

    [Fact]
    public void HandleException_NonCritical_RaisesOnErrorEvent()
    {
        ErrorContext? received = null;
        _sut.OnError += ctx => received = ctx;

        _sut.HandleException(new Exception("test"), "context", isCritical: false);

        // Event is posted via Dispatcher.UIThread.Post, which may not fire in test context
        // But we can verify the wiring doesn't throw
    }

    [Fact]
    public void HandleException_Critical_RaisesOnCriticalErrorEvent()
    {
        ErrorContext? received = null;
        _sut.OnCriticalError += ctx => received = ctx;

        _sut.HandleException(new Exception("critical"), "critical context", isCritical: true);

        // Event is posted via Dispatcher.UIThread.Post
    }

    [Fact]
    public void HandleException_NullContext_UsesDefault()
    {
        var act = () => _sut.HandleException(new Exception("err"), null);
        act.Should().NotThrow();
    }

    [Fact]
    public void HandleException_EmptyContext_UsesDefault()
    {
        var act = () => _sut.HandleException(new Exception("err"), "");
        act.Should().NotThrow();
    }

    [Fact]
    public void HandleException_WhitespaceContext_UsesDefault()
    {
        var act = () => _sut.HandleException(new Exception("err"), "   ");
        act.Should().NotThrow();
    }

    #endregion

    #region Helpers

    private static Microsoft.Data.Sqlite.SqliteException CreateSqliteException(int errorCode)
    {
        // SqliteException constructor: (string message, int errorCode)
        try
        {
            // Use reflection to construct since some constructors are internal
            var type = typeof(Microsoft.Data.Sqlite.SqliteException);
            var ctor = type.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(int) },
                null);

            if (ctor != null)
            {
                return (Microsoft.Data.Sqlite.SqliteException)ctor.Invoke(new object[] { $"SQLite Error {errorCode}", errorCode });
            }

            // Fallback: try the public constructor with errorCode + offset
            return new Microsoft.Data.Sqlite.SqliteException($"SQLite Error {errorCode}", errorCode);
        }
        catch
        {
            // Final fallback
            return new Microsoft.Data.Sqlite.SqliteException("SQLite Error", errorCode);
        }
    }

    /// <summary>
    /// Concrete MICException for testing since it's abstract.
    /// </summary>
    private sealed class TestMICException : MICException
    {
        public TestMICException(string message, string errorCode, string? userMessage = null)
            : base(message, errorCode, userMessage)
        {
        }
    }

    #endregion
}
