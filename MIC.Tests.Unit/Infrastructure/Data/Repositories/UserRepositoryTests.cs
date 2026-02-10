using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Persistence;
using MIC.Infrastructure.Data.Repositories;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Data.Repositories;

/// <summary>
/// Comprehensive tests for UserRepository.
/// Tests CRUD operations, username/email lookups, and existence checks.
/// Target: 16 tests for user repository coverage
/// </summary>
public class UserRepositoryTests : IDisposable
{
    private readonly MicDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MicDbContext>()
            .UseInMemoryDatabase(databaseName: $"UserRepositoryTest_{Guid.NewGuid()}")
            .Options;

        _context = new MicDbContext(options);
        _repository = new UserRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetByIdAsync Tests (3 tests)

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsUser()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com");
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyGuid_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _repository.GetByIdAsync(Guid.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("id");
    }

    #endregion

    #region GetTrackedByIdAsync Tests (2 tests)

    [Fact]
    public async Task GetTrackedByIdAsync_WithExistingId_ReturnsTrackedUser()
    {
        // Arrange
        var user = CreateTestUser("tracked", "tracked@example.com");
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetTrackedByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        _context.Entry(result).State.Should().Be(EntityState.Unchanged);
    }

    [Fact]
    public async Task GetTrackedByIdAsync_WithEmptyGuid_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _repository.GetTrackedByIdAsync(Guid.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("id");
    }

    #endregion

    #region GetByUsernameAsync Tests (3 tests)

    [Fact]
    public async Task GetByUsernameAsync_WithExactMatch_ReturnsUser()
    {
        // Arrange
        var user = CreateTestUser("johndoe", "john@example.com");
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetByUsernameAsync("johndoe");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("johndoe");
    }

    [Fact]
    public async Task GetByUsernameAsync_IsCaseInsensitive()
    {
        // Arrange
        var user = CreateTestUser("JohnDoe", "john@example.com");
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetByUsernameAsync("johndoe");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("JohnDoe");
    }

    [Fact]
    public async Task GetByUsernameAsync_WithNullOrWhitespace_ThrowsArgumentException()
    {
        // Act
        Func<Task> actNull = async () => await _repository.GetByUsernameAsync(null!);
        Func<Task> actEmpty = async () => await _repository.GetByUsernameAsync("");
        Func<Task> actWhitespace = async () => await _repository.GetByUsernameAsync("   ");

        // Assert
        await actNull.Should().ThrowAsync<ArgumentException>().WithParameterName("username");
        await actEmpty.Should().ThrowAsync<ArgumentException>().WithParameterName("username");
        await actWhitespace.Should().ThrowAsync<ArgumentException>().WithParameterName ("username");
    }

    #endregion

    #region GetByEmailAsync Tests (3 tests)

    [Fact]
    public async Task GetByEmailAsync_WithExactMatch_ReturnsUser()
    {
        // Arrange
        var user = CreateTestUser("user1", "user1@example.com");
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetByEmailAsync("user1@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("user1@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_IsCaseInsensitive()
    {
        // Arrange
        var user = CreateTestUser("user2", "User2@Example.COM");
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetByEmailAsync("user2@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("User2@Example.COM");
    }

    [Fact]
    public async Task GetByEmailAsync_WithNullOrWhitespace_ThrowsArgumentException()
    {
        // Act
        Func<Task> actNull = async () => await _repository.GetByEmailAsync(null!);
        Func<Task> actEmpty = async () => await _repository.GetByEmailAsync("");

        // Assert
        await actNull.Should().ThrowAsync<ArgumentException>().WithParameterName("email");
        await actEmpty.Should().ThrowAsync<ArgumentException>().WithParameterName("email");
    }

    #endregion

    #region UsernameExistsAsync & EmailExistsAsync Tests (3 tests)

    [Fact]
    public async Task UsernameExistsAsync_WithExistingUsername_ReturnsTrue()
    {
        // Arrange
        var user = CreateTestUser("existinguser", "exists@example.com");
        await _repository.CreateAsync(user);

        // Act
        var exists = await _repository.UsernameExistsAsync("existinguser");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_WithExistingEmail_ReturnsTrue()
    {
        // Arrange
        var user = CreateTestUser("user", "existing@example.com");
        await _repository.CreateAsync(user);

        // Act
        var exists = await _repository.EmailExistsAsync("existing@example.com");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNullOrWhitespace_ReturnsFalse()
    {
        // Act
        var usernameNull = await _repository.UsernameExistsAsync(null!);
        var usernameEmpty = await _repository.UsernameExistsAsync("");
        var emailNull = await _repository.EmailExistsAsync(null!);
        var emailEmpty = await _repository.EmailExistsAsync("");

        // Assert
        usernameNull.Should().BeFalse();
        usernameEmpty.Should().BeFalse();
        emailNull.Should().BeFalse();
        emailEmpty.Should().BeFalse();
    }

    #endregion

    #region CreateAsync & UpdateAsync Tests (2 tests)

    [Fact]
    public async Task CreateAsync_WithValidUser_SavesAndReturnsUser()
    {
        // Arrange
        var user = CreateTestUser("newuser", "new@example.com");

        // Act
        var result = await _repository.CreateAsync(user);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);

        var saved = await _context.Users.FindAsync(result.Id);
        saved.Should().NotBeNull();
        saved!.Username.Should().Be("newuser");
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedUser_PersistsChanges()
    {
        // Arrange
        var user = CreateTestUser("originalname", "original@example.com");
        await _repository.CreateAsync(user);

        // Clear tracker to simulate getting entity from different context
        _context.ChangeTracker.Clear();

        // Modify user (get untracked version)
        var userToUpdate = await _repository.GetByIdAsync(user.Id);
        userToUpdate!.FullName = "Updated Name";
        userToUpdate.Department = "Engineering";

        // Act
        await _repository.UpdateAsync(userToUpdate);

        // Assert
        _context.ChangeTracker.Clear(); // Clear to get fresh from DB
        var updated = await _context.Users.FindAsync(user.Id);
        updated!.FullName.Should().Be("Updated Name");
        updated.Department.Should().Be("Engineering");
    }

    #endregion

    #region Helper Methods

    private User CreateTestUser(string username, string email)
    {
        return new User
        {
            Username = username,
            Email = email,
            PasswordHash = "hashed_password",
            Salt = "salt_value",
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
