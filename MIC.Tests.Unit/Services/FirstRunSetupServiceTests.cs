using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using MIC.Desktop.Avalonia.Services;

namespace MIC.Tests.Unit.Services;

public sealed class FirstRunSetupServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FirstRunSetupService _sut;

    public FirstRunSetupServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"MIC_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _sut = new FirstRunSetupService();

        // Override the private paths to point to our temp directory
        var setupField = typeof(FirstRunSetupService).GetField("_setupFilePath", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var configField = typeof(FirstRunSetupService).GetField("_configFilePath", BindingFlags.NonPublic | BindingFlags.Instance)!;

        setupField.SetValue(_sut, Path.Combine(_tempDir, "setup.json"));
        configField.SetValue(_sut, Path.Combine(_tempDir, "appsettings.runtime.json"));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch { /* best-effort cleanup */ }
    }

    private string SetupFilePath => Path.Combine(_tempDir, "setup.json");
    private string ConfigFilePath => Path.Combine(_tempDir, "appsettings.runtime.json");

    // ──────────────────────────────────────────────────────────────
    // IsFirstRunAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task IsFirstRunAsync_NoSetupFile_ReturnsTrue()
    {
        var result = await _sut.IsFirstRunAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFirstRunAsync_SetupFileExists_ReturnsFalse()
    {
        await File.WriteAllTextAsync(SetupFilePath, "{}");

        var result = await _sut.IsFirstRunAsync();

        result.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────
    // IsSetupCompleteAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task IsSetupCompleteAsync_NoSetupFile_ReturnsFalse()
    {
        var result = await _sut.IsSetupCompleteAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsSetupCompleteAsync_SetupNotCompleted_ReturnsFalse()
    {
        await File.WriteAllTextAsync(SetupFilePath,
            JsonSerializer.Serialize(new { SetupCompleted = false }));

        var result = await _sut.IsSetupCompleteAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsSetupCompleteAsync_SetupCompleted_ReturnsTrue()
    {
        await File.WriteAllTextAsync(SetupFilePath,
            JsonSerializer.Serialize(new { SetupCompleted = true, SetupDate = DateTime.UtcNow }));

        var result = await _sut.IsSetupCompleteAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSetupCompleteAsync_InvalidJson_ReturnsFalse()
    {
        await File.WriteAllTextAsync(SetupFilePath, "NOT VALID JSON {{{{");

        var result = await _sut.IsSetupCompleteAsync();

        result.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────
    // CompleteFirstRunSetupAsync — validation
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@no-local.com")]
    public async Task CompleteFirstRunSetupAsync_InvalidEmail_ThrowsArgumentException(string email)
    {
        var act = () => _sut.CompleteFirstRunSetupAsync(email, "StrongP@ssw0rd!1");

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*email*");
    }

    [Theory]
    [InlineData("short")]         // too short
    [InlineData("nouppercase1!")] // no uppercase
    [InlineData("NOLOWERCASE1!")] // no lowercase
    [InlineData("NoDigits!!!!!")] // no digit
    [InlineData("NoSpecial1234")] // no special char
    public async Task CompleteFirstRunSetupAsync_WeakPassword_ThrowsArgumentException(string password)
    {
        var act = () => _sut.CompleteFirstRunSetupAsync("valid@example.com", password);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Password*");
    }

    // ──────────────────────────────────────────────────────────────
    // CompleteFirstRunSetupAsync — happy path
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteFirstRunSetupAsync_ValidInputs_CreatesSetupAndConfigFiles()
    {
        await _sut.CompleteFirstRunSetupAsync("admin@example.com", "Str0ng!P@ssword");

        File.Exists(SetupFilePath).Should().BeTrue();
        File.Exists(ConfigFilePath).Should().BeTrue();
    }

    [Fact]
    public async Task CompleteFirstRunSetupAsync_ValidInputs_MarksSetupComplete()
    {
        await _sut.CompleteFirstRunSetupAsync("admin@example.com", "Str0ng!P@ssword");

        var result = await _sut.IsSetupCompleteAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteFirstRunSetupAsync_ValidInputs_GeneratesJwtSecretKey()
    {
        await _sut.CompleteFirstRunSetupAsync("admin@example.com", "Str0ng!P@ssword");

        var key = _sut.GetRuntimeJwtSecretKey();
        key.Should().NotBeNullOrWhiteSpace();
        // 64 bytes → base64 should be ~88 chars
        key.Length.Should().BeGreaterThan(40);
    }

    [Fact]
    public async Task CompleteFirstRunSetupAsync_CalledTwice_OverwritesFiles()
    {
        await _sut.CompleteFirstRunSetupAsync("first@example.com", "Str0ng!P@ssword");
        var key1 = _sut.GetRuntimeJwtSecretKey();

        await _sut.CompleteFirstRunSetupAsync("second@example.com", "Str0ng!P@ssword");
        var key2 = _sut.GetRuntimeJwtSecretKey();

        // New key generated each time
        key1.Should().NotBe(key2);
    }

    // ──────────────────────────────────────────────────────────────
    // GetRuntimeJwtSecretKey
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void GetRuntimeJwtSecretKey_NoConfigFile_ReturnsNull()
    {
        var key = _sut.GetRuntimeJwtSecretKey();

        key.Should().BeNull();
    }

    [Fact]
    public void GetRuntimeJwtSecretKey_InvalidConfigJson_ReturnsNull()
    {
        File.WriteAllText(ConfigFilePath, "NOT VALID JSON");

        var key = _sut.GetRuntimeJwtSecretKey();

        key.Should().BeNull();
    }

    [Fact]
    public void GetRuntimeJwtSecretKey_ValidConfig_ReturnsKey()
    {
        var config = new { JwtSettings = new { SecretKey = "test-secret-key-123" } };
        File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(config));

        var key = _sut.GetRuntimeJwtSecretKey();

        key.Should().Be("test-secret-key-123");
    }

    // ──────────────────────────────────────────────────────────────
    // Implements IFirstRunSetupService
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ImplementsIFirstRunSetupService()
    {
        _sut.Should().BeAssignableTo<MIC.Core.Application.Common.Interfaces.IFirstRunSetupService>();
    }

    // ──────────────────────────────────────────────────────────────
    // IsFirstRunAsync after setup complete
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task IsFirstRunAsync_AfterSetupComplete_ReturnsFalse()
    {
        await _sut.CompleteFirstRunSetupAsync("admin@example.com", "Str0ng!P@ssword");

        var result = await _sut.IsFirstRunAsync();

        result.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────
    // Email validation edge cases
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("user@domain.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("user+tag@domain.com")]
    public async Task CompleteFirstRunSetupAsync_ValidEmails_DoNotThrow(string email)
    {
        var act = () => _sut.CompleteFirstRunSetupAsync(email, "Str0ng!P@ssword");

        await act.Should().NotThrowAsync();
    }
}
