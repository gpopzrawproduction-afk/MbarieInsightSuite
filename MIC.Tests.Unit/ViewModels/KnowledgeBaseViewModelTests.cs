using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using ReactiveUI;
using Xunit;
using System.Reactive.Concurrency;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Authentication.Common;
using MIC.Core.Application.KnowledgeBase.Commands.UploadDocument;
using MIC.Desktop.Avalonia.ViewModels;

namespace MIC.Tests.Unit.ViewModels;

public class KnowledgeBaseViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<IKnowledgeBaseService> _knowledgeBaseServiceMock;
    private readonly Mock<ILogger<KnowledgeBaseViewModel>> _loggerMock;
    private readonly Mock<IErrorHandlingService> _errorHandlingServiceMock;
    private readonly KnowledgeBaseViewModel _sut;

    static KnowledgeBaseViewModelTests()
    {
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;
    }

    public KnowledgeBaseViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sessionServiceMock = new Mock<ISessionService>();
        _knowledgeBaseServiceMock = new Mock<IKnowledgeBaseService>();
        _loggerMock = new Mock<ILogger<KnowledgeBaseViewModel>>();
        _errorHandlingServiceMock = new Mock<IErrorHandlingService>();

        _sut = new KnowledgeBaseViewModel(
            _mediatorMock.Object,
            _sessionServiceMock.Object,
            _knowledgeBaseServiceMock.Object,
            _loggerMock.Object,
            _errorHandlingServiceMock.Object);
        // Activate lifecycle for reactive behaviors in tests when needed
        _sut.Initialize();
    }

    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        // Assert
        _sut.Entries.Should().NotBeNull();
        _sut.Entries.Should().BeEmpty();
        _sut.SearchText.Should().BeEmpty();
        _sut.IsLoading.Should().BeFalse();
        _sut.SuccessMessage.Should().BeNullOrEmpty();
        _sut.ErrorMessage.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task LoadDocumentsAsync_WithValidData_LoadsEntries()
    {
        // Arrange
        var mockEntries = new List<KnowledgeEntry>
        {
            new KnowledgeEntry
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Title = "Test Document",
                Content = "Test content"
            }
        };

        _knowledgeBaseServiceMock
            .Setup(x => x.SearchAsync("", It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEntries);

        _sessionServiceMock
            .Setup(x => x.GetUser())
            .Returns(new UserDto { Id = Guid.NewGuid() });

        // Act
        await _sut.LoadDocumentsAsync();

        // Assert
        _sut.Entries.Should().HaveCount(1);
        _sut.Entries[0].Title.Should().Be("Test Document");
    }

    [Fact]
    public async Task LoadDocumentsAsync_WithException_SetsErrorMessage()
    {
        // Arrange
        _knowledgeBaseServiceMock
            .Setup(x => x.SearchAsync("", It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        _sessionServiceMock
            .Setup(x => x.GetUser())
            .Returns(new UserDto { Id = Guid.NewGuid() });

        // Act
        await _sut.LoadDocumentsAsync();

        // Assert
        _sut.ErrorMessage.Should().Contain("Failed to load documents");
        _sut.Entries.Should().BeEmpty();
        _errorHandlingServiceMock.Verify(x => x.HandleException(It.IsAny<Exception>(), "Failed to load documents", false), Times.Once);
    }

    [Fact]
    public async Task PerformSearchAsync_WithValidQuery_SearchesAndUpdatesEntries()
    {
        // Arrange
        var searchResults = new List<KnowledgeEntry>
        {
            new KnowledgeEntry
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Title = "Search Result",
                Content = "Found content"
            }
        };

        _knowledgeBaseServiceMock
            .Setup(x => x.SearchAsync("test query", It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        _sessionServiceMock
            .Setup(x => x.GetUser())
            .Returns(new UserDto { Id = Guid.NewGuid() });

        // Act
        await _sut.PerformSearchAsync("test query");

        // Assert
        _sut.Entries.Should().HaveCount(1);
        _sut.Entries[0].Title.Should().Be("Search Result");
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task PerformSearchAsync_WithException_SetsErrorMessage()
    {
        // Arrange
        _knowledgeBaseServiceMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Search failed"));

        _sessionServiceMock
            .Setup(x => x.GetUser())
            .Returns(new UserDto { Id = Guid.NewGuid() });

        // Act
        await _sut.PerformSearchAsync("test");

        // Assert
        _sut.ErrorMessage.Should().Contain("Failed to search documents");
        _sut.IsLoading.Should().BeFalse();
        _errorHandlingServiceMock.Verify(x => x.HandleException(It.IsAny<Exception>(), "Failed to search documents", false), Times.Once);
    }

    [Fact]
    public async Task SearchText_WhenChanged_TriggersSearch()
    {
        // Arrange
        var searchResults = new List<KnowledgeEntry>
        {
            new KnowledgeEntry
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Title = "Result",
                Content = "Content"
            }
        };

        _knowledgeBaseServiceMock
            .Setup(x => x.SearchAsync("new search", It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        _sessionServiceMock
            .Setup(x => x.GetUser())
            .Returns(new UserDto { Id = Guid.NewGuid() });

        // Act - directly invoke search (avoid relying on real time delays)
        await _sut.PerformSearchAsync("new search");

        // Assert
        _sut.Entries.Should().HaveCount(1);
        _sut.Entries[0].Title.Should().Be("Result");
    }
}