using FluentAssertions;
using MIC.Infrastructure.Identity.TokenStorage;

namespace MIC.Tests.Unit.Infrastructure.Identity;

/// <summary>
/// Tests for WindowsCredentialManager argument validation and error handling.
/// Tests only the input validation paths (null checks, size limits) since the
/// actual Win32 API calls require a real Windows credential store.
/// </summary>
public class WindowsCredentialManagerTests
{
    // WindowsCredentialManager constructor throws PlatformNotSupportedException on non-Windows
    // Since tests run on Windows, we can create instances

    private WindowsCredentialManager CreateManager() => new();

    #region Constructor

    [Fact]
    public void Constructor_OnWindows_DoesNotThrow()
    {
        if (!OperatingSystem.IsWindows()) return;

        var act = () => new WindowsCredentialManager();
        act.Should().NotThrow();
    }

    #endregion

    #region Write Argument Validation

    [Fact]
    public void Write_NullTargetName_ThrowsArgumentNullException()
    {
        if (!OperatingSystem.IsWindows()) return;

        var mgr = CreateManager();
        var act = () => mgr.Write(null!, new byte[] { 1, 2, 3 });
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Write_NullSecret_ThrowsArgumentNullException()
    {
        if (!OperatingSystem.IsWindows()) return;

        var mgr = CreateManager();
        var act = () => mgr.Write("test-target", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Write_OversizedSecret_ThrowsArgumentOutOfRangeException()
    {
        if (!OperatingSystem.IsWindows()) return;

        var mgr = CreateManager();
        var oversized = new byte[5121]; // > 5120 limit
        var act = () => mgr.Write("test-target", oversized);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Read Argument Validation

    [Fact]
    public void Read_NullTargetName_ThrowsArgumentNullException()
    {
        if (!OperatingSystem.IsWindows()) return;

        var mgr = CreateManager();
        var act = () => mgr.Read(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Read_NonExistentTarget_ReturnsNull()
    {
        if (!OperatingSystem.IsWindows()) return;

        var mgr = CreateManager();
        var result = mgr.Read("MIC_TEST_NONEXISTENT_" + Guid.NewGuid().ToString("N"));
        result.Should().BeNull();
    }

    #endregion

    #region Delete Argument Validation

    [Fact]
    public void Delete_NullTargetName_ThrowsArgumentNullException()
    {
        if (!OperatingSystem.IsWindows()) return;

        var mgr = CreateManager();
        var act = () => mgr.Delete(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Delete_NonExistentTarget_DoesNotThrow()
    {
        if (!OperatingSystem.IsWindows()) return;

        var mgr = CreateManager();
        var act = () => mgr.Delete("MIC_TEST_NONEXISTENT_" + Guid.NewGuid().ToString("N"));
        act.Should().NotThrow();
    }

    #endregion

    #region Enumerate Argument Validation

    [Fact]
    public void Enumerate_NullPrefix_ThrowsArgumentNullException()
    {
        if (!OperatingSystem.IsWindows()) return;

        var mgr = CreateManager();
        var act = () => mgr.Enumerate(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Enumerate_NonExistentPrefix_ReturnsEmptyList()
    {
        if (!OperatingSystem.IsWindows()) return;

        var mgr = CreateManager();
        var result = mgr.Enumerate("MIC_TEST_NONEXISTENT_" + Guid.NewGuid().ToString("N"));
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region Write-Read-Delete Round Trip

    [Fact]
    public void WriteReadDelete_RoundTrip_Works()
    {
        if (!OperatingSystem.IsWindows()) return;

        var mgr = CreateManager();
        var targetName = "MIC_UNIT_TEST_" + Guid.NewGuid().ToString("N");
        var secret = System.Text.Encoding.UTF8.GetBytes("test-secret-value");

        try
        {
            mgr.Write(targetName, secret);

            var read = mgr.Read(targetName);
            read.Should().NotBeNull();
            read.Should().Equal(secret);
        }
        finally
        {
            mgr.Delete(targetName);
        }

        // After delete, should be null
        var afterDelete = mgr.Read(targetName);
        afterDelete.Should().BeNull();
    }

    [Fact]
    public void Write_EmptySecret_RoundTrips()
    {
        if (!OperatingSystem.IsWindows()) return;

        var mgr = CreateManager();
        var targetName = "MIC_UNIT_TEST_EMPTY_" + Guid.NewGuid().ToString("N");

        try
        {
            mgr.Write(targetName, Array.Empty<byte>());
            var read = mgr.Read(targetName);
            read.Should().NotBeNull();
            read.Should().BeEmpty();
        }
        finally
        {
            mgr.Delete(targetName);
        }
    }

    [Fact]
    public void Enumerate_AfterWrite_FindsCredential()
    {
        if (!OperatingSystem.IsWindows()) return;

        var mgr = CreateManager();
        var prefix = "MIC_UNIT_TEST_ENUM_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var target = prefix + "_key1";

        try
        {
            mgr.Write(target, new byte[] { 1, 2, 3 });
            var results = mgr.Enumerate(prefix);
            results.Should().HaveCount(1);
            results[0].TargetName.Should().Be(target);
        }
        finally
        {
            mgr.Delete(target);
        }
    }

    #endregion
}
