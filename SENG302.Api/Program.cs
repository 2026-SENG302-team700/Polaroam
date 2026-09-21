using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SENG302.Api.DataAccess;
using SENG302.Api.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Resources.DefaultDatabase;
using SENG302.Api.Services.ImageTagging;
using SENG302.Api.Services.Email;

namespace SENG302.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // If no environment is set default it to Development (instead of Production)
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
        {
            builder.Environment.EnvironmentName = Environments.Development;
        }
        Console.WriteLine($"*** CURRENT ENVIRONMENT: {builder.Environment.EnvironmentName} ***");

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddOpenApi();
        }

        // Add services to the container Using `WithViews` registers the Antiforgery filters required for [ValidateAntiForgeryToken]
        builder.Services.AddControllersWithViews(options =>
        {
            // tells browsers to not cache API responses (was breaking deployment on firefox)
            options.Filters.Add(new ResponseCacheAttribute { NoStore = true, Location = ResponseCacheLocation.None });
        });

        // Add authorization service
        builder.Services.AddAuthorization();

        // Add rate limit handler
        builder.Services.AddSingleton(new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 1,
            TokensPerPeriod = 1,
            ReplenishmentPeriod = TimeSpan.FromMilliseconds(1500),
            AutoReplenishment = true,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 15
        }));

        builder.Services.AddTransient<NominatimRateLimitingHandler>();


        if (builder.Environment.IsProduction() || builder.Environment.IsStaging())
        {
            var dbHost = builder.Configuration["DB_HOST"];
            var dbName = builder.Configuration["DB_NAME"];
            var dbUser = builder.Configuration["DB_USER"];
            var dbPass = builder.Configuration["DB_PASS"];

            if (dbHost == null || dbName == null || dbUser == null || dbPass == null)
            {
                throw new InvalidOperationException("Missing required database environment variables.");
            }

            // build the connection string to be used by PosgreSQL
            var connectionString =
                $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPass};SSL Mode=Require;Trust Server Certificate=true";

            builder.Services.AddDbContextFactory<DatabaseContext>((_, options) =>
                options.UseNpgsql(connectionString)
                );
        }
        else
        {
            // Configure database context with factory pattern
            builder.Services.AddDbContextFactory<DatabaseContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db"));

        }

        builder.Services.AddHttpContextAccessor();

        // Register custom services 
        RegisterServices(builder.Services);

        // Setup antiforgery (CSRF)
        var cookiePolicy = builder.Environment.IsProduction() || builder.Environment.IsStaging()
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;

        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "X-CSRF-TOKEN-COOKIE";
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.HttpOnly = true;

            options.Cookie.SecurePolicy = cookiePolicy;
        });

        // Configure the cookie-based authentication and set security options 
        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "POLAROAM-AUTH-COOKIE";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = cookiePolicy;
                options.Cookie.SameSite = SameSiteMode.Lax;

                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.AccessDeniedPath = "/api/auth/access-denied";
            })
            .AddCookie("PasswordResetScheme", options =>
            {
                options.Cookie.Name = "POLAROAM-RESET-COOKIE";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = cookiePolicy;
                options.Cookie.SameSite = SameSiteMode.Lax;

                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            });

        // Add CORS for development only
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
        }

        // add the custom environment file
        builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true).AddEnvironmentVariables();

        // bind it in email service
        var emailSettings = new EmailSettings();

        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.GetSection("EmailSettings").Bind(emailSettings);
        }
        builder.Services.AddSingleton(emailSettings);
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddSingleton<IEmailQueue, EmailQueue>();
        builder.Services.AddHostedService<EmailBackgroundService>();
        builder.Services.AddTransient<ISmtpClientWrapper, SmtpClientWrapper>();
        builder.Services.AddSingleton<IImageTaggingQueue, ImageTaggingQueue>();
        builder.Services.AddHostedService<ImageTaggingBackgroundService>();

        var app = builder.Build();

        // Make sure we use forwarded headers in production so our api works behind reverse proxy (nginx) with https
        if (builder.Environment.IsProduction() || builder.Environment.IsStaging())
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
            });
        }

        InitializeDatabase(app.Services.CreateScope().ServiceProvider, !(app.Environment.IsProduction() || app.Environment.IsStaging()));

        var pathBase = app.Configuration["PathBase"];
        if (!string.IsNullOrEmpty(pathBase))
        {
            app.UsePathBase(pathBase);
        }

        app.UseRouting();

        // Only use https redirects in production
        if (builder.Environment.IsProduction() || builder.Environment.IsStaging())
        {
            app.UseHttpsRedirection();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseCors("AllowFrontend");
        }

        // Tell app to use authentication and authorization middleware
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseAntiforgery();

        // CSRF token endpoint`
        app.MapGet("/api/csrf-token", (Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery, HttpContext context) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new { token = tokens.RequestToken });
        });

        app.MapControllers();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "SENG302  API v1");
            });
        }

        // returns http code 413 payload to large if size exceeds kestrel size limit
        // block below written by claude.
        app.Use(async (ctx, next) =>
        {
            try { await next(); }
            catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge)
            {
                ctx.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await ctx.Response.WriteAsJsonAsync(new { error = "Upload too large." });
            }
        });
        // end of ai gen code.

        app.Run();
    }

    protected static async Task InitializeDatabase(IServiceProvider serviceProvider, bool isDevelopment)
    {
        var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<DatabaseContext>>();
        var dbContext = dbContextFactory.CreateDbContext();

        try
        {
            if (isDevelopment)
            {
                await dbContext.Database.EnsureCreatedAsync();
                await dbContext.Database.MigrateAsync();
            }
            else
            {
                await dbContext.Database.MigrateAsync();
            }

            await CreateExampleUsersHelper.CreateExamples(dbContext);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error occured: ", e);
            throw;
        }

    }

    protected static void RegisterServices(IServiceCollection services)
    {
        // Register a TimeProvider so we don't need to rely on DateTimeOffset.UtcNow, and can mock the time in automated tests
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<ITripService, TripService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IAlbumService, AlbumService>();
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<IScrapboardService, ScrapboardService>();
        services.AddScoped<IOneTimeCodeService, OneTimeCodeService>();
        services.AddScoped<ICollaborationService, CollaborationService>();
        services.AddHttpClient<IGeoService, GeoService>(client =>
        {
            client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Polaroam/0.1 (noreply.outstanding@gmail.com)");
        })
        .AddHttpMessageHandler<NominatimRateLimitingHandler>();
        services.AddHttpClient<IImageTaggingService, ImageTaggingService>(
            client =>
            {
                client.BaseAddress = new Uri("https://apps.csse.canterbury.ac.nz/llm/v1/");
                client.Timeout = TimeSpan.FromSeconds(60);
            }
        );

        // Make sure you know the differences between AddSingleton, AddScoped, and AddTransient.
        // (If in doubt, you probably just want AddScoped
    }

}
