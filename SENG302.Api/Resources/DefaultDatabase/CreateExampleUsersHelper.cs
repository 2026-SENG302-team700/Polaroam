using Microsoft.AspNetCore.Identity;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Responses;

namespace SENG302.Api.Resources.DefaultDatabase;

public static class CreateExampleUsersHelper
{
    private const string ExamplePassword = "P4$$word";

    /// <summary>
    /// Creates realistic users and travel data for local demonstrations and testing.
    /// Existing users and trips are left unchanged.
    /// 
    /// The majority of the seed data was generated using ChatGPT, with some minor edits to ensure accuracy and realism.
    /// </summary>
    public static async Task CreateExamples(DatabaseContext db, string fileStoragePath)
    {
        var john = await GetOrCreateUser(
            db,
            "John Whitaker",
            "john@example.com",
            "NZ"
        );
        var jane = await GetOrCreateUser(
            db,
            "Jane Watson",
            "jane@example.com",
            "GB"
        );
        var jeff = await GetOrCreateUser(
            db,
            "Jeff Williams",
            "jeff@example.com",
            "AU"
        );

        var southIslandTrip = await CreateTripIfMissing(
            db,
            fileStoragePath,
            john,
            "South Island Road Trip",
            "A winter road trip through some of the South Island's most memorable landscapes.",
            UtcDate(2025, 7, 5),
            UtcDate(2025, 7, 16),
            [jane.Id],
            [
                new SeedPost(
                    "Lake Tekapo at Sunset",
                    "We arrived just before sunset and walked along the lakefront. The colour of the water was incredible.",
                    UtcDate(2025, 7, 7),
                    23678492,
                    "relation",
                    "Lake Tekapo",
                    "-44.0047",
                    "170.4771",
                    "Lake Tekapo, Mackenzie District, Canterbury, New Zealand",
                    john.Id,
                    "lake-tekapo.jpeg",
                    ["lake", "mountains", "sunset", "New Zealand"]
                ),
                new SeedPost(
                    "A Day in Queenstown",
                    "Coffee by the lake, the gondola in the afternoon, and dinner overlooking the Remarkables.",
                    UtcDate(2025, 7, 11),
                    2316595,
                    "relation",
                    "Queenstown",
                    "-45.0312",
                    "168.6626",
                    "Queenstown, Queenstown-Lakes District, Otago, New Zealand",
                    jane.Id,
                    "queenstown.jpg",
                    ["Queenstown", "lake", "mountains", "New Zealand"]
                ),
                new SeedPost(
                    "Milford Sound Cruise",
                    "A rainy morning turned into dramatic waterfalls all around the fiord. Easily a highlight of the trip.",
                    UtcDate(2025, 7, 14),
                    11189570,
                    "relation",
                    "Milford Sound",
                    "-44.6414",
                    "167.8974",
                    "Milford Sound, Southland, New Zealand",
                    john.Id,
                    "milford-sound.jpg",
                    ["Milford Sound", "fiord", "waterfalls", "New Zealand"]
                ),
            ]
        );

        var sydneyTrip = await CreateTripIfMissing(
            db,
            fileStoragePath,
            jeff,
            "Sydney Harbour Weekend",
            "A long weekend exploring Sydney's harbour, beaches, and neighbourhoods.",
            UtcDate(2026, 2, 6),
            UtcDate(2026, 2, 9),
            [john.Id],
            [
                new SeedPost(
                    "Morning at the Opera House",
                    "We walked through the Botanic Garden and reached the Opera House before the crowds arrived.",
                    UtcDate(2026, 2, 7),
                    5750005,
                    "way",
                    "Sydney Opera House",
                    "-33.8568",
                    "151.2153",
                    "Sydney Opera House, Bennelong Point, Sydney, New South Wales, Australia",
                    jeff.Id,
                    "sydney-opera-house.jpg",
                    ["Sydney", "Opera House", "harbour", "architecture"]
                ),
                new SeedPost(
                    "Bondi to Coogee Walk",
                    "A sunny afternoon walking the coastal track, with plenty of stops for swimming and photos.",
                    UtcDate(2026, 2, 8),
                    5841149,
                    "relation",
                    "Bondi Beach",
                    "-33.8915",
                    "151.2767",
                    "Bondi Beach, Sydney, New South Wales, Australia",
                    john.Id,
                    "bondi-beach.jpg",
                    ["Bondi Beach", "beach", "ocean", "Australia"]
                ),
            ]
        );

        var europeTrip = await CreateTripIfMissing(
            db,
            fileStoragePath,
            jane,
            "European Summer",
            "Three weeks travelling by train through France, Switzerland, and Italy.",
            UtcDate(2025, 8, 20),
            UtcDate(2025, 9, 10),
            [jeff.Id],
            [
                new SeedPost(
                    "First Evening in Paris",
                    "We wandered along the Seine and watched the Eiffel Tower light up after dark.",
                    UtcDate(2025, 8, 21),
                    5013364,
                    "way",
                    "Eiffel Tower",
                    "48.8583",
                    "2.2945",
                    "Eiffel Tower, Paris, Ile-de-France, France",
                    jane.Id,
                    "paris.jpeg",
                    ["Paris", "Eiffel Tower", "city", "France"]
                ),
                new SeedPost(
                    "Train Through the Swiss Alps",
                    "The journey to Interlaken was almost as memorable as the destination, with mountain views the whole way.",
                    UtcDate(2025, 8, 28),
                    1682338,
                    "relation",
                    "Interlaken",
                    "46.6863",
                    "7.8632",
                    "Interlaken, Bern, Switzerland",
                    jeff.Id,
                    "interlaken.jpg",
                    ["Interlaken", "Alps", "mountains", "Switzerland"]
                ),
                new SeedPost(
                    "Exploring Rome",
                    "An early start at the Colosseum followed by an afternoon exploring the smaller streets around Monti.",
                    UtcDate(2025, 9, 5),
                    1877004,
                    "way",
                    "Colosseum",
                    "41.8902",
                    "12.4922",
                    "Colosseum, Piazza del Colosseo, Rome, Lazio, Italy",
                    jane.Id,
                    "rome.jpg",
                    ["Rome", "Colosseum", "history", "Italy"]
                ),
            ]
        );

        await CreateNotificationsIfMissing(
            db,
            john,
            jane,
            jeff,
            southIslandTrip,
            sydneyTrip,
            europeTrip
        );
    }

    private static async Task<User> GetOrCreateUser(
        DatabaseContext db,
        string displayName,
        string email,
        string country
    )
    {
        var existingUser = db.Users.FirstOrDefault(
            user => user.Email.ToLower() == email.ToLower()
        );
        if (existingUser is not null)
        {
            return existingUser;
        }

        var user = new User
        {
            DisplayName = displayName,
            Email = email,
            Country = country,
            EmailVerified = true,
            AcceptedAiPrivacyPolicy = true,
            AiTaggingEnabled = true,
            TimeCreated = DateTimeOffset.UtcNow,
        };
        user.PasswordKey = new PasswordHasher<User>().HashPassword(user, ExamplePassword);

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<Trip> CreateTripIfMissing(
        DatabaseContext db,
        string fileStoragePath,
        User owner,
        string title,
        string description,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        List<int> collaborators,
        List<SeedPost> posts
    )
    {
        var existingTrip = db.Trips.FirstOrDefault(
            trip => trip.UserId == owner.Id && trip.Title == title
        );
        if (existingTrip is not null)
        {
            return existingTrip;
        }

        var trip = new Trip
        {
            Title = title,
            Description = description,
            UserId = owner.Id,
            StartDate = startDate,
            EndDate = endDate,
            Collaborators = collaborators,
        };

        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var album = new Album { TripId = trip.Id };
        db.Albums.Add(album);

        foreach (var seedPost in posts)
        {
            var post = new Post
            {
                TripId = trip.Id,
                Title = seedPost.Title,
                Content = seedPost.Content,
                Date = seedPost.Date,
                CreationTime = seedPost.Date.AddHours(12),
                OsmId = seedPost.OsmId,
                OsmType = seedPost.OsmType,
                LocationName = seedPost.LocationName,
                CreatorId = seedPost.CreatorId,
            };

            db.Posts.Add(post);
            await db.SaveChangesAsync();

            var image = await SaveExampleImage(
                db,
                fileStoragePath,
                seedPost.ImageFileName,
                seedPost.CreatorId,
                seedPost.Tags
            );
            post.PhotoIds.Add(image.Id);
            album.PhotoIds.Add(image.Id);

            db.Locations.Add(new Location
            {
                PostId = post.PostId,
                OsmId = seedPost.OsmId,
                OsmType = seedPost.OsmType,
                Lat = seedPost.Latitude,
                Lon = seedPost.Longitude,
                Name = seedPost.LocationName,
                DisplayName = seedPost.DisplayName,
            });
        }

        await db.SaveChangesAsync();
        return trip;
    }

    private static async Task<CustomFile> SaveExampleImage(
        DatabaseContext db,
        string fileStoragePath,
        string fileName,
        int ownerId,
        List<string> tags
    )
    {
        var imagePath = Path.Combine(
            AppContext.BaseDirectory,
            "Resources",
            "DefaultDatabase",
            "ExampleImages",
            fileName
        );

        Directory.CreateDirectory(fileStoragePath);
        var fileKey = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var storedImagePath = Path.Combine(fileStoragePath, fileKey);
        await using (var source = File.OpenRead(imagePath))
        {
            await using var destination = File.Create(storedImagePath);
            await source.CopyToAsync(destination);
        }

        var image = new CustomFile
        {
            OwnerId = ownerId,
            FileKey = fileKey,
            OriginalFileName = fileName,
            MimeType = GetImageContentType(fileName),
            FileSize = new FileInfo(imagePath).Length,
            Tags = tags,
        };
        db.CustomFiles.Add(image);
        await db.SaveChangesAsync();
        return image;
    }

    private static string GetImageContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream",
        };

    private static async Task CreateNotificationsIfMissing(
        DatabaseContext db,
        User john,
        User jane,
        User jeff,
        Trip southIslandTrip,
        Trip sydneyTrip,
        Trip europeTrip
    )
    {
        if (db.Notifications.Any(notification =>
                notification.UserId == john.Id ||
                notification.UserId == jane.Id ||
                notification.UserId == jeff.Id))
        {
            return;
        }

        db.Notifications.AddRange(
            new Notification
            {
                UserId = john.Id,
                HeaderText = "New collaborator activity",
                BodyText = $"Jane added a post to '{southIslandTrip.Title}'.",
                ReadStatus = false,
                Timestamp = DateTimeOffset.UtcNow.AddHours(-2),
            },
            new Notification
            {
                UserId = john.Id,
                HeaderText = "Trip updated",
                BodyText = $"Jeff updated the details for '{sydneyTrip.Title}'.",
                ReadStatus = true,
                Timestamp = DateTimeOffset.UtcNow.AddDays(-2),
            },
            new Notification
            {
                UserId = jane.Id,
                HeaderText = "New collaborator activity",
                BodyText = $"John added a post to '{southIslandTrip.Title}'.",
                ReadStatus = false,
                Timestamp = DateTimeOffset.UtcNow.AddHours(-5),
            },
            new Notification
            {
                UserId = jane.Id,
                HeaderText = "Trip updated",
                BodyText = $"Jeff added a post to '{europeTrip.Title}'.",
                ReadStatus = true,
                Timestamp = DateTimeOffset.UtcNow.AddDays(-3),
            },
            new Notification
            {
                UserId = jeff.Id,
                HeaderText = "New collaborator activity",
                BodyText = $"John added a post to '{sydneyTrip.Title}'.",
                ReadStatus = false,
                Timestamp = DateTimeOffset.UtcNow.AddHours(-1),
            }
        );

        await db.SaveChangesAsync();
    }

    private static DateTimeOffset UtcDate(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);

    private sealed record SeedPost(
        string Title,
        string Content,
        DateTimeOffset Date,
        long OsmId,
        string OsmType,
        string LocationName,
        string Latitude,
        string Longitude,
        string DisplayName,
        int CreatorId,
        string ImageFileName,
        List<string> Tags
    );
}
