using Microsoft.EntityFrameworkCore;
using Storage.Models;

namespace Storage;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<NetworkEvent> NetworkEvents => Set<NetworkEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NetworkEvent>(entity =>
        {
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.SourceIp);
            entity.HasIndex(e => e.Severity);
        });
    }
}
