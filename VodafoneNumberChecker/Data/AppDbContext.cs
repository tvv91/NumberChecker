using Microsoft.EntityFrameworkCore;
using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<PhoneOffer> PhoneOffers { get; set; }
        public DbSet<SyncState> SyncStates { get; set; }
        public DbSet<PropositionType> PropositionTypes { get; set; }

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
                entity.HasIndex(e => e.PhoneNumber).IsUnique();
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.ValidUntil);
            });

            // Configure SyncState - ensure only one record exists
            modelBuilder.Entity<SyncState>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasData(new SyncState { Id = 1, LastProcessedPhoneId = null });
            });

            // Configure PropositionType
            modelBuilder.Entity<PropositionType>(entity =>
            {
                entity.HasKey(e => e.Id);
                // Changed from unique index on Title to allow same title with different content
                entity.HasIndex(e => new { e.Title, e.Content });
            });
        }
    }
}

