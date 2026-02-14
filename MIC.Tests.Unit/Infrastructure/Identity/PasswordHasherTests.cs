using FluentAssertions;
using MIC.Infrastructure.Identity.Services;

namespace MIC.Tests.Unit.Infrastructure.Identity;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void HashPassword_WithEmptyInput_Throws()
    {
        Action act = () => _sut.HashPassword(string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HashPassword_GeneratesDifferentSaltEachTime()
    {
        var first = _sut.HashPassword("MyStrongPassword!");
        var second = _sut.HashPassword("MyStrongPassword!");

        first.hash.Should().NotBeNullOrEmpty();
        first.salt.Should().NotBeNullOrEmpty();
        second.salt.Should().NotBe(first.salt);
    }

    [Fact]
    public void VerifyPassword_WithValidCredentials_ReturnsTrue()
    {
        var (hash, salt) = _sut.HashPassword("Pa$$w0rd!");

        var success = _sut.VerifyPassword("Pa$$w0rd!", hash, salt);

        success.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        var (hash, salt) = _sut.HashPassword("CorrectHorseBatteryStaple");

        var success = _sut.VerifyPassword("WrongHorseBatteryStaple", hash, salt);

        success.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithInvalidBase64_ReturnsFalse()
    {
        var success = _sut.VerifyPassword("password", "not-base64", "still-not-base64");

        success.Should().BeFalse();
    }

    [Fact]
    public void HashPassword_WithNullPassword_ThrowsArgumentException()
    {
        Action act = () => _sut.HashPassword(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HashPassword_WithWhitespacePassword_ThrowsArgumentException()
    {
        Action act = () => _sut.HashPassword("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HashPassword_WithVeryLongPassword_Succeeds()
    {
        // Arrange
        var longPassword = new string('a', 10000); // 10KB password

        // Act
        var result = _sut.HashPassword(longPassword);

        // Assert
        result.hash.Should().NotBeNullOrEmpty();
        result.salt.Should().NotBeNullOrEmpty();
        _sut.VerifyPassword(longPassword, result.hash, result.salt).Should().BeTrue();
    }

    [Fact]
    public void HashPassword_WithSpecialCharacters_Succeeds()
    {
        // Arrange
        var specialPassword = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";

        // Act
        var result = _sut.HashPassword(specialPassword);

        // Assert
        _sut.VerifyPassword(specialPassword, result.hash, result.salt).Should().BeTrue();
    }

    [Fact]
    public void HashPassword_WithUnicodeCharacters_Succeeds()
    {
        // Arrange
        var unicodePassword = "пароль密码🔐مرور";

        // Act
        var result = _sut.HashPassword(unicodePassword);

        // Assert
        _sut.VerifyPassword(unicodePassword, result.hash, result.salt).Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_IsCaseSensitive()
    {
        // Arrange
        var password = "Password123";
        var (hash, salt) = _sut.HashPassword(password);

        // Act & Assert
        _sut.VerifyPassword("password123", hash, salt).Should().BeFalse();
        _sut.VerifyPassword("PASSWORD123", hash, salt).Should().BeFalse();
        _sut.VerifyPassword(password, hash, salt).Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithNullPassword_ReturnsFalse()
    {
        var (hash, salt) = _sut.HashPassword("ValidPassword");

        var success = _sut.VerifyPassword(null!, hash, salt);

        success.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithEmptyPassword_ReturnsFalse()
    {
        var (hash, salt) = _sut.HashPassword("ValidPassword");

        var success = _sut.VerifyPassword("", hash, salt);

        success.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithNullHash_ReturnsFalse()
    {
        var (_, salt) = _sut.HashPassword("ValidPassword");

        var success = _sut.VerifyPassword("ValidPassword", null!, salt);

        success.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithEmptyHash_ReturnsFalse()
    {
        var (_, salt) = _sut.HashPassword("ValidPassword");

        var success = _sut.VerifyPassword("ValidPassword", "", salt);

        success.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithNullSalt_ReturnsFalse()
    {
        var (hash, _) = _sut.HashPassword("ValidPassword");

        var success = _sut.VerifyPassword("ValidPassword", hash, null!);

        success.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithEmptySalt_ReturnsFalse()
    {
        var (hash, _) = _sut.HashPassword("ValidPassword");

        var success = _sut.VerifyPassword("ValidPassword", hash, "");

        success.Should().BeFalse();
    }

    [Fact]
    public void HashPassword_ProducesArgon2idHash()
    {
        // Arrange & Act
        var (hash, salt) = _sut.HashPassword("TestPassword");

        // Assert - Argon2id hashes are base64 encoded and should be reasonable length
        hash.Should().NotBeNullOrEmpty();
        hash.Length.Should().BeGreaterThan(40); // Argon2 produces substantial output
        Convert.FromBase64String(hash).Should().NotBeEmpty(); // Valid base64
    }

    [Fact]
    public void HashPassword_GeneratesUniqueSaltForSamePassword()
    {
        // Arrange
        var password = "SamePassword";

        // Act
        var results = Enumerable.Range(0, 5)
            .Select(_ => _sut.HashPassword(password))
            .ToList();

        // Assert - All salts should be unique
        results.Select(r => r.salt).Distinct().Should().HaveCount(5);
        results.Select(r => r.hash).Distinct().Should().HaveCount(5);
    }

    [Fact]
    public void VerifyPassword_WithWrongSalt_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword";
        var (hash1, _) = _sut.HashPassword(password);
        var (_, salt2) = _sut.HashPassword(password);

        // Act - Use hash from first, salt from second
        var success = _sut.VerifyPassword(password, hash1, salt2);

        // Assert
        success.Should().BeFalse();
    }

    [Fact]
    public void HashPassword_WithMinimalPassword_Succeeds()
    {
        // Arrange
        var minimalPassword = "a";

        // Act
        var result = _sut.HashPassword(minimalPassword);

        // Assert
        result.hash.Should().NotBeNullOrEmpty();
        result.salt.Should().NotBeNullOrEmpty();
        _sut.VerifyPassword(minimalPassword, result.hash, result.salt).Should().BeTrue();
    }
}
