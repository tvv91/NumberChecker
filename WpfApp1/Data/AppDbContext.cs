using Microsoft.EntityFrameworkCore;
using VodafoneLogin.Models;

namespace VodafoneLogin.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<PhoneOffer> PhoneOffers { get; set; }
        public DbSet<SyncState> SyncStates { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure PhoneOffer
            modelBuilder.Entity<PhoneOffer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PhoneNormalized).IsUnique();
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.ValidUntil);
            });

            // Configure SyncState - ensure only one record exists
            modelBuilder.Entity<SyncState>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasData(new SyncState { Id = 1, LastProcessedPhoneId = null });
            });
        }
    }
}

