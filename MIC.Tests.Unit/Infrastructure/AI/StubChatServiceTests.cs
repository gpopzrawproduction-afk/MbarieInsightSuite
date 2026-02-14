using FluentAssertions;
using MIC.Infrastructure.AI;
using MIC.Infrastructure.AI.Services;

namespace MIC.Tests.Unit.Infrastructure.AI;

/// <summary>
/// Tests for the StubChatService fallback when AI is not configured.
/// Uses reflection to instantiate the internal class.
/// </summary>
public class StubChatServiceTests
{
    private static IChatService CreateStubService()
    {
        // StubChatService is internal; instantiate via reflection
        var type = typeof(DependencyInjection).Assembly
            .GetType("MIC.Infrastructure.AI.StubChatService")
            ?? throw new InvalidOperationException("StubChatService type not found.");
        return (IChatService)Activator.CreateInstance(type)!;
    }

    [Fact]
    public async Task SendMessageAsync_ReturnsUnsuccessfulResult()
    {
        var service = CreateStubService();
        var result = await service.SendMessageAsync("Hello");

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SendMessageAsync_ErrorContainsConfigurationMessage()
    {
        var service = CreateStubService();
        var result = await service.SendMessageAsync("Hello");

        result.Error.Should().Contain("not configured");
    }

    [Fact]
    public async Task SendMessageAsync_DurationIsZero()
    {
        var service = CreateStubService();
        var result = await service.SendMessageAsync("Hello");

        result.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task SendMessageAsync_WithConversationId_StillReturnsError()
    {
        var service = CreateStubService();
        var result = await service.SendMessageAsync("Hello", "conv-123");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SendMessageAsync_WithCancellationToken_StillReturnsError()
    {
        var service = CreateStubService();
        using var cts = new CancellationTokenSource();
        var result = await service.SendMessageAsync("Hello", cancellationToken: cts.Token);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetConversationHistoryAsync_ReturnsEmptyList()
    {
        var service = CreateStubService();
        var history = await service.GetConversationHistoryAsync("conv-123");

        history.Should().NotBeNull();
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearConversationAsync_CompletesWithoutError()
    {
        var service = CreateStubService();
        var act = () => service.ClearConversationAsync("conv-123");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void GetActiveConversations_ReturnsEmptyEnumerable()
    {
        var service = CreateStubService();
        var conversations = service.GetActiveConversations();

        conversations.Should().NotBeNull();
        conversations.Should().BeEmpty();
    }

    [Fact]
    public async Task IsAvailableAsync_ReturnsFalse()
    {
        var service = CreateStubService();
        var available = await service.IsAvailableAsync();

        available.Should().BeFalse();
    }

    [Fact]
    public async Task IsAvailableAsync_WithCancellationToken_ReturnsFalse()
    {
        var service = CreateStubService();
        using var cts = new CancellationTokenSource();
        var available = await service.IsAvailableAsync(cts.Token);

        available.Should().BeFalse();
    }
}
