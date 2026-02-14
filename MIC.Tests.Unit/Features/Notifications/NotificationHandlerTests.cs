using FluentAssertions;
using Moq;
using Xunit;
using MIC.Core.Application.Notifications.Queries.GetNotifications;
using MIC.Core.Application.Notifications.Commands.MarkRead;
using MIC.Core.Application.Notifications.Commands.MarkAllRead;
using MIC.Core.Application.Notifications.Commands.Dismiss;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MIC.Tests.Unit.Features.Notifications;

public sealed class NotificationQueryHandlerTests
{
    [Fact]
    public async Task GetNotificationsQueryHandler_ReturnsCorrectUnreadCount()
    {
        // Arrange
        var userId = "user-123";
        var notifications = new List<Notification>
        {
            Notification.Create("Alert 1", "Message 1", NotificationType.Alert, NotificationSeverity.Critical, userId),
            Notification.Create("Alert 2", "Message 2", NotificationType.Alert, NotificationSeverity.Warning, userId),
            Notification.Create("Email", "Message 3", NotificationType.Email, NotificationSeverity.Info, userId)
        };
        notifications[0].MarkAsRead(); // Mark first as read

        var repositoryMock = new Mock<INotificationRepository>();
        var sessionServiceMock = new Mock<ISessionService>();
        var loggerMock = new Mock<ILogger<GetNotificationsQueryHandler>>();

        repositoryMock.Setup(r => r.GetUserNotificationsAsync(userId, null, false, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notifications.Where(n => !n.IsRead).ToList());
        repositoryMock.Setup(r => r.GetUnreadCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        repositoryMock.Setup(r => r.GetTotalCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        sessionServiceMock.Setup(s => s.GetCurrentUserId()).Returns(userId);

        var handler = new GetNotificationsQueryHandler(repositoryMock.Object, sessionServiceMock.Object, loggerMock.Object);

        // Act
        var result = await handler.Handle(new GetNotificationsQuery(), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.UnreadCount.Should().Be(2);
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetNotificationsQueryHandler_WithTypeFilter_ReturnsOnlyThatType()
    {
        // Arrange
        var userId = "user-123";
        var alertNotifications = new List<Notification>
        {
            Notification.Create("Alert 1", "Message 1", NotificationType.Alert, NotificationSeverity.Critical, userId),
            Notification.Create("Alert 2", "Message 2", NotificationType.Alert, NotificationSeverity.Warning, userId)
        };

        var repositoryMock = new Mock<INotificationRepository>();
        var sessionServiceMock = new Mock<ISessionService>();
        var loggerMock = new Mock<ILogger<GetNotificationsQueryHandler>>();

        repositoryMock.Setup(r => r.GetUserNotificationsAsync(userId, NotificationType.Alert, false, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alertNotifications);
        repositoryMock.Setup(r => r.GetUnreadCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        repositoryMock.Setup(r => r.GetTotalCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        sessionServiceMock.Setup(s => s.GetCurrentUserId()).Returns(userId);

        var handler = new GetNotificationsQueryHandler(repositoryMock.Object, sessionServiceMock.Object, loggerMock.Object);
        var query = new GetNotificationsQuery { FilterType = NotificationType.Alert };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Notifications.Count.Should().Be(2);
        result.Value.Notifications.All(n => n.Type == NotificationType.Alert).Should().BeTrue();
    }

    [Fact]
    public async Task GetNotificationsQueryHandler_TimeAgo_CalculatesCorrectly()
    {
        // Arrange
        var userId = "user-123";
        var now = DateTime.UtcNow;
        var oneHourAgo = now.AddHours(-1);
        
        var notifications = new List<Notification>
        {
            Notification.Create("Old", "Message", NotificationType.System, NotificationSeverity.Info, userId)
        };
        notifications[0].GetType().GetProperty("CreatedAt")?.SetValue(notifications[0], oneHourAgo);

        var repositoryMock = new Mock<INotificationRepository>();
        var sessionServiceMock = new Mock<ISessionService>();
        var loggerMock = new Mock<ILogger<GetNotificationsQueryHandler>>();

        repositoryMock.Setup(r => r.GetUserNotificationsAsync(userId, null, false, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notifications);
        repositoryMock.Setup(r => r.GetUnreadCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        repositoryMock.Setup(r => r.GetTotalCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        sessionServiceMock.Setup(s => s.GetCurrentUserId()).Returns(userId);

        var handler = new GetNotificationsQueryHandler(repositoryMock.Object, sessionServiceMock.Object, loggerMock.Object);

        // Act
        var result = await handler.Handle(new GetNotificationsQuery(), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Notifications[0].TimeAgo.Should().Contain("hr ago");
    }
}

public sealed class MarkNotificationReadCommandHandlerTests
{
    [Fact]
    public async Task MarkNotificationReadCommand_ExistingNotification_MarksAsRead()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var notification = Notification.Create("Title", "Message", NotificationType.Alert, NotificationSeverity.Warning, "user-123");

        var repositoryMock = new Mock<INotificationRepository>();
        var loggerMock = new Mock<ILogger<MarkNotificationReadCommandHandler>>();

        repositoryMock.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        var handler = new MarkNotificationReadCommandHandler(repositoryMock.Object, loggerMock.Object);

        // Act
        var result = await handler.Handle(new MarkNotificationReadCommand { NotificationId = notificationId }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        notification.IsRead.Should().BeTrue();
        repositoryMock.Verify(r => r.UpdateAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkNotificationReadCommand_NonExistent_ReturnsNotFoundError()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var repositoryMock = new Mock<INotificationRepository>();
        var loggerMock = new Mock<ILogger<MarkNotificationReadCommandHandler>>();

        repositoryMock.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);

        var handler = new MarkNotificationReadCommandHandler(repositoryMock.Object, loggerMock.Object);

        // Act
        var result = await handler.Handle(new MarkNotificationReadCommand { NotificationId = notificationId }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Contain("not_found");
    }
}

public sealed class DismissNotificationCommandHandlerTests
{
    [Fact]
    public async Task DismissNotificationCommand_ExistingNotification_SetsDismissed()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var notification = Notification.Create("Title", "Message", NotificationType.Email, NotificationSeverity.Info, "user-123");

        var repositoryMock = new Mock<INotificationRepository>();
        var loggerMock = new Mock<ILogger<DismissNotificationCommandHandler>>();

        repositoryMock.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        var handler = new DismissNotificationCommandHandler(repositoryMock.Object, loggerMock.Object);

        // Act
        var result = await handler.Handle(new DismissNotificationCommand { NotificationId = notificationId }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        notification.IsDismissed.Should().BeTrue();
        repositoryMock.Verify(r => r.UpdateAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
    }
}
