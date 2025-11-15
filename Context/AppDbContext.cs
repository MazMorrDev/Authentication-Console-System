using Microsoft.EntityFrameworkCore;

namespace AuthenticationConsoleSystem;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
}
