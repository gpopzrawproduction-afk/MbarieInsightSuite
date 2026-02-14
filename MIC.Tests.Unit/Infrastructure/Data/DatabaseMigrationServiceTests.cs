using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MIC.Infrastructure.Data.Persistence;
using MIC.Infrastructure.Data.Services;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Data;

/// <summary>
/// Tests for <see cref="DatabaseMigrationService"/> using InMemory EF Core.
/// InMemory has no real migrations, so we verify code paths (empty results, connectivity, exception handling).
/// </summary>
public class DatabaseMigrationServiceTests : IDisposable
{
    private readonly MicDbContext _context;
    private readonly ILogger<DatabaseMigrationService> _logger;
    private readonly DatabaseMigrationService _service;

    public DatabaseMigrationServiceTests()
    {
        var options = new DbContextOptionsBuilder<MicDbContext>()
            .UseInMemoryDatabase($"MigrationSvc_{Guid.NewGuid()}")
            .Options;
        _context = new MicDbContext(options);
        _logger = Substitute.For<ILogger<DatabaseMigrationService>>();
        _service = new DatabaseMigrationService(_context, _logger);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void Constructor_ThrowsOnNullContext()
    {
        var act = () => new DatabaseMigrationService(null!, _logger);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        var act = () => new DatabaseMigrationService(_context, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CanConnectAsync_ReturnsTrue_ForInMemoryDb()
    {
        var result = await _service.CanConnectAsync();
        result.Should().BeTrue();
    }

    // Note: GetPendingMigrationsAsync, GetAppliedMigrationsAsync, and ApplyMigrationsAsync
    // are relational-only operations that throw InvalidOperationException with InMemory provider.
    // They are tested via integration tests with SQLite or PostgreSQL instead.

    [Fact]
    public async Task CanConnectAsync_ThrowsWhenContextDisposed()
    {
        _context.Dispose();
        var act = async () => await _service.CanConnectAsync();
        await act.Should().ThrowAsync<Exception>();
    }
}
