using System;
using System.Linq;
using System.Text;
using FluentAssertions;
using MIC.Infrastructure.Identity.TokenStorage;

namespace MIC.Tests.Unit.Infrastructure.Identity;

public sealed class WindowsDpapiDataProtectorTests
{
    // These tests only run on Windows where DPAPI is available.
    // The SUT constructor throws PlatformNotSupportedException on non-Windows.

    [Fact]
    public void Constructor_OnWindows_DoesNotThrow()
    {
        if (!OperatingSystem.IsWindows())
        {
            // Skip on non-Windows
            return;
        }

        var act = () => new WindowsDpapiDataProtector();
        act.Should().NotThrow();
    }

    [Fact]
    public void Protect_Unprotect_RoundTrip_RestoresOriginalData()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var sut = new WindowsDpapiDataProtector();
        var original = Encoding.UTF8.GetBytes("Hello, DPAPI roundtrip test!");

        var protectedBytes = sut.Protect(original);

        protectedBytes.Should().NotBeNull();
        protectedBytes.Should().NotBeEquivalentTo(original, "protected bytes should be encrypted");

        var unprotected = sut.Unprotect(protectedBytes);

        unprotected.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void Protect_ProducesDifferentBytes_ThanInput()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var sut = new WindowsDpapiDataProtector();
        var original = Encoding.UTF8.GetBytes("some secret data");

        var protectedBytes = sut.Protect(original);

        // DPAPI adds overhead + entropy, so protected should be longer
        protectedBytes.Length.Should().BeGreaterThan(original.Length);
    }

    [Fact]
    public void Protect_EmptyData_Succeeds()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var sut = new WindowsDpapiDataProtector();
        var empty = Array.Empty<byte>();

        // ProtectedData.Protect with empty input should still work
        var act = () => sut.Protect(empty);
        act.Should().NotThrow();
    }

    [Fact]
    public void Unprotect_WithTamperedData_Throws()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var sut = new WindowsDpapiDataProtector();
        var original = Encoding.UTF8.GetBytes("tamper test");
        var protectedBytes = sut.Protect(original);

        // Tamper with the protected data
        protectedBytes[0] ^= 0xFF;

        // DPAPI should reject tampered data
        var act = () => sut.Unprotect(protectedBytes);
        act.Should().Throw<Exception>(); // CryptographicException typically
    }
}
