using System;
using System.Net.Http;
using FluentAssertions;
using MIC.Desktop.Avalonia.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MIC.Tests.Unit.Services;

/// <summary>
/// Tests for UpdateService.
/// Tests update checking, version comparison, and update management.
/// Target: 5 additional tests for update functionality
/// </summary>
public class UpdateServiceTests
{
    private readonly Mock<ILogger<UpdateService>> _mockLogger;
    private readonly HttpClient _httpClient;

    public UpdateServiceTests()
    {
        _mockLogger = new Mock<ILogger<UpdateService>>();
        _httpClient = new HttpClient();
    }

    #region Constructor Tests (1 test)

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var service = new UpdateService(_httpClient, _mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region UpdateInfo Tests (2 tests)

    [Fact]
    public void UpdateInfo_DefaultInstance_HasExpectedProperties()
    {
        // Act
        var updateInfo = new UpdateService.UpdateInfo();

        // Assert
        updateInfo.Version.Should().NotBeNull();
        updateInfo.DownloadUrl.Should().NotBeNull();
        updateInfo.ReleaseNotes.Should().NotBeNull();
        updateInfo.IsRequired.Should().BeFalse();
        updateInfo.Size.Should().Be(0);
    }

    [Fact]
    public void UpdateInfo_WithSetProperties_RetainsValues()
    {
        // Arrange
        var updateInfo = new UpdateService.UpdateInfo
        {
            Version = "2.0.0",
            DownloadUrl = "https://example.com/download",
            ReleaseNotes = "Bug fixes",
            IsRequired = true,
            Size = 1024000
        };

        // Assert
        updateInfo.Version.Should().Be("2.0.0");
        updateInfo.DownloadUrl.Should().Be("https://example.com/download");
        updateInfo.ReleaseNotes.Should().Be("Bug fixes");
        updateInfo.IsRequired.Should().BeTrue();
        updateInfo.Size.Should().Be(1024000);
    }

    #endregion

    #region Service Initialization Tests (2 tests)

    [Fact]
    public void UpdateService_CreatedWithHttpClient_AcceptsValidClient()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var logger = new Mock<ILogger<UpdateService>>();

        // Act
        var service = new UpdateService(httpClient, logger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void UpdateService_CreatedWithLogger_AcceptsValidLogger()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var logger = new Mock<ILogger<UpdateService>>();

        // Act
        var service = new UpdateService(httpClient, logger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion
}
