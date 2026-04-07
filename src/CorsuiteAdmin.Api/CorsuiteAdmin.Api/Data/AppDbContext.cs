using Microsoft.EntityFrameworkCore;
using CorsuiteAdmin.Api.Models;

namespace CorsuiteAdmin.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<DllModule> Modules { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<DllModule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(20);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.AddedBy).HasMaxLength(100);
        });
    }
}
