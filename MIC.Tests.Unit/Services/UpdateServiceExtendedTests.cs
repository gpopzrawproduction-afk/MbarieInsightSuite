using System.Net;
using System.Net.Http;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MIC.Desktop.Avalonia.Services;
using Moq;
using Moq.Protected;

namespace MIC.Tests.Unit.Services;

/// <summary>
/// Extended UpdateService tests covering version comparison, update checking, and DTOs.
/// </summary>
public class UpdateServiceExtendedTests
{
    private readonly ILogger<UpdateService> _logger = NullLogger<UpdateService>.Instance;

    private UpdateService CreateService(HttpClient? client = null)
    {
        return new UpdateService(client ?? new HttpClient(), _logger);
    }

    private static HttpClient CreateMockHttpClient(HttpStatusCode statusCode, string content)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });

        return new HttpClient(handler.Object);
    }

    #region UpdateInfo DTO Tests

    [Fact]
    public void UpdateInfo_Defaults_AreCorrect()
    {
        var info = new UpdateService.UpdateInfo();
        info.Version.Should().BeEmpty();
        info.DownloadUrl.Should().BeEmpty();
        info.ReleaseNotes.Should().BeEmpty();
        info.IsRequired.Should().BeFalse();
        info.Size.Should().Be(0);
    }

    [Fact]
    public void UpdateInfo_AllPropertiesSettable()
    {
        var info = new UpdateService.UpdateInfo
        {
            Version = "2.0.0",
            DownloadUrl = "https://example.com/update.msix",
            ReleaseNotes = "Bug fixes and improvements",
            IsRequired = true,
            Size = 1024 * 1024
        };

        info.Version.Should().Be("2.0.0");
        info.DownloadUrl.Should().Be("https://example.com/update.msix");
        info.ReleaseNotes.Should().Be("Bug fixes and improvements");
        info.IsRequired.Should().BeTrue();
        info.Size.Should().Be(1024 * 1024);
    }

    #endregion

    #region IsNewerVersion Tests (via reflection)

    private bool InvokeIsNewerVersion(string latest, string current)
    {
        var svc = CreateService();
        var method = typeof(UpdateService).GetMethod("IsNewerVersion",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        return (bool)method.Invoke(svc, new object[] { latest, current })!;
    }

    [Fact]
    public void IsNewerVersion_NewerMajor_ReturnsTrue()
    {
        InvokeIsNewerVersion("2.0.0", "1.0.0").Should().BeTrue();
    }

    [Fact]
    public void IsNewerVersion_NewerMinor_ReturnsTrue()
    {
        InvokeIsNewerVersion("1.1.0", "1.0.0").Should().BeTrue();
    }

    [Fact]
    public void IsNewerVersion_NewerPatch_ReturnsTrue()
    {
        InvokeIsNewerVersion("1.0.1", "1.0.0").Should().BeTrue();
    }

    [Fact]
    public void IsNewerVersion_SameVersion_ReturnsFalse()
    {
        InvokeIsNewerVersion("1.0.0", "1.0.0").Should().BeFalse();
    }

    [Fact]
    public void IsNewerVersion_OlderVersion_ReturnsFalse()
    {
        InvokeIsNewerVersion("1.0.0", "2.0.0").Should().BeFalse();
    }

    [Fact]
    public void IsNewerVersion_InvalidLatest_ReturnsFalse()
    {
        InvokeIsNewerVersion("invalid", "1.0.0").Should().BeFalse();
    }

    [Fact]
    public void IsNewerVersion_InvalidCurrent_ReturnsFalse()
    {
        InvokeIsNewerVersion("1.0.0", "not-a-version").Should().BeFalse();
    }

    [Fact]
    public void IsNewerVersion_BothInvalid_ReturnsFalse()
    {
        InvokeIsNewerVersion("abc", "xyz").Should().BeFalse();
    }

    #endregion

    #region IsMajorUpdate Tests (via reflection)

    private bool InvokeIsMajorUpdate(string latest, string current)
    {
        var svc = CreateService();
        var method = typeof(UpdateService).GetMethod("IsMajorUpdate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        return (bool)method.Invoke(svc, new object[] { latest, current })!;
    }

    [Fact]
    public void IsMajorUpdate_MajorBump_ReturnsTrue()
    {
        InvokeIsMajorUpdate("2.0.0", "1.5.3").Should().BeTrue();
    }

    [Fact]
    public void IsMajorUpdate_SameMajor_ReturnsFalse()
    {
        InvokeIsMajorUpdate("1.5.0", "1.0.0").Should().BeFalse();
    }

    [Fact]
    public void IsMajorUpdate_InvalidVersions_ReturnsFalse()
    {
        InvokeIsMajorUpdate("abc", "xyz").Should().BeFalse();
    }

    [Fact]
    public void IsMajorUpdate_MultipleMajorBumps_ReturnsTrue()
    {
        InvokeIsMajorUpdate("5.0.0", "1.0.0").Should().BeTrue();
    }

    #endregion

    #region CheckForUpdatesAsync Tests

    [Fact]
    public async Task CheckForUpdatesAsync_WhenHttpErrors_ReturnsNull()
    {
        var client = CreateMockHttpClient(HttpStatusCode.InternalServerError, "{}");
        var service = CreateService(client);

        var result = await service.CheckForUpdatesAsync("1.0.0");

        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenResponseIsNull_ReturnsNull()
    {
        var client = CreateMockHttpClient(HttpStatusCode.OK, "null");
        var service = CreateService(client);

        var result = await service.CheckForUpdatesAsync("1.0.0");

        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenSameVersion_ReturnsNull()
    {
        var json = """{"tag_name": "v1.0.0", "body": "Release notes", "assets": []}""";
        var client = CreateMockHttpClient(HttpStatusCode.OK, json);
        var service = CreateService(client);

        var result = await service.CheckForUpdatesAsync("1.0.0");

        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenNewerVersion_ReturnsUpdateInfo()
    {
        var json = """
        {
            "tag_name": "v2.0.0",
            "body": "Major update",
            "assets": [
                {"name": "app.msix", "browser_download_url": "https://example.com/app.msix", "size": 5000}
            ]
        }
        """;
        var client = CreateMockHttpClient(HttpStatusCode.OK, json);
        var service = CreateService(client);

        var result = await service.CheckForUpdatesAsync("1.0.0");

        result.Should().NotBeNull();
        result!.Version.Should().Be("2.0.0");
        result.DownloadUrl.Should().Contain("app.msix");
        result.ReleaseNotes.Should().Be("Major update");
        result.IsRequired.Should().BeTrue(); // Major version bump
        result.Size.Should().Be(5000);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenMinorUpdate_IsNotRequired()
    {
        var json = """
        {
            "tag_name": "v1.1.0",
            "body": "Minor update",
            "assets": [
                {"name": "app.msix", "browser_download_url": "https://example.com/app.msix", "size": 3000}
            ]
        }
        """;
        var client = CreateMockHttpClient(HttpStatusCode.OK, json);
        var service = CreateService(client);

        var result = await service.CheckForUpdatesAsync("1.0.0");

        result.Should().NotBeNull();
        result!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_NoMsixAsset_DownloadUrlIsEmpty()
    {
        var json = """
        {
            "tag_name": "v2.0.0",
            "body": "No installer",
            "assets": [
                {"name": "source.zip", "browser_download_url": "https://example.com/source.zip", "size": 1000}
            ]
        }
        """;
        var client = CreateMockHttpClient(HttpStatusCode.OK, json);
        var service = CreateService(client);

        var result = await service.CheckForUpdatesAsync("1.0.0");

        result.Should().NotBeNull();
        result!.DownloadUrl.Should().BeEmpty();
        result.Size.Should().Be(0);
    }

    #endregion
}
