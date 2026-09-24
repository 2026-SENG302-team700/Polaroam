using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services;
using Shouldly;

namespace SENG302.Api.Tests.Integration.Services;

public class NotificationServiceTests : BaseIntegrationTestFixture
{
    private INotificationService ServiceUnderTest => ServiceProvider.GetRequiredService<INotificationService>();

    public NotificationServiceTests(
        WebApplicationFactory<Program> webApplicationFactory
    ) : base(webApplicationFactory)
    {
    }

    private static Notification CreateNotification(
        int userId,
        string header,
        bool readStatus = false,
        bool handled = false,
        DateTimeOffset? timestamp = null)
    {
        return new Notification
        {
            UserId = userId,
            HeaderText = header,
            BodyText = "body",
            ReadStatus = readStatus,
            Handled = handled,
            Timestamp = timestamp ?? DateTimeOffset.UtcNow
        };
    }

    [Fact]
    public async Task AddNotification_ValidNotification_PersistsNotification()
    {
        var notification = CreateNotification(
            userId: 1,
            header: "New notification"
        );

        await ServiceUnderTest.AddNotificationAsync(notification);

        await using var context = await DbContextFactory.CreateDbContextAsync();
        var persistedNotification = await context.Notifications.SingleAsync();

        persistedNotification.UserId.ShouldBe(1);
        persistedNotification.HeaderText.ShouldBe("New notification");
        persistedNotification.BodyText.ShouldBe("body");
        persistedNotification.ReadStatus.ShouldBeFalse();
        persistedNotification.Handled.ShouldBeFalse();
    }

    [Fact]
    public async Task GetNotificationsByUserIdAsync_ReturnsNotificationsOwnedByUser()
    {
        await using (var context = await DbContextFactory.CreateDbContextAsync())
        {
            context.Notifications.AddRange(
                CreateNotification(1, "ones first notification"),
                CreateNotification(2, "twos notification"),
                CreateNotification(1, "ones second notification")
            );

            await context.SaveChangesAsync();
        }

        var notifications = await ServiceUnderTest.GetNotificationsByUserIdAsync(1, 10);

        notifications.Count.ShouldBe(2);
        notifications.ShouldAllBe(notification => notification.UserId == 1);
    }

    [Fact]
    public async Task GetNotificationsByUser_NotificationsExist_ReturnsUreadBeforeRead()
    {
        var timestamp = DateTimeOffset.UtcNow;

        await using (var context = await DbContextFactory.CreateDbContextAsync())
        {
            context.Notifications.AddRange(
                CreateNotification(
                    1,
                    "Read notification",
                    readStatus: true,
                    timestamp: timestamp.AddMinutes(2)
                ),
                CreateNotification(
                    1,
                    "Unread notification",
                    readStatus: false,
                    timestamp: timestamp
                )
            );

            await context.SaveChangesAsync();
        }

        var notifications = await ServiceUnderTest.GetNotificationsByUserIdAsync(1, 10);

        notifications.Count.ShouldBe(2);
        notifications[0].HeaderText.ShouldBe("Unread notification");
        notifications[1].HeaderText.ShouldBe("Read notification");
    }

    [Fact]
    public async Task GetNotificationsByUserIdAsync_AllRead_ReturnsNewestFirst()
    {
        var timestamp = DateTimeOffset.UtcNow;

        await using (var context = await DbContextFactory.CreateDbContextAsync())
        {
            context.Notifications.AddRange(
                CreateNotification(
                    1,
                    "old notification",
                    timestamp: timestamp.AddHours(-2)
                ),
                CreateNotification(
                    1,
                    "new notification",
                    timestamp: timestamp
                ),
                CreateNotification(
                    1,
                    "in between notification",
                    timestamp: timestamp.AddHours(-1)
                )
            );

            await context.SaveChangesAsync();
        }

        var notifications = await ServiceUnderTest.GetNotificationsByUserIdAsync(1, 10);
        notifications.Select(notification => notification.HeaderText).ShouldBe(["new notification", "in between notification", "old notification"]);
    }

    [Fact]
    public async Task GetNotificationsByUserIdAsync_CountProvided_LimitsResults()
    {
        await using (var context = await DbContextFactory.CreateDbContextAsync())
        {
            context.Notifications.AddRange(
                CreateNotification(1, "Notification one"),
                CreateNotification(1, "Notification two"),
                CreateNotification(1, "Notification three")
            );

            await context.SaveChangesAsync();
        }

        var notifications = await ServiceUnderTest.GetNotificationsByUserIdAsync(1, 2);
        notifications.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SetNotificationStatusById_UnreadNotification_MarksAsRead()
    {
        var notification = CreateNotification(
            1,
            "Unread notification",
            readStatus: false
        );

        await using (var context = await DbContextFactory.CreateDbContextAsync())
        {
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
        }

        await ServiceUnderTest.SetNotificationStatusByIdAsync(
            notification.Id,
            true
        );

        await using var verificationContext = await DbContextFactory.CreateDbContextAsync();

        var updatedNotification = await verificationContext.Notifications.SingleAsync(n => n.Id == notification.Id);

        updatedNotification.ReadStatus.ShouldBeTrue();
    }

    [Fact]
    public async Task SetNotificationStatusById_ReadNotification_MarksAsUnread()
    {
        var notification = CreateNotification(
            1,
            "Read notification",
            readStatus: true
        );

        await using (var context = await DbContextFactory.CreateDbContextAsync())
        {
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
        }

        await ServiceUnderTest.SetNotificationStatusByIdAsync(
            notification.Id,
            false
        );

        await using var verificationContext = await DbContextFactory.CreateDbContextAsync();

        var updatedNotification = await verificationContext.Notifications.SingleAsync(n => n.Id == notification.Id);

        updatedNotification.ReadStatus.ShouldBeFalse();
    }

    [Fact]
    public async Task SetNotificationHandledById_UnhandledNotification_MarksAsHandled()
    {
        var notification = CreateNotification(
            1,
            "Trip invitation",
            handled: false
        );

        await using (var context = await DbContextFactory.CreateDbContextAsync())
        {
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
        }

        await ServiceUnderTest.SetNotificationHandledByIdAsync(
            notification.Id,
            true
        );

        await using var verificationContext = await DbContextFactory.CreateDbContextAsync();

        var updatedNotification = await verificationContext.Notifications.SingleAsync(n => n.Id == notification.Id);

        updatedNotification.Handled.ShouldBeTrue();
    }

    [Fact]
    public async Task UserOwnsNotification_NotificationIsUsers_ReturnsTrue()
    {
        var notification = CreateNotification(1, "Owned notification");

        await using (var context = await DbContextFactory.CreateDbContextAsync())
        {
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
        }

        var result = await ServiceUnderTest.UserOwnsNotification(
            notification.Id,
            1
        );

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task UserOwnsNotification_IsOtherUsers_ReturnsFalse()
    {
        var notification = CreateNotification(1, "other users notification");

        await using (var context = await DbContextFactory.CreateDbContextAsync())
        {
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
        }

        var result = await ServiceUnderTest.UserOwnsNotification(
            notification.Id,
            2
        );

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task UserOwnsNotification_NotificationDoesNotExist_ReturnsFalse()
    {
        var result = await ServiceUnderTest.UserOwnsNotification(999, 1);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task GetUnreadCount_UserHasReadAndUnreadNotifications_ReturnsUnreadCount()
    {
        await using (var context = await DbContextFactory.CreateDbContextAsync())
        {
            context.Notifications.AddRange(
                CreateNotification(1, "Unread one"),
                CreateNotification(1, "Unread two"),
                CreateNotification(1, "Read", readStatus: true),
                CreateNotification(2, "other users unread notification")
            );

            await context.SaveChangesAsync();
        }

        var unreadCount = await ServiceUnderTest.GetUnreadCount(1);

        unreadCount.ShouldBe(2);
    }

    [Fact]
    public async Task UserHasMoreNotificationsThanCount_ReturnsTrue()
    {
        await using (var context = await DbContextFactory.CreateDbContextAsync())
        {
            context.Notifications.AddRange(
                CreateNotification(1, "Notification one"),
                CreateNotification(1, "Notification two"),
                CreateNotification(1, "Notification three")
            );

            await context.SaveChangesAsync();
        }

        var result = await ServiceUnderTest.UserHasMoreThan(1, 2);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task UserDoesNotHaveMoreNotificationsThanCount_ReturnsFalse()
    {
        await using (var context = await DbContextFactory.CreateDbContextAsync())
        {
            context.Notifications.AddRange(
                CreateNotification(1, "Notification one"),
                CreateNotification(1, "Notification two")
            );

            await context.SaveChangesAsync();
        }

        var result = await ServiceUnderTest.UserHasMoreThan(1, 2);

        result.ShouldBeFalse();
    }
    
    [Fact]
    public async Task GetNotificationsByUserIdAsync_OffsetProvided_SkipsOffset()
    {
        var timestamp = DateTimeOffset.UtcNow;
        
        var newestNotification = CreateNotification(1, "Newest notification");
        var oldestNotification = CreateNotification(1, "Oldest notification", timestamp: timestamp.AddHours(-13));
        
        await using (var context = await DbContextFactory.CreateDbContextAsync())
        {
            context.Notifications.AddRange(
                CreateNotification(
                    1, 
                    "Notification two",
                    timestamp: timestamp.AddHours(-11) 
                ),                
                CreateNotification(
                    1, 
                    "Notification three",
                    timestamp: timestamp.AddHours(-10) 
                ),
                CreateNotification(
                    1, 
                    "Notification four",
                    timestamp: timestamp.AddHours(-9) 
                ),
                CreateNotification(
                    1, 
                    "Notification five",
                    timestamp: timestamp.AddHours(-8) 
                ),
                CreateNotification(
                    1, 
                    "Notification six",
                    timestamp: timestamp.AddHours(-7) 
                )
            );

            context.Notifications.Add(newestNotification);
            context.Notifications.Add(oldestNotification);
            
            await context.SaveChangesAsync();
        }

        var notifications = await ServiceUnderTest.GetNotificationsByUserIdAsync(1, 6, 1);
        notifications.Count.ShouldBe(6);
        notifications.ShouldNotContain(n => n.Id == newestNotification.Id);
        notifications.ShouldContain(n => n.Id == oldestNotification.Id);
    }
    
    [Fact]
    public async Task GetNotificationsByUserIdAsync_NoOffsetProvided_NoOffset()
    {
        var timestamp = DateTimeOffset.UtcNow;
        
        var newestNotification = CreateNotification(1, "Newest notification");
        var oldestNotification = CreateNotification(1, "Oldest notification", timestamp: timestamp.AddHours(-13));
        
        await using (var context = await DbContextFactory.CreateDbContextAsync())
        {
            context.Notifications.AddRange(
                CreateNotification(
                    1, 
                    "Notification two",
                    timestamp: timestamp.AddHours(-11) 
                ),                
                CreateNotification(
                    1, 
                    "Notification three",
                    timestamp: timestamp.AddHours(-10) 
                ),
                CreateNotification(
                    1, 
                    "Notification four",
                    timestamp: timestamp.AddHours(-9) 
                ),
                CreateNotification(
                    1, 
                    "Notification five",
                    timestamp: timestamp.AddHours(-8) 
                ),
                CreateNotification(
                    1, 
                    "Notification six",
                    timestamp: timestamp.AddHours(-7) 
                )
            );

            context.Notifications.Add(newestNotification);
            context.Notifications.Add(oldestNotification);
            
            await context.SaveChangesAsync();
        }

        var notifications = await ServiceUnderTest.GetNotificationsByUserIdAsync(1, 6, 0);
        notifications.Count.ShouldBe(6);
        notifications.ShouldNotContain(n => n.Id == oldestNotification.Id);
        notifications.ShouldContain(n => n.Id == newestNotification.Id);
    }
}
