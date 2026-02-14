using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Configuration;
using MIC.Infrastructure.Data.Persistence;
using MIC.Infrastructure.Data.Services;
using NSubstitute;

namespace MIC.Tests.Unit.Infrastructure.Data;

public sealed class DbInitializerTests : IDisposable
{
    private readonly MicDbContext _context;
    private readonly DatabaseMigrationService _migrationService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializerTests()
    {
        var options = new DbContextOptionsBuilder<MicDbContext>()
            .UseInMemoryDatabase($"MicDb_Init_{Guid.NewGuid():N}")
            .Options;

        _context = new MicDbContext(options);
        _context.Database.EnsureCreated();

        _migrationService = new DatabaseMigrationService(
            _context,
            NullLoggerFactory.Instance.CreateLogger<DatabaseMigrationService>());

        _passwordHasher = Substitute.For<IPasswordHasher>();
        _logger = NullLoggerFactory.Instance.CreateLogger<DbInitializer>();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private DbInitializer CreateSut(DatabaseSettings? settings = null)
    {
        var s = settings ?? new DatabaseSettings();
        return new DbInitializer(
            _context,
            Options.Create(s),
            _migrationService,
            _logger,
            _passwordHasher);
    }

    // ──────────────────────────────────────────────────────────────
    // Constructor guards
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullContext_Throws()
    {
        var act = () => new DbInitializer(
            null!,
            Options.Create(new DatabaseSettings()),
            _migrationService,
            _logger,
            _passwordHasher);

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void Constructor_NullSettings_Throws()
    {
        var act = () => new DbInitializer(
            _context,
            null!,
            _migrationService,
            _logger,
            _passwordHasher);

        act.Should().Throw<ArgumentNullException>().WithParameterName("settings");
    }

    [Fact]
    public void Constructor_NullMigrationService_Throws()
    {
        var act = () => new DbInitializer(
            _context,
            Options.Create(new DatabaseSettings()),
            null!,
            _logger,
            _passwordHasher);

        act.Should().Throw<ArgumentNullException>().WithParameterName("migrationService");
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var act = () => new DbInitializer(
            _context,
            Options.Create(new DatabaseSettings()),
            _migrationService,
            null!,
            _passwordHasher);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullPasswordHasher_Throws()
    {
        var act = () => new DbInitializer(
            _context,
            Options.Create(new DatabaseSettings()),
            _migrationService,
            _logger,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("passwordHasher");
    }

    [Fact]
    public void Constructor_ValidArgs_DoesNotThrow()
    {
        var act = () => CreateSut();

        act.Should().NotThrow();
    }

    // ──────────────────────────────────────────────────────────────
    // InitializeAsync — InMemory doesn't support migrations/relational  
    // operations, so we can only test constructor and SeedDataAsync
    // ──────────────────────────────────────────────────────────────

    // ──────────────────────────────────────────────────────────────
    // SeedDataAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SeedDataAsync_CompletesWithoutError()
    {
        var sut = CreateSut();

        var act = async () => await sut.SeedDataAsync();

        await act.Should().NotThrowAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // Private seed methods via reflection
    // ──────────────────────────────────────────────────────────────

    private async Task InvokeSeedMethod(string methodName, DbInitializer sut)
    {
        var method = typeof(DbInitializer).GetMethod(methodName,
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var paramCount = method.GetParameters().Length;
        var args = paramCount == 1 ? new object[] { CancellationToken.None } : Array.Empty<object>();
        await (Task)method.Invoke(sut, args)!;
    }

    [Fact]
    public async Task SeedAlertsAsync_AddsThreeAlerts()
    {
        var sut = CreateSut();

        await InvokeSeedMethod("SeedAlertsAsync", sut);
        await _context.SaveChangesAsync();

        _context.Alerts.Count().Should().Be(3);
    }

    [Fact]
    public async Task SeedAssetsAsync_AddsThreeAssets()
    {
        var sut = CreateSut();

        await InvokeSeedMethod("SeedAssetsAsync", sut);
        await _context.SaveChangesAsync();

        _context.Assets.Count().Should().Be(3);
    }

    [Fact]
    public async Task SeedDecisionsAsync_AddsDecisionWithOptions()
    {
        var sut = CreateSut();

        await InvokeSeedMethod("SeedDecisionsAsync", sut);
        await _context.SaveChangesAsync();

        _context.Decisions.Count().Should().Be(1);
    }

    [Fact]
    public async Task SeedMetricsAsync_AddsMetrics()
    {
        var sut = CreateSut();

        await InvokeSeedMethod("SeedMetricsAsync", sut);
        await _context.SaveChangesAsync();

        _context.Metrics.Count().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SeedRolesAsync_CompletesWithoutError()
    {
        var sut = CreateSut();

        var act = async () => await InvokeSeedMethod("SeedRolesAsync", sut);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SeedEmailsAsync_WhenNoUsers_SkipsSeeding()
    {
        var sut = CreateSut();

        await InvokeSeedMethod("SeedEmailsAsync", sut);
        await _context.SaveChangesAsync();

        _context.EmailMessages.Count().Should().Be(0);
    }

    [Fact]
    public async Task SeedEmailsAsync_WhenUserExists_CreatesEmailsAndAccount()
    {
        // Seed a user first
        var user = new User();
        user.SetCredentials("testuser", "test@example.com");
        user.SetPasswordHash("hash", "salt");
        user.UpdateProfile("Test User");
        user.SetRole(UserRole.User);
        user.Activate();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var sut = CreateSut();

        await InvokeSeedMethod("SeedEmailsAsync", sut);
        await _context.SaveChangesAsync();

        _context.EmailAccounts.Count().Should().Be(1);
        _context.EmailMessages.Count().Should().BeGreaterThan(0);
    }
}
