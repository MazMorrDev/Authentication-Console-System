using Microsoft.EntityFrameworkCore;

namespace AuthenticationConsoleSystem;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Users> Users { get; set; }
    public DbSet<Roles> Roles { get; set; }
    public DbSet<UserRoles> UserRoles { get; set; }
}
