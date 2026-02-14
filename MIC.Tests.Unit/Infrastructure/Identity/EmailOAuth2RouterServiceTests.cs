using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Identity.Services;
using NSubstitute;

namespace MIC.Tests.Unit.Infrastructure.Identity;

public sealed class EmailOAuth2RouterServiceTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEmailOAuth2Service _gmailService;
    private readonly IEmailOAuth2Service _outlookService;
    private readonly EmailOAuth2RouterService _sut;

    public EmailOAuth2RouterServiceTests()
    {
        _gmailService = Substitute.For<IEmailOAuth2Service>();
        _outlookService = Substitute.For<IEmailOAuth2Service>();

        _serviceProvider = Substitute.For<IServiceProvider, IKeyedServiceProvider>();
        var keyedProvider = (IKeyedServiceProvider)_serviceProvider;

        keyedProvider.GetKeyedService<IEmailOAuth2Service>("Gmail")
            .Returns(_gmailService);
        keyedProvider.GetKeyedService<IEmailOAuth2Service>("Outlook")
            .Returns(_outlookService);

        // Wire the extension method (GetKeyedService is an extension on IServiceProvider
        // but it calls IKeyedServiceProvider under the hood)
        _sut = new EmailOAuth2RouterService(_serviceProvider);
    }

    // For the GetKeyedService extension to work, we need the IServiceProvider
    // to also implement IKeyedServiceProvider. NSubstitute handles this with
    // Substitute.For<IServiceProvider, IKeyedServiceProvider>()

    // ──────────────────────────────────────────────────────────────
    // AuthenticateGmailAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AuthenticateGmailAsync_DelegatesToGmailService()
    {
        var expected = new OAuthResult { Success = true, AccessToken = "token", RefreshToken = "refresh", ExpiresAt = DateTime.UtcNow.AddHours(1) };
        _gmailService.AuthenticateGmailAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _sut.AuthenticateGmailAsync();

        result.Should().BeEquivalentTo(expected);
        await _gmailService.Received(1).AuthenticateGmailAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────
    // AuthenticateOutlookAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AuthenticateOutlookAsync_DelegatesToOutlookService()
    {
        var expected = new OAuthResult { Success = true, AccessToken = "token-ol", RefreshToken = "refresh-ol", ExpiresAt = DateTime.UtcNow.AddHours(1) };
        _outlookService.AuthenticateOutlookAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _sut.AuthenticateOutlookAsync();

        result.Should().BeEquivalentTo(expected);
        await _outlookService.Received(1).AuthenticateOutlookAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────
    // RefreshGmailTokenAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshGmailTokenAsync_DelegatesToGmailService()
    {
        _gmailService.RefreshGmailTokenAsync("refresh123", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("new-token"));

        var result = await _sut.RefreshGmailTokenAsync("refresh123");

        result.Should().Be("new-token");
    }

    // ──────────────────────────────────────────────────────────────
    // RefreshOutlookTokenAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshOutlookTokenAsync_DelegatesToOutlookService()
    {
        _outlookService.RefreshOutlookTokenAsync("ol-refresh", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("ol-new-token"));

        var result = await _sut.RefreshOutlookTokenAsync("ol-refresh");

        result.Should().Be("ol-new-token");
    }

    // ──────────────────────────────────────────────────────────────
    // ValidateTokenAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateTokenAsync_Gmail_DelegatesToGmailService()
    {
        _gmailService.ValidateTokenAsync("tok", EmailProvider.Gmail)
            .Returns(Task.FromResult(true));

        var result = await _sut.ValidateTokenAsync("tok", EmailProvider.Gmail);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTokenAsync_Outlook_DelegatesToOutlookService()
    {
        _outlookService.ValidateTokenAsync("tok", EmailProvider.Outlook)
            .Returns(Task.FromResult(true));

        var result = await _sut.ValidateTokenAsync("tok", EmailProvider.Outlook);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTokenAsync_UnknownProvider_ReturnsFalse()
    {
        var result = await _sut.ValidateTokenAsync("tok", (EmailProvider)999);

        result.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────
    // GetGmailAccessTokenAsync / GetOutlookAccessTokenAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetGmailAccessTokenAsync_DelegatesToGmailService()
    {
        _gmailService.GetGmailAccessTokenAsync("user@gmail.com", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("gmail-tok"));

        var result = await _sut.GetGmailAccessTokenAsync("user@gmail.com");

        result.Should().Be("gmail-tok");
    }

    [Fact]
    public async Task GetOutlookAccessTokenAsync_DelegatesToOutlookService()
    {
        _outlookService.GetOutlookAccessTokenAsync("user@outlook.com", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("ol-tok"));

        var result = await _sut.GetOutlookAccessTokenAsync("user@outlook.com");

        result.Should().Be("ol-tok");
    }

    // ──────────────────────────────────────────────────────────────
    // AuthorizeGmailAccountAsync / AuthorizeOutlookAccountAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AuthorizeGmailAccountAsync_DelegatesToGmailService()
    {
        _gmailService.AuthorizeGmailAccountAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var result = await _sut.AuthorizeGmailAccountAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeOutlookAccountAsync_DelegatesToOutlookService()
    {
        _outlookService.AuthorizeOutlookAccountAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var result = await _sut.AuthorizeOutlookAccountAsync();

        result.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────
    // GetRequiredService — missing registration
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRequiredService_ThrowsInvalidOperation_WhenServiceNotRegistered()
    {
        var emptyProvider = Substitute.For<IServiceProvider, IKeyedServiceProvider>();
        var keyedEmpty = (IKeyedServiceProvider)emptyProvider;
        keyedEmpty.GetKeyedService<IEmailOAuth2Service>(Arg.Any<string>())
            .Returns((IEmailOAuth2Service?)null);

        var sut = new EmailOAuth2RouterService(emptyProvider);

        var act = () => sut.AuthenticateGmailAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Gmail*not registered*");
    }
}
