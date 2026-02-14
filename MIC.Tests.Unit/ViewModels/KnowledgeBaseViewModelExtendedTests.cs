using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Authentication.Common;
using MIC.Core.Domain.Entities;
using MIC.Desktop.Avalonia.ViewModels;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace MIC.Tests.Unit.ViewModels;

/// <summary>
/// Extended tests for KnowledgeBaseViewModel covering Initialize, GetContentType,
/// LoadDocuments edge cases, PerformSearch edge cases, UpdateEntries, and Dispose.
/// </summary>
public class KnowledgeBaseViewModelExtendedTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ISessionService _sessionService = Substitute.For<ISessionService>();
    private readonly IKnowledgeBaseService _kbService = Substitute.For<IKnowledgeBaseService>();
    private readonly ILogger<KnowledgeBaseViewModel> _logger = Substitute.For<ILogger<KnowledgeBaseViewModel>>();
    private readonly IErrorHandlingService _errorService = Substitute.For<IErrorHandlingService>();

    private KnowledgeBaseViewModel CreateVm() =>
        new(_mediator, _sessionService, _kbService, _logger, _errorService);

    #region GetContentType (via reflection since it's private)

    private string InvokeGetContentType(string fileName)
    {
        var vm = CreateVm();
        var method = typeof(KnowledgeBaseViewModel)
            .GetMethod("GetContentType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (string)method!.Invoke(vm, new object[] { fileName })!;
    }

    [Theory]
    [InlineData("doc.pdf", "application/pdf")]
    [InlineData("doc.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("doc.doc", "application/msword")]
    [InlineData("doc.txt", "text/plain")]
    [InlineData("doc.md", "text/markdown")]
    [InlineData("doc.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("doc.csv", "text/csv")]
    [InlineData("doc.pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation")]
    [InlineData("doc.unknown", "application/octet-stream")]
    [InlineData("file.PDF", "application/pdf")]
    [InlineData("noextension", "application/octet-stream")]
    public void GetContentType_ReturnsCorrectMimeType(string fileName, string expected)
    {
        InvokeGetContentType(fileName).Should().Be(expected);
    }

    #endregion

    #region Initialize

    [Fact]
    public void Initialize_CalledOnce_DoesNotThrow()
    {
        var vm = CreateVm();
        var act = () => vm.Initialize();
        act.Should().NotThrow();
    }

    [Fact]
    public void Initialize_CalledTwice_IsIdempotent()
    {
        var vm = CreateVm();
        vm.Initialize();
        var act = () => vm.Initialize();
        act.Should().NotThrow();
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var vm = CreateVm();
        vm.Initialize();
        var act = () => vm.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var vm = CreateVm();
        vm.Dispose();
        var act = () => vm.Dispose();
        act.Should().NotThrow();
    }

    #endregion

    #region LoadDocumentsAsync extended

    [Fact]
    public async Task LoadDocumentsAsync_NullUser_ClearsEntries()
    {
        _sessionService.GetUser().Returns((UserDto?)null);
        var vm = CreateVm();

        await vm.LoadDocumentsAsync();

        vm.Entries.Should().BeEmpty();
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadDocumentsAsync_EmptyUserId_ClearsEntries()
    {
        var dto = new UserDto { Id = Guid.Empty };
        _sessionService.GetUser().Returns(dto);
        var vm = CreateVm();

        await vm.LoadDocumentsAsync();

        vm.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadDocumentsAsync_ValidUser_PopulatesEntries()
    {
        var userId = Guid.NewGuid();
        var dto = new UserDto { Id = userId, Username = "u", Email = "u@t.com" };
        _sessionService.GetUser().Returns(dto);
        _kbService.SearchAsync("", userId, Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeEntry>
            {
                new() { Title = "Doc1", Content = "Content1", Tags = new List<string> { "A" } },
                new() { Title = "Doc2", Content = "Content2", Tags = new List<string> { "B", "C" } }
            });
        var vm = CreateVm();

        await vm.LoadDocumentsAsync();

        vm.Entries.Should().HaveCount(2);
        vm.Entries[0].Title.Should().Be("Doc1");
        vm.Entries[1].Tags.Should().Be("B, C");
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadDocumentsAsync_Exception_SetsErrorAndClearsEntries()
    {
        var userId = Guid.NewGuid();
        var dto = new UserDto { Id = userId, Username = "u", Email = "u@t.com" };
        _sessionService.GetUser().Returns(dto);
        _kbService.SearchAsync("", userId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("service down"));
        var vm = CreateVm();

        await vm.LoadDocumentsAsync();

        vm.ErrorMessage.Should().Contain("Failed to load documents");
        vm.Entries.Should().BeEmpty();
        vm.IsLoading.Should().BeFalse();
        _errorService.Received(1).HandleException(Arg.Any<Exception>(), Arg.Any<string>());
    }

    [Fact]
    public async Task LoadDocumentsAsync_ClearsErrorBeforeLoad()
    {
        var userId = Guid.NewGuid();
        var dto = new UserDto { Id = userId, Username = "u", Email = "u@t.com" };
        _sessionService.GetUser().Returns(dto);
        _kbService.SearchAsync("", userId, Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeEntry>());
        var vm = CreateVm();
        vm.ErrorMessage = "old error";

        await vm.LoadDocumentsAsync();

        vm.ErrorMessage.Should().BeEmpty();
    }

    #endregion

    #region PerformSearchAsync extended

    [Fact]
    public async Task PerformSearchAsync_NullUser_ClearsEntries()
    {
        _sessionService.GetUser().Returns((UserDto?)null);
        var vm = CreateVm();

        await vm.PerformSearchAsync("test");

        vm.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task PerformSearchAsync_ValidQuery_SearchesAndPopulates()
    {
        var userId = Guid.NewGuid();
        var dto = new UserDto { Id = userId, Username = "u", Email = "u@t.com" };
        _sessionService.GetUser().Returns(dto);
        _kbService.SearchAsync("hello", userId, Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeEntry>
            {
                new() { Title = "Match", Content = "Found", Tags = new List<string>() }
            });
        var vm = CreateVm();

        await vm.PerformSearchAsync("hello");

        vm.Entries.Should().HaveCount(1);
        vm.Entries[0].Title.Should().Be("Match");
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task PerformSearchAsync_Exception_SetsError()
    {
        var userId = Guid.NewGuid();
        var dto = new UserDto { Id = userId, Username = "u", Email = "u@t.com" };
        _sessionService.GetUser().Returns(dto);
        _kbService.SearchAsync("q", userId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("timeout"));
        var vm = CreateVm();

        await vm.PerformSearchAsync("q");

        vm.ErrorMessage.Should().Contain("Failed to search documents");
        vm.IsLoading.Should().BeFalse();
    }

    #endregion

    #region Properties

    [Fact]
    public void SearchText_SetAndGet()
    {
        var vm = CreateVm();
        vm.SearchText = "test";
        vm.SearchText.Should().Be("test");
    }

    [Fact]
    public void IsLoading_SetAndGet()
    {
        var vm = CreateVm();
        vm.IsLoading = true;
        vm.IsLoading.Should().BeTrue();
    }

    [Fact]
    public void SuccessMessage_SetAndGet()
    {
        var vm = CreateVm();
        vm.SuccessMessage = "ok";
        vm.SuccessMessage.Should().Be("ok");
    }

    [Fact]
    public void ErrorMessage_SetAndGet()
    {
        var vm = CreateVm();
        vm.ErrorMessage = "err";
        vm.ErrorMessage.Should().Be("err");
    }

    [Fact]
    public void Commands_NotNull()
    {
        var vm = CreateVm();
        vm.UploadDocumentsCommand.Should().NotBeNull();
        vm.LoadEntriesCommand.Should().NotBeNull();
        vm.SearchCommand.Should().NotBeNull();
    }

    #endregion

    #region KnowledgeEntry entity

    [Fact]
    public void KnowledgeEntry_UpdateAccessTime_SetsLastAccessed()
    {
        var entry = new KnowledgeEntry();
        entry.LastAccessed.Should().BeNull();

        entry.UpdateAccessTime();

        entry.LastAccessed.Should().NotBeNull();
        entry.LastAccessed.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void KnowledgeEntry_UpdateRelevanceScore_SetsScore()
    {
        var entry = new KnowledgeEntry();
        entry.RelevanceScore.Should().Be(0.0);

        entry.UpdateRelevanceScore(0.95);

        entry.RelevanceScore.Should().Be(0.95);
    }

    [Fact]
    public void KnowledgeEntry_DefaultProperties()
    {
        var entry = new KnowledgeEntry();
        entry.Title.Should().BeEmpty();
        entry.Content.Should().BeEmpty();
        entry.FullContent.Should().BeEmpty();
        entry.SourceType.Should().BeEmpty();
        entry.Tags.Should().BeEmpty();
        entry.AISummary.Should().BeNull();
        entry.FilePath.Should().BeNull();
        entry.FileSize.Should().BeNull();
        entry.ContentType.Should().BeNull();
    }

    [Fact]
    public void KnowledgeEntry_SetAllProperties()
    {
        var entry = new KnowledgeEntry
        {
            UserId = Guid.NewGuid(),
            Title = "T",
            Content = "C",
            FullContent = "FC",
            SourceType = "Email",
            SourceId = Guid.NewGuid(),
            Tags = new List<string> { "tag1" },
            AISummary = "Summary",
            FilePath = "/path",
            FileSize = 1024,
            ContentType = "text/plain"
        };

        entry.Title.Should().Be("T");
        entry.FullContent.Should().Be("FC");
        entry.SourceType.Should().Be("Email");
        entry.FileSize.Should().Be(1024);
    }

    #endregion

    #region KnowledgeEntryViewModel

    [Fact]
    public void KnowledgeEntryViewModel_Properties()
    {
        var vm = new KnowledgeEntryViewModel
        {
            Title = "T",
            Content = "C",
            Tags = "A, B",
            CreatedAt = new DateTime(2025, 1, 1)
        };

        vm.Title.Should().Be("T");
        vm.Content.Should().Be("C");
        vm.Tags.Should().Be("A, B");
        vm.CreatedAt.Should().Be(new DateTime(2025, 1, 1));
    }

    #endregion
}
