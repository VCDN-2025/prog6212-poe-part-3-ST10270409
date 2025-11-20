using CMCS.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CMCS.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Claim> Claims { get; set; }
    public DbSet<ClaimDocument> ClaimDocuments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
        });

        // Claim configuration - EF automatically ignores [NotMapped] properties
        modelBuilder.Entity<Claim>(entity =>
        {
            entity.Property(c => c.HourlyRate).HasColumnType("decimal(18,2)");
        });

        // ClaimDocument configuration
        modelBuilder.Entity<ClaimDocument>(entity =>
        {
            entity.HasOne(cd => cd.Claim)
                .WithMany(c => c.Documents)
                .HasForeignKey(cd => cd.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}