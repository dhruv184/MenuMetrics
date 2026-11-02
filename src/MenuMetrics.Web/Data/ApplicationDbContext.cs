using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MenuMetrics.Web.Models.Entities;

namespace MenuMetrics.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts) : base(opts) {}
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<Forecast> Forecasts => Set<Forecast>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    
    public DbSet<ImportRun> ImportRuns => Set<ImportRun>();
    
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<MenuItem>().Ignore(m => m.Margin);
        b.Entity<MenuItem>().Property(m => m.Price).HasPrecision(10, 2);
        b.Entity<MenuItem>().Property(m => m.Cost).HasPrecision(10, 2);
        b.Entity<Transaction>().Property(t => t.LineTotal).HasPrecision(12, 2);
        b.Entity<Transaction>().HasIndex(t => new { t.BusinessId, t.OccurredAt });
        b.Entity<Promotion>().Property(p => p.DiscountPct).HasPrecision(5, 2);
        b.Entity<Promotion>().Property(p => p.PreRevenue).HasPrecision(12, 2);
        b.Entity<Promotion>().Property(p => p.DuringRevenue).HasPrecision(12, 2);
        b.Entity<Promotion>().Property(p => p.PostRevenue).HasPrecision(12, 2);
        b.Entity<Promotion>().Property(p => p.Roi).HasPrecision(8, 2);
        b.Entity<Forecast>().Property(f => f.PredictedOrders).HasPrecision(12, 2);
        b.Entity<Forecast>().Property(f => f.BaseDemand).HasPrecision(12, 2);
        b.Entity<Forecast>().Property(f => f.SeasonalityFactor).HasPrecision(8, 4);
        b.Entity<Forecast>().Property(f => f.TrendFactor).HasPrecision(8, 4);
        b.Entity<Forecast>().Property(f => f.Lower95).HasPrecision(12, 2);
        b.Entity<Forecast>().Property(f => f.Upper95).HasPrecision(12, 2);
        b.Entity<Recommendation>().Property(r => r.ImpactScore).HasPrecision(5, 2);
        b.Entity<Recommendation>().Property(r => r.EstimatedRevenueLift).HasPrecision(12, 2);
        b.Entity<InventoryItem>().Property(i => i.OnHand).HasPrecision(12, 3);
        b.Entity<InventoryItem>().Property(i => i.ReorderPoint).HasPrecision(12, 3);
        b.Entity<ImportRun>().HasIndex(r => new { r.BusinessId, r.CreatedAtUtc });
        b.Entity<ImportRun>().HasIndex(r => new { r.BusinessId, r.UserId });
        b.Entity<Transaction>()
            .HasOne(t => t.ImportRun)
            .WithMany(r => r.Transactions)
            .HasForeignKey(t => t.ImportRunId)
            .OnDelete(DeleteBehavior.SetNull);
        b.Entity<Transaction>().HasIndex(t => t.ImportRunId);
        b.Entity<ImportRun>()
            .HasOne(r => r.Location)
            .WithMany()
            .HasForeignKey(r => r.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
