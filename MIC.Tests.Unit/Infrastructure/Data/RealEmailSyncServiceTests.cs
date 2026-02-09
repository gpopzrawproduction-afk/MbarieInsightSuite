using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.AI.Services;
using MIC.Infrastructure.Data.Services;
using Moq;

namespace MIC.Tests.Unit.Infrastructure.Data;

public class RealEmailSyncServiceTests
{
    private readonly Mock<IEmailRepository> _emailRepository = new();
    private readonly Mock<IEmailAccountRepository> _accountRepository = new();
    private readonly Mock<IEmailOAuth2Service> _oauth2Service = new();
    private readonly Mock<IEmailAnalysisService> _analysisService = new();
    private readonly Mock<IAttachmentStorageService> _attachmentStorage = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<RealEmailSyncService>> _logger = new();

    private RealEmailSyncService CreateService(IConfiguration? configuration = null)
    {
        configuration ??= new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailSync:InitialSyncMonths"] = "3"
            })
            .Build();

        return new RealEmailSyncService(
            _emailRepository.Object,
            _accountRepository.Object,
            _oauth2Service.Object,
            _analysisService.Object,
            _attachmentStorage.Object,
            _unitOfWork.Object,
            _logger.Object,
            configuration);
    }

    [Fact]
    public async Task SyncHistoricalEmailsAsync_WhenNoAccountsConfigured_ReturnsNoAccountStatus()
    {
        _accountRepository
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailAccount>());

        var settings = new MIC.Core.Domain.Settings.EmailSyncSettings();
        var result = await CreateService().SyncHistoricalEmailsAsync(Guid.NewGuid(), settings);

        result.Status.Should().Be(IEmailSyncService.SyncStatus.NoAccountsConfigured);
        result.EmailsSynced.Should().Be(0);
    }

    [Fact]
    public async Task SyncHistoricalEmailsAsync_WhenCancelledReportsNoWork()
    {
        var account = new EmailAccount("user@example.com", EmailProvider.Outlook, Guid.NewGuid());
        _accountRepository
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailAccount> { account });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var settings = new MIC.Core.Domain.Settings.EmailSyncSettings();
        var result = await CreateService().SyncHistoricalEmailsAsync(
            Guid.NewGuid(),
            settings,
            progress: null,
            cancellationToken: cts.Token);

        result.Status.Should().Be(IEmailSyncService.SyncStatus.Completed);
        result.EmailsSynced.Should().Be(0);
    }

    [Fact]
    public async Task SyncHistoricalEmailsAsync_WhenRepositoryThrows_ReturnsFailure()
    {
        _accountRepository
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db error"));

        var settings = new MIC.Core.Domain.Settings.EmailSyncSettings();
        var result = await CreateService().SyncHistoricalEmailsAsync(Guid.NewGuid(), settings);

        result.Status.Should().Be(IEmailSyncService.SyncStatus.Failed);
        result.Errors.Should().ContainSingle(e => e.Contains("db error"));
    }
}
