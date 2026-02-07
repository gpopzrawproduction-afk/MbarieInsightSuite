using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Tests.Integration.Infrastructure;
using Xunit;

namespace MIC.Tests.Integration.Features.Email;

public sealed class EmailRepositoryIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task GetEmailsAsync_WithUnreadFilter_ReturnsDescendingInbox()
    {
        var user = await SeedUserAsync("emailuser", "Password123!");
        var account = await SeedEmailAccountAsync(user.Id, "emailuser@example.com");

        var oldest = await SeedEmailAsync(user.Id, account.Id, "Old", EmailFolder.Inbox, isRead: true, receivedAt: DateTime.UtcNow.AddHours(-3));
        var middle = await SeedEmailAsync(user.Id, account.Id, "Middle", EmailFolder.Inbox, isRead: false, receivedAt: DateTime.UtcNow.AddHours(-2));
        var newest = await SeedEmailAsync(user.Id, account.Id, "Newest", EmailFolder.Inbox, isRead: false, receivedAt: DateTime.UtcNow.AddHours(-1));
        await SeedEmailAsync(user.Id, account.Id, "Archived", EmailFolder.Archive, isRead: false);

        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();

        var results = await repository.GetEmailsAsync(user.Id, folder: EmailFolder.Inbox, isUnread: true);

        results.Should().HaveCount(2);
        results.Should().BeInDescendingOrder(email => email.ReceivedDate);
        results.Select(e => e.Subject).Should().ContainInOrder(new[] { "Newest", "Middle" });
        results.Select(e => e.Id).Should().NotContain(oldest.Id);
    }

    [Fact]
    public async Task MarkAsReadAsync_PersistsChangesAndUpdatesCounts()
    {
        var user = await SeedUserAsync("emailuser2", "Password123!");
        var account = await SeedEmailAccountAsync(user.Id, "emailuser2@example.com");

        var unreadOne = await SeedEmailAsync(user.Id, account.Id, "Unread One", EmailFolder.Inbox, isRead: false);
        var unreadTwo = await SeedEmailAsync(user.Id, account.Id, "Unread Two", EmailFolder.Inbox, isRead: false);

        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();

        await repository.MarkAsReadAsync(new[] { unreadOne.Id, unreadTwo.Id });

        var unreadCount = await repository.GetUnreadCountAsync(user.Id);
        unreadCount.Should().Be(0);

        var persisted = await QueryDbContextAsync(async context =>
            await context.EmailMessages
                .Where(email => email.Id == unreadOne.Id || email.Id == unreadTwo.Id)
                .ToListAsync());

        persisted.Should().OnlyContain(email => email.IsRead);
    }

    [Fact]
    public async Task GetRequiresResponseCountAsync_ReturnsExpectedValue()
    {
        var user = await SeedUserAsync("emailuser3", "Password123!");
        var account = await SeedEmailAccountAsync(user.Id, "emailuser3@example.com");

        await SeedEmailAsync(user.Id, account.Id, "Regular", EmailFolder.Inbox, requiresResponse: false);
        await SeedEmailAsync(user.Id, account.Id, "Needs Response", EmailFolder.Inbox, requiresResponse: true);
        await SeedEmailAsync(user.Id, account.Id, "Archived Response", EmailFolder.Archive, requiresResponse: true, isRead: true);

        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();

        var requiresResponseCount = await repository.GetRequiresResponseCountAsync(user.Id);

        requiresResponseCount.Should().Be(1);
    }
}
