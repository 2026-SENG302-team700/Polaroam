using Microsoft.AspNetCore.Identity;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;

namespace SENG302.Api.Resources.DefaultDatabase;

public static class CreateExampleUsersHelper
{
    /// <summary>
    /// Makes some pre-made examples of users
    /// </summary>
    /// <param name="db">The database context</param>
    public static async Task CreateExamples(DatabaseContext db)
    {
        await CreateExample_Admin(db);
        await CreateExample_John(db);
        await CreateExample_Jane(db);
    }

    /// <summary>
    /// Creates a blank admin account
    /// </summary>
    /// <param name="dbContext">The database context</param>
    private static async Task CreateExample_Admin(DatabaseContext dbContext)
    {
        // Add admin user
        if (!dbContext.Users.Any(u => u.Email.ToLower().Equals("admin@outstanding.com")))
        {
            var user = new User
            {
                DisplayName = "admin",
                Email = "admin@outstanding.com",
                Country = "NZ",
                EmailVerified = true
            };
            var hasher = new PasswordHasher<User>();
            user.PasswordKey = hasher.HashPassword(user, "Team700!");
            dbContext.Users.Add(user);

            await dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Creates a user John Whitaker, john@example.com
    /// He has 20 trips with a varying amount of posts in each
    /// Some posts have a long description
    /// </summary>
    /// <param name="dbContext">The database context</param>
    private static async Task CreateExample_John(DatabaseContext dbContext)
    {
        if (!dbContext.Users.Any(u => u.Email.ToLower().Equals("john@example.com")))
        {
            var user = new User
            {
                DisplayName = "John Whitaker",
                Email = "john@example.com",
                Country = "US",
                EmailVerified = true
            };
            var hasher = new PasswordHasher<User>();
            user.PasswordKey = hasher.HashPassword(user, "P4$$word");
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            for (int i = 20; i < 40; i++)
            {
                var trip = new Trip
                {
                    Id = i,
                    Title = "Name " + i.ToString(),
                    UserId = user.Id,
                    StartDate = DateTimeOffset.UtcNow,
                    EndDate = DateTimeOffset.UtcNow.AddDays(1),
                    Collaborators = [1, 3]
                };

                dbContext.Add(trip);

                var album = new Album
                {
                    TripId = i,
                    PhotoIds = new List<int>()
                };

                dbContext.Add(album);

                await dbContext.SaveChangesAsync();

                for (int j = 0; j < (i % 10); j++)
                {
                    var post = new Post
                    {
                        Title = "Post " + j.ToString(),
                        TripId = trip.Id,
                        Content = (j % 5) != 4 ? "Number: " + j.ToString() : "antimony, arsenic, aluminum, selenium And hydrogen, and oxygen, and nitrogen, and rhenium And nickel, neodymium, neptunium, germanium And iron, americium, ruthenium, uranium Europium, zirconium, lutetium, vanadium And lanthanum, and osmium, and astatine, and radium And gold, protactinium, and indium, and gallium And iodine, and thorium, and thulium, and thallium There's yttrium, ytterbium, actinium, rubidium And boron, gadolinium, niobium, iridium And strontium, and silicon, and silver, and samarium And bismuth, bromine, lithium, beryllium, and barium",
                        Date = DateTimeOffset.MaxValue,
                        CreationTime = DateTimeOffset.UtcNow,
                        OsmId = 1,
                        OsmType = "node",
                        LocationName = "Location_" + j
                    };
                    dbContext.Add(post);
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }


    /// <summary>
    /// Adds a blank user Jane Watson, jane@example.com to the database
    /// </summary>
    /// <param name="dbContext">The database context</param>
    private static async Task CreateExample_Jane(DatabaseContext dbContext)
    {
        if (!dbContext.Users.Any(u => u.Email.ToLower().Equals("jane@example.com")))
        {
            var user = new User
            {
                DisplayName = "Jane Watson",
                Email = "jane@example.com",
                Country = "CR",
                EmailVerified = true
            };
            var hasher = new PasswordHasher<User>();
            user.PasswordKey = hasher.HashPassword(user, "P4$$word");
            await dbContext.Users.AddAsync(user);

            var notification1 = new Notification
            {
                BodyText = "Andrew has posted on your timeline",
                HeaderText = "New Post!",
                ReadStatus = false,
                Timestamp = DateTimeOffset.UtcNow.AddDays(-5),
                UserId = 3,
            };

            var notification2 = new Notification
            {
                BodyText = "Sarah has commented on your post",
                HeaderText = "New Comment!",
                ReadStatus = false,
                Timestamp = DateTimeOffset.UtcNow.AddDays(-4),
                UserId = 3,
            };

            var notification3 = new Notification
            {
                BodyText = "Michael has liked your post",
                HeaderText = "Post Liked!",
                ReadStatus = true,
                Timestamp = DateTimeOffset.UtcNow.AddDays(-3),
                UserId = 3,
            };

            var notification4 = new Notification
            {
                BodyText = "Emma has invited you to join a trip",
                HeaderText = "Trip Invitation!",
                ReadStatus = false,
                Timestamp = DateTimeOffset.UtcNow.AddDays(-2),
                UserId = 3,
            };

            var notification5 = new Notification
            {
                BodyText = "James has started following you",
                HeaderText = "New Follower!",
                ReadStatus = true,
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1),
                UserId = 3,
            };

            var notification6 = new Notification
            {
                BodyText = "Olivia has commented on your trip",
                HeaderText = "New Comment!",
                ReadStatus = false,
                Timestamp = DateTimeOffset.UtcNow.AddHours(-20),
                UserId = 3,
            };

            var notification7 = new Notification
            {
                BodyText = "Daniel has accepted your trip request",
                HeaderText = "Request Accepted!",
                ReadStatus = false,
                Timestamp = DateTimeOffset.UtcNow.AddHours(-15),
                UserId = 3,
            };

            var notification8 = new Notification
            {
                BodyText = "Charlotte has sent you a message",
                HeaderText = "New Message!",
                ReadStatus = true,
                Timestamp = DateTimeOffset.UtcNow.AddHours(-10),
                UserId = 3,
            };

            var notification9 = new Notification
            {
                BodyText = "Liam has joined your trip",
                HeaderText = "New Collaborator!",
                ReadStatus = false,
                Timestamp = DateTimeOffset.UtcNow.AddHours(-6),
                UserId = 3,
            };

            var notification10 = new Notification
            {
                BodyText = "Sophie has liked your comment",
                HeaderText = "Comment Liked!",
                ReadStatus = false,
                Timestamp = DateTimeOffset.UtcNow.AddHours(-3),
                UserId = 3,
            };

            var notification11 = new Notification
            {
                BodyText = "Noah has mentioned you in a post",
                HeaderText = "You Were Mentioned!",
                ReadStatus = false,
                Timestamp = DateTimeOffset.UtcNow.AddHours(-1),
                UserId = 3,
            };

            await dbContext.Notifications.AddAsync(notification1);
            await dbContext.Notifications.AddAsync(notification2);
            await dbContext.Notifications.AddAsync(notification3);
            await dbContext.Notifications.AddAsync(notification4);
            await dbContext.Notifications.AddAsync(notification5);
            await dbContext.Notifications.AddAsync(notification6);
            await dbContext.Notifications.AddAsync(notification7);
            await dbContext.Notifications.AddAsync(notification8);
            //await dbContext.Notifications.AddAsync(notification9);
            // await dbContext.Notifications.AddAsync(notification10);
            // await dbContext.Notifications.AddAsync(notification11);

            var notification12 = new TripInvitationNotification
            {
                BodyText = "Andrew has invited you to his trip 'Helinksi'",
                HeaderText = "You're famous!",
                ReadStatus = true,
                Timestamp = DateTimeOffset.UtcNow.AddDays(-2),
                TripId = 1,
                UserId = 3,
            };
            await dbContext.Notifications.AddAsync(notification12);

            await dbContext.SaveChangesAsync();
        }
    }
}