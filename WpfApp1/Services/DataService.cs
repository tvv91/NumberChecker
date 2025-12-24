using Microsoft.EntityFrameworkCore;
using VodafoneLogin.Data;
using VodafoneLogin.Models;

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
            
            // Check and add missing columns if needed (for schema updates)
            await EnsureSchemaUpToDateAsync(context);
        }

        private async Task EnsureSchemaUpToDateAsync(AppDbContext context)
        {
            try
            {
                // Check if IsError column exists by querying pragma_table_info
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('PhoneOffers') WHERE name='IsError';";
                var result = await command.ExecuteScalarAsync();
                var count = Convert.ToInt32(result);
                
                if (count == 0)
                {
                    // IsError column doesn't exist, add it
                    command.CommandText = "ALTER TABLE PhoneOffers ADD COLUMN IsError INTEGER NOT NULL DEFAULT 0;";
                    await command.ExecuteNonQueryAsync();
                }
                
                // Check if ErrorDescription column exists
                command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('PhoneOffers') WHERE name='ErrorDescription';";
                result = await command.ExecuteScalarAsync();
                count = Convert.ToInt32(result);
                
                if (count == 0)
                {
                    // ErrorDescription column doesn't exist, add it
                    command.CommandText = "ALTER TABLE PhoneOffers ADD COLUMN ErrorDescription TEXT;";
                    await command.ExecuteNonQueryAsync();
                }
                
                await connection.CloseAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't fail - might be first run or other issue
                System.Diagnostics.Debug.WriteLine($"Schema update error: {ex.Message}");
            }
        }


        public async Task<int> SavePhoneOfferAsync(string phoneNumber, Offer offer)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            
            var existingOffer = await context.PhoneOffers
                .FirstOrDefaultAsync(p => p.PhoneNumber == phoneNumber);

            if (existingOffer != null)
            {
                // Update existing offer
                existingOffer.DiscountPercent = offer.Discount;
                existingOffer.MinTopupAmount = offer.MinTopUp;
                existingOffer.GiftAmount = offer.Gift;
                existingOffer.ActiveDays = offer.ActiveDays;
                existingOffer.ValidUntil = offer.ValidUntil;
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
                    DiscountPercent = offer.Discount,
                    MinTopupAmount = offer.MinTopUp,
                    GiftAmount = offer.Gift,
                    ActiveDays = offer.ActiveDays,
                    ValidUntil = offer.ValidUntil,
                    CreatedAt = DateTime.UtcNow
                };
                
                context.PhoneOffers.Add(phoneOffer);
                await context.SaveChangesAsync();
                return phoneOffer.Id;
            }
        }

        public async Task<int> ImportPhoneNumberAsync(string phoneNumber)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            
            // Check if phone number already exists
            var existingOffer = await context.PhoneOffers
                .FirstOrDefaultAsync(p => p.PhoneNumber == phoneNumber);

            if (existingOffer != null)
            {
                // Already exists, return existing ID
                return existingOffer.Id;
            }

            // Create new PhoneOffer with default values
            var phoneOffer = new PhoneOffer
            {
                PhoneNumber = phoneNumber,
                DiscountPercent = 0,
                MinTopupAmount = 0,
                GiftAmount = 0,
                ActiveDays = 0,
                ValidUntil = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                IsSynchronized = false,
                SyncedAt = null,
                LastSyncAttempt = null,
                SyncAttempts = 0,
                SyncError = null,
                IsProcessed = false
            };
            
            context.PhoneOffers.Add(phoneOffer);
            await context.SaveChangesAsync();
            return phoneOffer.Id;
        }

        public async Task<int> ImportPhoneNumbersAsync(List<string> phoneNumbers)
        {
            // Clear all existing phone offers first
            await ClearAllPhoneOffersAsync();
            
            int importedCount = 0;
            
            foreach (var phoneNumber in phoneNumbers)
            {
                try
                {
                    await ImportPhoneNumberAsync(phoneNumber);
                    importedCount++;
                }
                catch
                {
                    // Skip duplicates or errors, continue with next number
                    continue;
                }
            }
            
            return importedCount;
        }

        public async Task ClearAllPhoneOffersAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var allOffers = await context.PhoneOffers.ToListAsync();
            context.PhoneOffers.RemoveRange(allOffers);
            await context.SaveChangesAsync();
        }

        public async Task<List<PhoneOffer>> GetUnprocessedPhoneOffersAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            
            var lastProcessedId = await GetLastProcessedPhoneIdAsync();
            
            // Get unprocessed offers (including those with errors for retry)
            var query = context.PhoneOffers
                .Where(p => !p.IsProcessed || p.IsError);
            
            if (lastProcessedId.HasValue)
            {
                // Start from the next record after last processed
                query = query.Where(p => p.Id > lastProcessedId.Value);
            }
            
            return await query
                .OrderBy(p => p.Id)
                .ToListAsync();
        }

        public async Task MarkPhoneOfferAsProcessedAsync(int phoneOfferId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var offer = await context.PhoneOffers.FindAsync(phoneOfferId);
            if (offer != null)
            {
                offer.IsProcessed = true;
                offer.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }

        public async Task SetPhoneOfferErrorAsync(int phoneOfferId, string error)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var offer = await context.PhoneOffers.FindAsync(phoneOfferId);
            if (offer != null)
            {
                offer.IsError = true;
                offer.ErrorDescription = error;
                offer.SyncError = error;
                offer.LastSyncAttempt = DateTime.UtcNow;
                offer.SyncAttempts++;
                await context.SaveChangesAsync();
            }
        }

        public async Task ClearPhoneOfferErrorAsync(int phoneOfferId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var offer = await context.PhoneOffers.FindAsync(phoneOfferId);
            if (offer != null)
            {
                offer.IsError = false;
                offer.ErrorDescription = null;
                await context.SaveChangesAsync();
            }
        }

        public async Task ResetScannedAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var allOffers = await context.PhoneOffers.ToListAsync();
            foreach (var offer in allOffers)
            {
                offer.IsProcessed = false;
            }
            
            // Reset LastProcessedPhoneId using the same context
            var syncState = await context.SyncStates.FindAsync(1);
            if (syncState == null)
            {
                syncState = new SyncState { Id = 1, LastProcessedPhoneId = null };
                context.SyncStates.Add(syncState);
            }
            else
            {
                syncState.LastProcessedPhoneId = null;
            }
            
            await context.SaveChangesAsync();
        }

        public async Task ResetAllAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var allOffers = await context.PhoneOffers.ToListAsync();
            foreach (var offer in allOffers)
            {
                offer.DiscountPercent = 0;
                offer.MinTopupAmount = 0;
                offer.GiftAmount = 0;
                offer.ActiveDays = 0;
                offer.ValidUntil = null;
                offer.UpdatedAt = null;
                offer.IsError = false;
                offer.ErrorDescription = null;
                offer.IsSynchronized = false;
                offer.SyncedAt = null;
                offer.LastSyncAttempt = null;
                offer.SyncAttempts = 0;
                offer.SyncError = null;
                offer.IsProcessed = false;
            }
            
            // Reset LastProcessedPhoneId using the same context
            var syncState = await context.SyncStates.FindAsync(1);
            if (syncState == null)
            {
                syncState = new SyncState { Id = 1, LastProcessedPhoneId = null };
                context.SyncStates.Add(syncState);
            }
            else
            {
                syncState.LastProcessedPhoneId = null;
            }
            
            await context.SaveChangesAsync();
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
                query = query.Where(p => p.PhoneNumber.Contains(phoneFilter));
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
                .OrderBy(p => p.Id)
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
                query = query.Where(p => p.PhoneNumber.Contains(phoneFilter));
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

