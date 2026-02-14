using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Persistence;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Data;

/// <summary>
/// Tests for <see cref="UnitOfWork"/> — SaveChangesAsync with InMemory EF Core
/// and transaction methods (commit/rollback when no transaction is active).
/// </summary>
public class UnitOfWorkTests : IDisposable
{
    private readonly MicDbContext _context;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<MicDbContext>()
            .UseInMemoryDatabase($"UoW_{Guid.NewGuid()}")
            .Options;
        _context = new MicDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsEntities()
    {
        var user = new User { Username = "uow_test", Email = "uow@test.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        var result = await _unitOfWork.SaveChangesAsync();
        result.Should().BeGreaterThan(0);

        var found = await _context.Users.FirstOrDefaultAsync(u => u.Username == "uow_test");
        found.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_ReturnsZero_WhenNoChanges()
    {
        var result = await _unitOfWork.SaveChangesAsync();
        result.Should().Be(0);
    }

    [Fact]
    public async Task CommitTransactionAsync_NoOps_WhenNoActiveTransaction()
    {
        // Should not throw when _transaction is null
        await _unitOfWork.CommitTransactionAsync();
    }

    [Fact]
    public async Task RollbackTransactionAsync_NoOps_WhenNoActiveTransaction()
    {
        // Should not throw when _transaction is null
        await _unitOfWork.RollbackTransactionAsync();
    }

    // Note: BeginTransactionAsync is not tested here because InMemoryDatabase
    // does not support transactions; it would throw InvalidOperationException.
}
