using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using MailKit;
using Microsoft.Extensions.Logging;
using Moq;
using Polly.Utilities;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Resilience;

public class RetryPoliciesTests
{
    [Fact]
    public async Task CreateMailConnectivityPolicy_RetriesAndLogsWarnings()
    {
        var loggerMock = new Mock<ILogger>();
        var originalSleep = SystemClock.SleepAsync;
        SystemClock.SleepAsync = (_, _) => Task.CompletedTask;

        try
        {
            var policy = MIC.Infrastructure.Data.Resilience.RetryPolicies.CreateMailConnectivityPolicy(loggerMock.Object, "IMAP Connect", retryCount: 2);
            var attempts = 0;

            await Assert.ThrowsAsync<IOException>(async () =>
            {
                await policy.ExecuteAsync(() =>
                {
                    attempts++;
                    return Task.FromException(new IOException("fail"));
                });
            });

            attempts.Should().Be(3);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Retry") && state.ToString()!.Contains("IMAP Connect")),
                    It.IsAny<IOException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Exactly(2));
        }
        finally
        {
            SystemClock.SleepAsync = originalSleep;
        }
    }

    [Fact]
    public async Task CreateStandardPolicy_RetriesTaskCanceledException()
    {
        var loggerMock = new Mock<ILogger>();
        var originalSleep = SystemClock.SleepAsync;
        SystemClock.SleepAsync = (_, _) => Task.CompletedTask;

        try
        {
            var policy = MIC.Infrastructure.Data.Resilience.RetryPolicies.CreateStandardPolicy(loggerMock.Object, "Fetch Alerts", retryCount: 1);
            var attempts = 0;

            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await policy.ExecuteAsync(() =>
                {
                    attempts++;
                    return Task.FromException(new TaskCanceledException());
                });
            });

            attempts.Should().Be(2);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Fetch Alerts")),
                    It.IsAny<TaskCanceledException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            SystemClock.SleepAsync = originalSleep;
        }
    }

    [Fact]
    public void CreateMailConnectivityPolicy_WithInvalidArguments_Throws()
    {
        var loggerMock = new Mock<ILogger>().Object;

        Action actNullLogger = () => MIC.Infrastructure.Data.Resilience.RetryPolicies.CreateMailConnectivityPolicy(null!, "Op");
        Action actNullName = () => MIC.Infrastructure.Data.Resilience.RetryPolicies.CreateMailConnectivityPolicy(loggerMock, " ");

        actNullLogger.Should().Throw<ArgumentNullException>();
        actNullName.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateStandardPolicy_WithInvalidArguments_Throws()
    {
        var loggerMock = new Mock<ILogger>().Object;

        Action actNullLogger = () => MIC.Infrastructure.Data.Resilience.RetryPolicies.CreateStandardPolicy(null!, "Op");
        Action actNullName = () => MIC.Infrastructure.Data.Resilience.RetryPolicies.CreateStandardPolicy(loggerMock, " ");

        actNullLogger.Should().Throw<ArgumentNullException>();
        actNullName.Should().Throw<ArgumentException>();
    }
}
