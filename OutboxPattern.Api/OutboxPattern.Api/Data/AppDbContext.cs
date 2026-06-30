using Microsoft.EntityFrameworkCore;
using OutboxPattern.Api.Models;

namespace OutboxPattern.Api.Data;

/// <summary>
/// Single DbContext that owns BOTH the business table (Orders)
/// and the outbox table (OutboxMessages).
///
/// KEY INSIGHT: Because both tables live in the same database, a single
/// SaveChanges() call writes the business record AND the outbox message
/// atomically. If the process crashes right after, the outbox message
/// is already there and will be delivered on restart.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(o => o.Id);
            b.Property(o => o.CustomerEmail).IsRequired().HasMaxLength(200);
            b.Property(o => o.ProductName).IsRequired().HasMaxLength(200);
            b.Property(o => o.Amount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.EventType).IsRequired().HasMaxLength(200);
            b.Property(m => m.Payload).IsRequired();
            // Index on ProcessedAt so the relay worker can efficiently
            // query for unprocessed messages
            b.HasIndex(m => m.ProcessedAt);
        });
    }
}
