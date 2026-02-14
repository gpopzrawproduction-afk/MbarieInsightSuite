using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Persistence;
using MIC.Infrastructure.Data.Repositories;

namespace MIC.Tests.Unit.Infrastructure.Data;

public sealed class RepositoryTests : IDisposable
{
    private readonly MicDbContext _context;
    private readonly Repository<User> _sut;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MicDbContext>()
            .UseInMemoryDatabase($"MicDb_Repo_{Guid.NewGuid():N}")
            .Options;

        _context = new MicDbContext(options);
        _context.Database.EnsureCreated();
        _sut = new Repository<User>(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private static User CreateUser(string username = "repouser") 
    {
        var user = new User();
        user.SetCredentials(username, $"{username}@test.com");
        user.SetPasswordHash("Hash123!", "salt");
        return user;
    }

    // ──────────────────────────────────────────────────────────────
    // AddAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_AddsEntityToDbSet()
    {
        var user = CreateUser();
        
        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();

        var found = await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Username == "repouser");
        found.Should().NotBeNull();
    }

    // ──────────────────────────────────────────────────────────────
    // GetByIdAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsEntity_WhenExists()
    {
        var user = CreateUser("findme");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(user.Id);

        result.Should().NotBeNull();
        result!.Username.Should().Be("findme");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ──────────────────────────────────────────────────────────────
    // GetAllAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        _context.Users.Add(CreateUser("all1"));
        _context.Users.Add(CreateUser("all2"));
        await _context.SaveChangesAsync();

        var result = await _sut.GetAllAsync();

        result.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoEntities()
    {
        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────
    // UpdateAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_MarksEntityAsModified()
    {
        var user = CreateUser("update");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        _context.Entry(user).State = EntityState.Detached;

        user.SetCredentials("updated", "updated@test.com");
        await _sut.UpdateAsync(user);

        _context.Entry(user).State.Should().Be(EntityState.Modified);
    }

    // ──────────────────────────────────────────────────────────────
    // DeleteAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        var user = CreateUser("delete");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _sut.DeleteAsync(user);
        await _context.SaveChangesAsync();

        var found = await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Username == "delete");
        found.Should().BeNull();
    }
}
