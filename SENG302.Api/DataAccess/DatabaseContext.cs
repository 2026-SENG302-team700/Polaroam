using Microsoft.EntityFrameworkCore;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Responses;

namespace SENG302.Api.DataAccess;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<CustomFile> CustomFiles { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Location> Locations { get; set; }
}