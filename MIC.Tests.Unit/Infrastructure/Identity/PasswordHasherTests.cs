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
}
