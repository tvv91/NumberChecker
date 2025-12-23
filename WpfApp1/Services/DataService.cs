using Microsoft.EntityFrameworkCore;
using VodafoneLogin.Data;
using VodafoneLogin.Models;
using System.Text.RegularExpressions;

namespace VodafoneLogin.Services
{
    public class DataService : IDataService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public DataService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task EnsureDatabaseCreatedAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            await context.Database.EnsureCreatedAsync();
        }

        private string NormalizePhoneNumber(string phoneNumber)
        {
            // Remove all non-digit characters
            return Regex.Replace(phoneNumber, @"\D", "");
        }

        public async Task<int> SavePhoneOfferAsync(string phoneNumber, Offer offer)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            
            string normalized = NormalizePhoneNumber(phoneNumber);
            
            var existingOffer = await context.PhoneOffers
                .FirstOrDefaultAsync(p => p.PhoneNormalized == normalized);

            if (existingOffer != null)
            {
                // Update existing offer
                existingOffer.DiscountPercent = offer.Discount;
                existingOffer.MinTopupAmount = offer.MinTopUp;
                existingOffer.GiftAmount = offer.Gift;
                existingOffer.ActiveDays = offer.ActiveDays;
                existingOffer.ValidUntil = offer.ValidUntil;
                existingOffer.FullText = offer.FullText;
                existingOffer.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                return existingOffer.Id;
            }
            else
            {
                // Create new offer
                var phoneOffer = new PhoneOffer
                {
                    PhoneNumber = phoneNumber,
                    PhoneNormalized = normalized,
                    DiscountPercent = offer.Discount,
                    MinTopupAmount = offer.MinTopUp,
                    GiftAmount = offer.Gift,
                    ActiveDays = offer.ActiveDays,
                    ValidUntil = offer.ValidUntil,
                    FullText = offer.FullText,
                    CreatedAt = DateTime.UtcNow
                };
                
                context.PhoneOffers.Add(phoneOffer);
                await context.SaveChangesAsync();
                return phoneOffer.Id;
            }
        }

        public async Task<int?> GetLastProcessedPhoneIdAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var syncState = await context.SyncStates.FindAsync(1);
            return syncState?.LastProcessedPhoneId;
        }

        public async Task SetLastProcessedPhoneIdAsync(int? phoneId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var syncState = await context.SyncStates.FindAsync(1);
            
            if (syncState == null)
            {
                syncState = new SyncState { Id = 1, LastProcessedPhoneId = phoneId };
                context.SyncStates.Add(syncState);
            }
            else
            {
                syncState.LastProcessedPhoneId = phoneId;
            }

            await context.SaveChangesAsync();
        }

        public async Task<List<PhoneOffer>> GetPhoneOffersAsync(int skip = 0, int take = 50, string? phoneFilter = null, bool? hasDiscount = null, bool? hasGift = null)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            
            var query = context.PhoneOffers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(phoneFilter))
            {
                query = query.Where(p => p.PhoneNumber.Contains(phoneFilter) || p.PhoneNormalized.Contains(phoneFilter));
            }

            if (hasDiscount.HasValue)
            {
                if (hasDiscount.Value)
                    query = query.Where(p => p.DiscountPercent > 0);
                else
                    query = query.Where(p => p.DiscountPercent == 0);
            }

            if (hasGift.HasValue)
            {
                if (hasGift.Value)
                    query = query.Where(p => p.GiftAmount > 0);
                else
                    query = query.Where(p => p.GiftAmount == 0);
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetPhoneOffersCountAsync(string? phoneFilter = null, bool? hasDiscount = null, bool? hasGift = null)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            
            var query = context.PhoneOffers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(phoneFilter))
            {
                query = query.Where(p => p.PhoneNumber.Contains(phoneFilter) || p.PhoneNormalized.Contains(phoneFilter));
            }

            if (hasDiscount.HasValue)
            {
                if (hasDiscount.Value)
                    query = query.Where(p => p.DiscountPercent > 0);
                else
                    query = query.Where(p => p.DiscountPercent == 0);
            }

            if (hasGift.HasValue)
            {
                if (hasGift.Value)
                    query = query.Where(p => p.GiftAmount > 0);
                else
                    query = query.Where(p => p.GiftAmount == 0);
            }

            return await query.CountAsync();
        }
    }
}

