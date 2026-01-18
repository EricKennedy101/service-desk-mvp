using FRAServiceRequestPortal.Domain.Entities;
using FRAServiceRequestPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FRAServiceRequestPortal.Infrastructure.Persistence;

public class SqlServerAppDbContext : DbContext
{
    public SqlServerAppDbContext(DbContextOptions<SqlServerAppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Case> Cases { get; set; } = default!;
    public DbSet<CaseEvent> CaseEvents { get; set; } = default!;
    public DbSet<CaseEvidence> CaseEvidence { get; set; } = default!;
    public DbSet<Ticket> Tickets { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Case>(entity =>
        {
            entity.ToTable("Cases");
            entity.Property(c => c.Status).HasConversion<string>();
            entity.Property(c => c.Priority).HasConversion<string>();
            entity.Property(c => c.Severity).HasConversion<string>();
        });

        modelBuilder.Entity<CaseEvent>(entity =>
        {
            entity.ToTable("CaseEvents");
            entity.HasOne<Case>()
                .WithMany()
                .HasForeignKey(e => e.CaseId);
        });

        modelBuilder.Entity<CaseEvidence>(entity =>
        {
            entity.ToTable("CaseEvidence");
            entity.HasOne<Case>()
                .WithMany()
                .HasForeignKey(e => e.CaseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Tickets");
            entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Category).HasMaxLength(50).IsRequired();
            entity.Property(t => t.Priority).HasMaxLength(20).IsRequired();
            entity.Property(t => t.Status).HasMaxLength(20).IsRequired();
            entity.Property(t => t.RequesterEmail).HasMaxLength(200).IsRequired();
        });
    }
}
