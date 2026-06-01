using Microsoft.EntityFrameworkCore;
using VodafoneNumberChecker.Data;
using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Services
{
    public class DataService(IDbContextFactory<AppDbContext> dbContextFactory) : IDataService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

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
                
                // Check if IterationCount column exists
                command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('PhoneOffers') WHERE name='IterationCount';";
                result = await command.ExecuteScalarAsync();
                count = Convert.ToInt32(result);
                
                if (count == 0)
                {
                    // IterationCount column doesn't exist, add it
                    command.CommandText = "ALTER TABLE PhoneOffers ADD COLUMN IterationCount INTEGER NOT NULL DEFAULT 0;";
                    await command.ExecuteNonQueryAsync();
                }
                
                // Check if IsPropositionsNotFound column exists
                command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('PhoneOffers') WHERE name='IsPropositionsNotFound';";
                result = await command.ExecuteScalarAsync();
                count = Convert.ToInt32(result);
                
                if (count == 0)
                {
                    // IsPropositionsNotFound column doesn't exist, add it
                    command.CommandText = "ALTER TABLE PhoneOffers ADD COLUMN IsPropositionsNotFound INTEGER NOT NULL DEFAULT 0;";
                    await command.ExecuteNonQueryAsync();
                }
                
                // Check if IsPropositionsNotSuitable column exists
                command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('PhoneOffers') WHERE name='IsPropositionsNotSuitable';";
                result = await command.ExecuteScalarAsync();
                count = Convert.ToInt32(result);
                
                if (count == 0)
                {
                    // IsPropositionsNotSuitable column doesn't exist, add it
                    command.CommandText = "ALTER TABLE PhoneOffers ADD COLUMN IsPropositionsNotSuitable INTEGER NOT NULL DEFAULT 0;";
                    await command.ExecuteNonQueryAsync();
                }

                // Check if IsPriority column exists
                command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('PhoneOffers') WHERE name='IsPriority';";
                result = await command.ExecuteScalarAsync();
                count = Convert.ToInt32(result);

                if (count == 0)
                {
                    // IsPriority defaults to 0 for existing and newly imported non-priority numbers
                    command.CommandText = "ALTER TABLE PhoneOffers ADD COLUMN IsPriority INTEGER NOT NULL DEFAULT 0;";
                    await command.ExecuteNonQueryAsync();
                }
                
                // Drop CreatedAt and UpdatedAt columns from PropositionTypes if they exist
                // SQLite 3.35.0+ supports DROP COLUMN, but we'll check version first
                try
                {
                    // Check SQLite version
                    command.CommandText = "SELECT sqlite_version();";
                    var versionResult = await command.ExecuteScalarAsync();
                    if (versionResult != null)
                    {
                        var version = versionResult.ToString();
                        // SQLite 3.35.0+ supports DROP COLUMN
                        if (version.CompareTo("3.35.0") >= 0)
                        {
                            // Check if CreatedAt column exists in PropositionTypes
                            command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('PropositionTypes') WHERE name='CreatedAt';";
                            result = await command.ExecuteScalarAsync();
                            count = Convert.ToInt32(result);
                            
                            if (count > 0)
                            {
                                command.CommandText = "ALTER TABLE PropositionTypes DROP COLUMN CreatedAt;";
                                await command.ExecuteNonQueryAsync();
                            }
                            
                            // Check if UpdatedAt column exists in PropositionTypes
                            command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('PropositionTypes') WHERE name='UpdatedAt';";
                            result = await command.ExecuteScalarAsync();
                            count = Convert.ToInt32(result);
                            
                            if (count > 0)
                            {
                                command.CommandText = "ALTER TABLE PropositionTypes DROP COLUMN UpdatedAt;";
                                await command.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
                catch
                {
                    // If DROP COLUMN fails (old SQLite version), columns will remain but won't be used
                    // This is acceptable - the application will work fine without them
                }

                // Create iteration reports tables if they don't exist yet
                command.CommandText =
                    @"CREATE TABLE IF NOT EXISTS IterationHistories (
                        Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        IterationNumber INTEGER NOT NULL,
                        IterationLabel TEXT NOT NULL,
                        StartedAt TEXT NOT NULL,
                        CompletedAt TEXT NOT NULL,
                        PlannedCount INTEGER NOT NULL,
                        ProcessedCount INTEGER NOT NULL,
                        FoundCount INTEGER NOT NULL,
                        ErrorCount INTEGER NOT NULL,
                        NotFoundCount INTEGER NOT NULL,
                        NotSuitableCount INTEGER NOT NULL,
                        NoOfferCount INTEGER NOT NULL,
                        CreatedAt TEXT NOT NULL
                    );";
                await command.ExecuteNonQueryAsync();

                command.CommandText =
                    @"CREATE TABLE IF NOT EXISTS IterationHistoryItems (
                        Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        IterationHistoryId INTEGER NOT NULL,
                        PhoneNumber TEXT NOT NULL,
                        Outcome TEXT NOT NULL,
                        DiscountPercent INTEGER NOT NULL,
                        GiftAmount TEXT NOT NULL,
                        MinTopupAmount TEXT NOT NULL,
                        IsError INTEGER NOT NULL,
                        ErrorDescription TEXT NULL,
                        FOREIGN KEY (IterationHistoryId) REFERENCES IterationHistories (Id) ON DELETE CASCADE
                    );";
                await command.ExecuteNonQueryAsync();
                
                await connection.CloseAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't fail - might be first run or other issue
                System.Diagnostics.Debug.WriteLine($"Schema update error: {ex.Message}");
            }
        }


        public async Task<int> SavePhoneOfferAsync(string phoneNumber, Offer offer, bool isPropositionsNotFound = false, bool isPropositionsNotSuitable = false)
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
                existingOffer.IsPropositionsNotFound = isPropositionsNotFound;
                existingOffer.IsPropositionsNotSuitable = isPropositionsNotSuitable;
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
                    IsPropositionsNotFound = isPropositionsNotFound,
                    IsPropositionsNotSuitable = isPropositionsNotSuitable,
                    CreatedAt = DateTime.UtcNow
                };
                
                context.PhoneOffers.Add(phoneOffer);
                await context.SaveChangesAsync();
                return phoneOffer.Id;
            }
        }

        public async Task<int> ImportPhoneNumberAsync(string phoneNumber, bool isPriority = false)
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
                IsProcessed = false,
                IsPriority = isPriority
            };
            
            context.PhoneOffers.Add(phoneOffer);
            await context.SaveChangesAsync();
            return phoneOffer.Id;
        }

        public async Task<int> ImportPhoneNumbersAsync(List<string> phoneNumbers, bool isPriority = false, bool addToExisting = false, Action<int, int, string?>? progressCallback = null)
        {
            // Replace mode clears existing numbers. Append mode keeps existing records.
            if (!addToExisting)
            {
                await ClearAllPhoneOffersAsync();
            }
            
            int importedCount = 0;
            int total = phoneNumbers.Count;
            
            for (int i = 0; i < phoneNumbers.Count; i++)
            {
                var phoneNumber = phoneNumbers[i];
                try
                {
                    await ImportPhoneNumberAsync(phoneNumber, isPriority);
                    importedCount++;
                }
                catch
                {
                    // Skip duplicates or errors, continue with next number
                }
                finally
                {
                    // Report progress (including current number being processed)
                    progressCallback?.Invoke(importedCount, total, phoneNumber);
                    
                    // Small delay to allow UI to update
                    if (i % 10 == 0 || i == phoneNumbers.Count - 1)
                    {
                        await Task.Delay(1);
                    }
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

        public async Task<PhoneOffer?> GetPhoneOfferByIdAsync(int phoneOfferId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            return await context.PhoneOffers.FirstOrDefaultAsync(p => p.Id == phoneOfferId);
        }

        public async Task<List<PhoneOffer>> GetEmptyPropositionsAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            
            // Get processed offers where propositions were not found (IsPropositionsNotFound = 1)
            return await context.PhoneOffers
                .Where(p => p.IsProcessed && 
                           p.DiscountPercent == 0 && 
                           p.GiftAmount == 0 && 
                           !p.IsError &&
                           p.IsPropositionsNotFound)
                .OrderBy(p => p.Id)
                .ToListAsync();
        }

        public async Task<List<PhoneOffer>> GetErrorNumbersAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            
            // Get offers with errors
            return await context.PhoneOffers
                .Where(p => p.IsError)
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

        public async Task IncrementIterationCountAsync(int phoneOfferId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var offer = await context.PhoneOffers.FindAsync(phoneOfferId);
            if (offer != null)
            {
                offer.IterationCount++;
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
                offer.IsProcessed = false;
                offer.IterationCount = 0;
                offer.IsPropositionsNotFound = false;
                offer.IsPropositionsNotSuitable = false;
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

        public async Task<List<PhoneOffer>> GetPhoneOffersAsync(int skip = 0, int take = 50, string? phoneFilter = null, bool? hasDiscount = null, bool? hasGift = null, bool? isEmptyProposition = null, bool? hasError = null, bool? isPropositionsNotFound = null, bool? isPropositionsNotSuitable = null, bool? isPriority = null)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            
            var query = context.PhoneOffers.AsQueryable();

            // Phone filter (text search) - always applied with AND
            if (!string.IsNullOrWhiteSpace(phoneFilter))
            {
                query = query.Where(p => p.PhoneNumber.Contains(phoneFilter));
            }

            if (isPriority.HasValue)
            {
                query = query.Where(p => p.IsPriority == isPriority.Value);
            }

            // Build OR conditions for all checked positive filters
            bool hasAnyPositiveFilter = false;
            bool hasDiscountFilter = hasDiscount.HasValue && hasDiscount.Value;
            bool hasGiftFilter = hasGift.HasValue && hasGift.Value;
            bool hasErrorFilter = hasError.HasValue && hasError.Value;
            bool hasNotFoundFilter = isPropositionsNotFound.HasValue && isPropositionsNotFound.Value;
            bool hasNotSuitableFilter = isPropositionsNotSuitable.HasValue && isPropositionsNotSuitable.Value;

            if (hasDiscountFilter || hasGiftFilter || hasErrorFilter || hasNotFoundFilter || hasNotSuitableFilter)
            {
                hasAnyPositiveFilter = true;
                // Combine all positive filters with OR logic
                query = query.Where(p => 
                    (hasDiscountFilter && p.DiscountPercent > 0) ||
                    (hasGiftFilter && p.GiftAmount > 0) ||
                    (hasErrorFilter && p.IsError) ||
                    (hasNotFoundFilter && p.IsPropositionsNotFound) ||
                    (hasNotSuitableFilter && p.IsPropositionsNotSuitable));
            }

            // Apply negative filters only if no positive filters are set
            if (!hasAnyPositiveFilter)
            {
                if (hasDiscount.HasValue && !hasDiscount.Value)
                {
                    query = query.Where(p => p.DiscountPercent == 0);
                }

                if (hasGift.HasValue && !hasGift.Value)
                {
                    query = query.Where(p => p.GiftAmount == 0);
                }

                if (hasError.HasValue && !hasError.Value)
                {
                    query = query.Where(p => !p.IsError);
                }

                if (isPropositionsNotFound.HasValue && !isPropositionsNotFound.Value)
                {
                    query = query.Where(p => !p.IsPropositionsNotFound);
                }

                if (isPropositionsNotSuitable.HasValue && !isPropositionsNotSuitable.Value)
                {
                    query = query.Where(p => !p.IsPropositionsNotSuitable);
                }
            }

            return await query
                .OrderBy(p => p.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetPhoneOffersCountAsync(string? phoneFilter = null, bool? hasDiscount = null, bool? hasGift = null, bool? isEmptyProposition = null, bool? hasError = null, bool? isPropositionsNotFound = null, bool? isPropositionsNotSuitable = null, bool? isPriority = null)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            
            var query = context.PhoneOffers.AsQueryable();

            // Phone filter (text search) - always applied with AND
            if (!string.IsNullOrWhiteSpace(phoneFilter))
            {
                query = query.Where(p => p.PhoneNumber.Contains(phoneFilter));
            }

            if (isPriority.HasValue)
            {
                query = query.Where(p => p.IsPriority == isPriority.Value);
            }

            // Build OR conditions for all checked positive filters
            bool hasAnyPositiveFilter = false;
            bool hasDiscountFilter = hasDiscount.HasValue && hasDiscount.Value;
            bool hasGiftFilter = hasGift.HasValue && hasGift.Value;
            bool hasErrorFilter = hasError.HasValue && hasError.Value;
            bool hasNotFoundFilter = isPropositionsNotFound.HasValue && isPropositionsNotFound.Value;
            bool hasNotSuitableFilter = isPropositionsNotSuitable.HasValue && isPropositionsNotSuitable.Value;

            if (hasDiscountFilter || hasGiftFilter || hasErrorFilter || hasNotFoundFilter || hasNotSuitableFilter)
            {
                hasAnyPositiveFilter = true;
                // Combine all positive filters with OR logic
                query = query.Where(p => 
                    (hasDiscountFilter && p.DiscountPercent > 0) ||
                    (hasGiftFilter && p.GiftAmount > 0) ||
                    (hasErrorFilter && p.IsError) ||
                    (hasNotFoundFilter && p.IsPropositionsNotFound) ||
                    (hasNotSuitableFilter && p.IsPropositionsNotSuitable));
            }

            // Apply negative filters only if no positive filters are set
            if (!hasAnyPositiveFilter)
            {
                if (hasDiscount.HasValue && !hasDiscount.Value)
                {
                    query = query.Where(p => p.DiscountPercent == 0);
                }

                if (hasGift.HasValue && !hasGift.Value)
                {
                    query = query.Where(p => p.GiftAmount == 0);
                }

                if (hasError.HasValue && !hasError.Value)
                {
                    query = query.Where(p => !p.IsError);
                }

                if (isPropositionsNotFound.HasValue && !isPropositionsNotFound.Value)
                {
                    query = query.Where(p => !p.IsPropositionsNotFound);
                }

                if (isPropositionsNotSuitable.HasValue && !isPropositionsNotSuitable.Value)
                {
                    query = query.Where(p => !p.IsPropositionsNotSuitable);
                }
            }

            return await query.CountAsync();
        }

        public async Task SavePropositionTypeAsync(string title, string content)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            
            // Find existing type by both Title AND Content
            var existingType = await context.PropositionTypes
                .FirstOrDefaultAsync(p => p.Title == title && p.Content == content);

            if (existingType != null)
            {
                // Exact match: same title AND same content - just increment count
                existingType.Count++;
                await context.SaveChangesAsync();
            }
            else
            {
                // No exact match: either different title OR different content - create new entry
                var propositionType = new PropositionType
                {
                    Title = title,
                    Content = content,
                    Count = 1
                };
                
                context.PropositionTypes.Add(propositionType);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<PropositionType>> GetPropositionTypesAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            
            return await context.PropositionTypes
                .OrderByDescending(p => p.Count)
                .ThenBy(p => p.Title)
                .ToListAsync();
        }

        public async Task<List<PhoneOffer>> GetAllPhoneOffersForExportAsync(string? phoneFilter = null, bool? hasDiscount = null, bool? hasGift = null, bool? isEmptyProposition = null, bool? hasError = null, bool? isPropositionsNotFound = null, bool? isPropositionsNotSuitable = null, bool? isPriority = null)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            
            var query = context.PhoneOffers.AsQueryable();

            // Phone filter (text search) - always applied with AND
            if (!string.IsNullOrWhiteSpace(phoneFilter))
            {
                query = query.Where(p => p.PhoneNumber.Contains(phoneFilter));
            }

            if (isPriority.HasValue)
            {
                query = query.Where(p => p.IsPriority == isPriority.Value);
            }

            // Build OR conditions for all checked positive filters
            bool hasAnyPositiveFilter = false;
            bool hasDiscountFilter = hasDiscount.HasValue && hasDiscount.Value;
            bool hasGiftFilter = hasGift.HasValue && hasGift.Value;
            bool hasErrorFilter = hasError.HasValue && hasError.Value;
            bool hasNotFoundFilter = isPropositionsNotFound.HasValue && isPropositionsNotFound.Value;
            bool hasNotSuitableFilter = isPropositionsNotSuitable.HasValue && isPropositionsNotSuitable.Value;

            if (hasDiscountFilter || hasGiftFilter || hasErrorFilter || hasNotFoundFilter || hasNotSuitableFilter)
            {
                hasAnyPositiveFilter = true;
                // Combine all positive filters with OR logic
                query = query.Where(p => 
                    (hasDiscountFilter && p.DiscountPercent > 0) ||
                    (hasGiftFilter && p.GiftAmount > 0) ||
                    (hasErrorFilter && p.IsError) ||
                    (hasNotFoundFilter && p.IsPropositionsNotFound) ||
                    (hasNotSuitableFilter && p.IsPropositionsNotSuitable));
            }

            // Apply negative filters only if no positive filters are set
            if (!hasAnyPositiveFilter)
            {
                if (hasDiscount.HasValue && !hasDiscount.Value)
                {
                    query = query.Where(p => p.DiscountPercent == 0);
                }

                if (hasGift.HasValue && !hasGift.Value)
                {
                    query = query.Where(p => p.GiftAmount == 0);
                }

                if (hasError.HasValue && !hasError.Value)
                {
                    query = query.Where(p => !p.IsError);
                }

                if (isPropositionsNotFound.HasValue && !isPropositionsNotFound.Value)
                {
                    query = query.Where(p => !p.IsPropositionsNotFound);
                }

                if (isPropositionsNotSuitable.HasValue && !isPropositionsNotSuitable.Value)
                {
                    query = query.Where(p => !p.IsPropositionsNotSuitable);
                }
            }

            return await query
                .OrderBy(p => p.Id)
                .ToListAsync();
        }

        public async Task<int> GetDiscountCountAsync(string? phoneFilter = null, bool? isPriority = null)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var query = context.PhoneOffers.AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(phoneFilter))
            {
                query = query.Where(p => p.PhoneNumber.Contains(phoneFilter));
            }

            if (isPriority.HasValue)
            {
                query = query.Where(p => p.IsPriority == isPriority.Value);
            }
            
            return await query.Where(p => p.DiscountPercent > 0).CountAsync();
        }

        public async Task<int> GetGiftCountAsync(string? phoneFilter = null, bool? isPriority = null)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var query = context.PhoneOffers.AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(phoneFilter))
            {
                query = query.Where(p => p.PhoneNumber.Contains(phoneFilter));
            }

            if (isPriority.HasValue)
            {
                query = query.Where(p => p.IsPriority == isPriority.Value);
            }
            
            return await query.Where(p => p.GiftAmount > 0).CountAsync();
        }

        public async Task<int> GetErrorCountAsync(string? phoneFilter = null, bool? isPriority = null)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var query = context.PhoneOffers.AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(phoneFilter))
            {
                query = query.Where(p => p.PhoneNumber.Contains(phoneFilter));
            }

            if (isPriority.HasValue)
            {
                query = query.Where(p => p.IsPriority == isPriority.Value);
            }
            
            return await query.Where(p => p.IsError).CountAsync();
        }

        public async Task<int> GetNotFoundCountAsync(string? phoneFilter = null, bool? isPriority = null)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var query = context.PhoneOffers.AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(phoneFilter))
            {
                query = query.Where(p => p.PhoneNumber.Contains(phoneFilter));
            }

            if (isPriority.HasValue)
            {
                query = query.Where(p => p.IsPriority == isPriority.Value);
            }
            
            return await query.Where(p => p.IsPropositionsNotFound).CountAsync();
        }

        public async Task<int> GetNotSuitableCountAsync(string? phoneFilter = null, bool? isPriority = null)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var query = context.PhoneOffers.AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(phoneFilter))
            {
                query = query.Where(p => p.PhoneNumber.Contains(phoneFilter));
            }

            if (isPriority.HasValue)
            {
                query = query.Where(p => p.IsPriority == isPriority.Value);
            }
            
            return await query.Where(p => p.IsPropositionsNotSuitable).CountAsync();
        }

        public async Task<IterationReport> SaveIterationReportAsync(IterationReport report)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var entity = new IterationHistory
            {
                IterationNumber = report.IterationNumber,
                IterationLabel = report.IterationLabel,
                StartedAt = report.StartedAt,
                CompletedAt = report.CompletedAt,
                PlannedCount = report.PlannedCount,
                ProcessedCount = report.ProcessedCount,
                FoundCount = report.FoundCount,
                ErrorCount = report.ErrorCount,
                NotFoundCount = report.NotFoundCount,
                NotSuitableCount = report.NotSuitableCount,
                NoOfferCount = report.NoOfferCount,
                CreatedAt = DateTime.UtcNow,
                Items = report.Items.Select(i => new IterationHistoryItem
                {
                    PhoneNumber = i.PhoneNumber,
                    Outcome = i.Outcome,
                    DiscountPercent = i.DiscountPercent,
                    GiftAmount = i.GiftAmount,
                    MinTopupAmount = i.MinTopupAmount,
                    IsError = i.IsError,
                    ErrorDescription = i.ErrorDescription
                }).ToList()
            };

            context.IterationHistories.Add(entity);
            await context.SaveChangesAsync();
            report.Id = entity.Id;
            return report;
        }

        public async Task<List<IterationReport>> GetIterationReportsAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var reports = await context.IterationHistories
                .Include(r => r.Items)
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            return reports.Select(r => new IterationReport
            {
                Id = r.Id,
                IterationNumber = r.IterationNumber,
                IterationLabel = r.IterationLabel,
                StartedAt = r.StartedAt,
                CompletedAt = r.CompletedAt,
                PlannedCount = r.PlannedCount,
                ProcessedCount = r.ProcessedCount,
                FoundCount = r.FoundCount,
                ErrorCount = r.ErrorCount,
                NotFoundCount = r.NotFoundCount,
                NotSuitableCount = r.NotSuitableCount,
                NoOfferCount = r.NoOfferCount,
                Items = new System.Collections.ObjectModel.ObservableCollection<IterationItemReport>(
                    r.Items.Select(i => new IterationItemReport
                    {
                        PhoneNumber = i.PhoneNumber,
                        Outcome = i.Outcome,
                        DiscountPercent = i.DiscountPercent,
                        GiftAmount = i.GiftAmount,
                        MinTopupAmount = i.MinTopupAmount,
                        IsError = i.IsError,
                        ErrorDescription = i.ErrorDescription
                    }))
            }).ToList();
        }

        public async Task ClearIterationReportsAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            await context.Database.ExecuteSqlRawAsync("DELETE FROM IterationHistoryItems;");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM IterationHistories;");
        }
    }
}

