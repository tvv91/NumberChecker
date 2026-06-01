using Microsoft.EntityFrameworkCore;
using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<PhoneOffer> PhoneOffers { get; set; }
        public DbSet<SyncState> SyncStates { get; set; }
        public DbSet<PropositionType> PropositionTypes { get; set; }
        public DbSet<IterationHistory> IterationHistories { get; set; }
        public DbSet<IterationHistoryItem> IterationHistoryItems { get; set; }

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
                entity.Property(e => e.IsPriority).HasDefaultValue(false);
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

            modelBuilder.Entity<IterationHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasMany(e => e.Items)
                    .WithOne(i => i.IterationHistory)
                    .HasForeignKey(i => i.IterationHistoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<IterationHistoryItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.IterationHistoryId);
                entity.HasIndex(e => e.PhoneNumber);
            });
        }
    }
}

