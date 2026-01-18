using FRAServiceRequestPortal.Domain.Entities;
using FRAServiceRequestPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FRAServiceRequestPortal.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Case> Cases => Set<Case>();

    public DbSet<CaseEvent> CaseEvents => Set<CaseEvent>();

    public DbSet<CaseEvidence> CaseEvidence => Set<CaseEvidence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Case>(entity =>
        {
            entity.Property(c => c.Status).HasConversion<string>();
            entity.Property(c => c.Priority).HasConversion<string>();
            entity.Property(c => c.Severity).HasConversion<string>();
        });

        modelBuilder.Entity<CaseEvidence>(entity =>
        {
            entity.HasOne<Case>()
                .WithMany()
                .HasForeignKey(e => e.CaseId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
