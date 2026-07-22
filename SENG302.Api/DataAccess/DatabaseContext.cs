using Microsoft.EntityFrameworkCore;
using SENG302.Api.Models.Entities;

namespace SENG302.Api.DataAccess;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<CustomFile> CustomFiles { get; set; }
    public DbSet<Post> Posts { get; set; }
}