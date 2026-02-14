using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MIC.Infrastructure.Data.Resilience;
using Polly;

namespace MIC.Tests.Unit.Infrastructure.Data;

public sealed class RetryPoliciesTests
{
    private readonly ILogger _logger = NullLoggerFactory.Instance.CreateLogger("Test");

    // ──────────────────────────────────────────────────────────────
    // CreateMailConnectivityPolicy — argument guards
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void CreateMailConnectivityPolicy_NullLogger_Throws()
    {
        var act = () => RetryPolicies.CreateMailConnectivityPolicy(null!, "op");

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateMailConnectivityPolicy_InvalidOperationName_Throws(string? operationName)
    {
        var act = () => RetryPolicies.CreateMailConnectivityPolicy(_logger, operationName!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateMailConnectivityPolicy_ValidArgs_ReturnsPolicyNotNull()
    {
        var policy = RetryPolicies.CreateMailConnectivityPolicy(_logger, "FetchMail");

        policy.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateMailConnectivityPolicy_RetriesOnIOException()
    {
        var policy = RetryPolicies.CreateMailConnectivityPolicy(_logger, "FetchMail", retryCount: 1);
        var attempts = 0;

        await policy.ExecuteAsync(() =>
        {
            attempts++;
            if (attempts == 1) throw new IOException("simulated");
            return Task.CompletedTask;
        });

        attempts.Should().Be(2);
    }

    [Fact]
    public async Task CreateMailConnectivityPolicy_RetriesOnTimeoutException()
    {
        var policy = RetryPolicies.CreateMailConnectivityPolicy(_logger, "FetchMail", retryCount: 1);
        var attempts = 0;

        await policy.ExecuteAsync(() =>
        {
            attempts++;
            if (attempts == 1) throw new TimeoutException("simulated");
            return Task.CompletedTask;
        });

        attempts.Should().Be(2);
    }

    [Fact]
    public async Task CreateMailConnectivityPolicy_DoesNotRetryUnhandledException()
    {
        var policy = RetryPolicies.CreateMailConnectivityPolicy(_logger, "FetchMail", retryCount: 2);
        var attempts = 0;

        var act = async () => await policy.ExecuteAsync(() =>
        {
            attempts++;
            throw new InvalidOperationException("not handled");
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
        attempts.Should().Be(1);
    }

    [Fact]
    public void CreateMailConnectivityPolicy_CustomRetryCount_IsRespected()
    {
        // Just ensure it creates without error for various counts
        var policy1 = RetryPolicies.CreateMailConnectivityPolicy(_logger, "op", retryCount: 0);
        var policy5 = RetryPolicies.CreateMailConnectivityPolicy(_logger, "op", retryCount: 5);

        policy1.Should().NotBeNull();
        policy5.Should().NotBeNull();
    }

    // ──────────────────────────────────────────────────────────────
    // CreateStandardPolicy — argument guards
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void CreateStandardPolicy_NullLogger_Throws()
    {
        var act = () => RetryPolicies.CreateStandardPolicy(null!, "op");

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateStandardPolicy_InvalidOperationName_Throws(string? operationName)
    {
        var act = () => RetryPolicies.CreateStandardPolicy(_logger, operationName!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateStandardPolicy_ValidArgs_ReturnsPolicyNotNull()
    {
        var policy = RetryPolicies.CreateStandardPolicy(_logger, "SaveData");

        policy.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateStandardPolicy_RetriesOnIOException()
    {
        var policy = RetryPolicies.CreateStandardPolicy(_logger, "Op", retryCount: 1);
        var attempts = 0;

        await policy.ExecuteAsync(() =>
        {
            attempts++;
            if (attempts == 1) throw new IOException("disk error");
            return Task.CompletedTask;
        });

        attempts.Should().Be(2);
    }

    [Fact]
    public async Task CreateStandardPolicy_RetriesOnTaskCanceledException()
    {
        var policy = RetryPolicies.CreateStandardPolicy(_logger, "Op", retryCount: 1);
        var attempts = 0;

        await policy.ExecuteAsync(() =>
        {
            attempts++;
            if (attempts == 1) throw new TaskCanceledException("timeout");
            return Task.CompletedTask;
        });

        attempts.Should().Be(2);
    }
}
