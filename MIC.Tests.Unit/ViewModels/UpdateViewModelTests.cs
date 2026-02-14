using System;
using System.Net;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MIC.Desktop.Avalonia.Services;
using MIC.Desktop.Avalonia.ViewModels;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ReactiveUI;
using Xunit;

namespace MIC.Tests.Unit.ViewModels;

/// <summary>
/// Tests for UpdateViewModel covering update checking, download, and skip operations.
/// </summary>
public class UpdateViewModelTests
{
    private readonly ILogger<UpdateViewModel> _vmLogger;
    private readonly ILogger<UpdateService> _svcLogger;

    static UpdateViewModelTests()
    {
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = CurrentThreadScheduler.Instance;
    }

    public UpdateViewModelTests()
    {
        _vmLogger = Substitute.For<ILogger<UpdateViewModel>>();
        _svcLogger = Substitute.For<ILogger<UpdateService>>();
    }

    private UpdateService CreateUpdateService(HttpMessageHandler? handler = null)
    {
        handler ??= new FakeHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = new HttpClient(handler);
        return new UpdateService(client, _svcLogger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesDefaultProperties()
    {
        var svc = CreateUpdateService();
        var vm = new UpdateViewModel(svc, _vmLogger);

        vm.CurrentVersion.Should().NotBeNullOrEmpty();
        vm.LatestVersion.Should().BeEmpty();
        vm.ReleaseNotes.Should().BeEmpty();
        vm.UpdateAvailable.Should().BeFalse();
        vm.IsUpdateRequired.Should().BeFalse();
        vm.IsCheckingForUpdates.Should().BeFalse();
        vm.IsDownloading.Should().BeFalse();
        vm.DownloadProgress.Should().Be(0);
        vm.StatusMessage.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        var svc = CreateUpdateService();
        var vm = new UpdateViewModel(svc, _vmLogger);

        vm.CheckForUpdatesCommand.Should().NotBeNull();
        vm.DownloadUpdateCommand.Should().NotBeNull();
        vm.SkipUpdateCommand.Should().NotBeNull();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var svc = CreateUpdateService();
        var vm = new UpdateViewModel(svc, _vmLogger);

        vm.LatestVersion = "2.0.0";
        vm.ReleaseNotes = "New features";
        vm.UpdateAvailable = true;
        vm.IsUpdateRequired = true;
        vm.IsCheckingForUpdates = true;
        vm.IsDownloading = true;
        vm.DownloadProgress = 50.5;
        vm.StatusMessage = "In progress";

        vm.LatestVersion.Should().Be("2.0.0");
        vm.ReleaseNotes.Should().Be("New features");
        vm.UpdateAvailable.Should().BeTrue();
        vm.IsUpdateRequired.Should().BeTrue();
        vm.IsCheckingForUpdates.Should().BeTrue();
        vm.IsDownloading.Should().BeTrue();
        vm.DownloadProgress.Should().Be(50.5);
        vm.StatusMessage.Should().Be("In progress");
    }

    #endregion

    #region CheckForUpdatesCommand Tests

    [Fact]
    public async Task CheckForUpdatesCommand_SetsStatusMessage_WhenNoUpdate()
    {
        // Returns 404, so update check returns null → no update
        var svc = CreateUpdateService();
        var vm = new UpdateViewModel(svc, _vmLogger);

        await vm.CheckForUpdatesCommand.Execute();

        vm.UpdateAvailable.Should().BeFalse();
        vm.StatusMessage.Should().NotBeNullOrEmpty();
        vm.IsCheckingForUpdates.Should().BeFalse();
    }

    [Fact]
    public async Task CheckForUpdatesCommand_HandlesNoUpdate_SetsAppropriateMessage()
    {
        // When CheckForUpdatesAsync returns null (no update/error), VM reports "latest version"
        var handler = new FakeHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var svc = CreateUpdateService(handler);
        var vm = new UpdateViewModel(svc, _vmLogger);

        await vm.CheckForUpdatesCommand.Execute();

        vm.StatusMessage.Should().Contain("latest version");
        vm.UpdateAvailable.Should().BeFalse();
        vm.IsCheckingForUpdates.Should().BeFalse();
    }

    #endregion

    #region SkipUpdateCommand Tests

    [Fact]
    public void SkipUpdateCommand_SetsUpdateAvailableToFalse()
    {
        var svc = CreateUpdateService();
        var vm = new UpdateViewModel(svc, _vmLogger);
        vm.UpdateAvailable = true;

        vm.SkipUpdateCommand.Execute().Subscribe();

        vm.UpdateAvailable.Should().BeFalse();
    }

    #endregion

    /// <summary>
    /// Simple HTTP handler for testing that can return canned responses or throw.
    /// </summary>
    private class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _factory;

        public FakeHandler(HttpResponseMessage response)
        {
            _factory = _ => response;
        }

        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> factory)
        {
            _factory = factory;
        }

        public FakeHandler(Action<HttpRequestMessage> thrower)
        {
            _factory = req => { thrower(req); return null!; };
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_factory(request));
        }
    }
}
