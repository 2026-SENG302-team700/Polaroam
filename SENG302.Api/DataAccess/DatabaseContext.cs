using Microsoft.EntityFrameworkCore;
using SENG302.Api.Models.Entities;

namespace SENG302.Api.DataAccess;

public interface IDatabaseContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<TaskList> TaskLists { get; set; }
    
    public DbSet<CustomFile> CustomFiles { get; set; }
    public DbSet<TaskItem> TaskItems { get; set; }

    /// <summary>
    /// Checks to see if the provided e-mail is already in the database
    /// </summary>
    /// <param name="email">String representation of the e-mail</param>
    /// <returns>
    /// True: e-mail is already stored and associated with an account
    /// False: e-mail is not stored and associated with an account
    /// </returns>
    public bool EmailExists(string email);
}

public class DatabaseContext : DbContext, IDatabaseContext
{
    public DatabaseContext(DbContextOptions options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<TaskList> TaskLists { get; set; }
    
    public DbSet<CustomFile> CustomFiles { get; set; }
    public DbSet<TaskItem> TaskItems { get; set; }

    /// <inheritdoc/>
    public bool EmailExists(string email)
    {
        return Users.Where((t) => t.Email.ToLower().Equals(email.ToLower())).Count() > 0;
    }
}