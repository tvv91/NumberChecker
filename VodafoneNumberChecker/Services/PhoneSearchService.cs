using System.Text.Json;
using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Services
{
    public class PhoneSearchService : IPhoneSearchService
    {
        private readonly IWebViewService _webViewService;
        private readonly IDataService _dataService;
        private readonly ILoggerService _logger;
        private readonly Random _rand = new();

        public PhoneSearchService(IWebViewService webViewService, IDataService dataService, ILoggerService logger)
        {
            _webViewService = webViewService;
            _dataService = dataService;
            _logger = logger;
        }

        public async Task ProcessPhoneNumberAsync(
            string phoneNumber,
            int index,
            ProcessingConfiguration configuration,
            IProgressReporter? progressReporter = null)
        {
            progressReporter?.ReportCurrentNumber(phoneNumber);

            try
            {
                await InputPhoneNumberAsync(phoneNumber, configuration, progressReporter);
                await ClickSearchButtonAsync(configuration, progressReporter);
                
                var (hasError, offer, isPropositionsNotFound, isPropositionsNotSuitable) = await ProcessSearchResultsAsync(phoneNumber, progressReporter);

                if (hasError)
                {
                    await HandleServerErrorAsync(phoneNumber, index, progressReporter);
                    return;
                }

                if (offer != null)
                {
                    await SaveOfferResultAsync(phoneNumber, offer, progressReporter, isPropositionsNotFound, isPropositionsNotSuitable);
                }
                else
                {
                    // No offer found, but we still need to save the proposition status
                    var emptyOffer = new Offer
                    {
                        Discount = 0,
                        MinTopUp = 0,
                        Gift = 0,
                        ActiveDays = 0,
                        ValidUntil = null
                    };
                    await SaveOfferResultAsync(phoneNumber, emptyOffer, progressReporter, isPropositionsNotFound, isPropositionsNotSuitable);
                }

                await SaveProgressAsync(index, progressReporter);
                await DelayBeforeNextAsync(configuration, progressReporter);
            }
            catch (Exception ex) when (ex.Message == "SERVER_ERROR")
            {
                await HandleServerErrorAsync(phoneNumber, index, progressReporter);
            }
            catch (Exception ex)
            {
                await HandleGeneralErrorAsync(phoneNumber, index, ex, progressReporter);
            }
        }

        private async Task InputPhoneNumberAsync(
            string phoneNumber,
            ProcessingConfiguration configuration,
            IProgressReporter? progressReporter,
            CancellationToken cancellationToken = default)
        {
            await _webViewService.WaitForElementAsync("#phoneNumber");

            int delayInput = GetRandomDelay(configuration.DelayInputMin, configuration.DelayInputMax);
            await DelayWithProgressAsync(delayInput, progressReporter != null ? progress => progressReporter.ReportInputProgress(progress) : null, cancellationToken);

            await _webViewService.ExecuteScriptAsync($@"
            (function(){{
                const input = document.getElementById('phoneNumber');
                if (input) {{
                    input.focus();
                    input.value = '{phoneNumber}';
                    input.dispatchEvent(new InputEvent('input', {{ bubbles: true, cancelable: true, composed: true }}));
                    input.dispatchEvent(new Event('change', {{ bubbles: true }}));
                }}
            }})()
            ");
        }

        private async Task ClickSearchButtonAsync(
            ProcessingConfiguration configuration,
            IProgressReporter? progressReporter,
            CancellationToken cancellationToken = default)
        {
            await _webViewService.WaitForButtonByTextAsync("Пошук");

            int delaySearch = GetRandomDelay(configuration.DelaySearchMin, configuration.DelaySearchMax);
            await DelayWithProgressAsync(delaySearch, progressReporter != null ? progress => progressReporter.ReportSearchProgress(progress) : null, cancellationToken);

            await _webViewService.ExecuteScriptAsync(@"
            (function(){
                const buttons = [...document.querySelectorAll('button')];
                const searchBtn = buttons.find(b => b.innerText.includes('Пошук'));
                if (searchBtn) {
                    searchBtn.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, view: window }));
                }
            })()
            ");
        }

        private async Task<(bool hasError, Offer? offer, bool isPropositionsNotFound, bool isPropositionsNotSuitable)> ProcessSearchResultsAsync(
            string phoneNumber,
            IProgressReporter? progressReporter,
            CancellationToken cancellationToken = default)
        {
            await _webViewService.WaitForOffersLoadedAsync();

            if (await _webViewService.CheckServerErrorToastAsync())
            {
                return (true, null, false, false);
            }

            string rawOffersJson = await _webViewService.GetOffersJsonAsync();
            // rawOffersJson is already a JSON string, need to deserialize it first
            string offersJson = JsonSerializer.Deserialize<string>(rawOffersJson) ?? "{}";
            
            // Try to deserialize as new format first (with OffersResponseDto)
            OffersResponseDto? responseDto = null;
            try
            {
                responseDto = JsonSerializer.Deserialize<OffersResponseDto>(offersJson);
            }
            catch
            {
                // Fallback to old format for backward compatibility
            }

            bool isPropositionsNotFound = false;
            List<Offer>? offers = null;

            if (responseDto != null)
            {
                // New format
                isPropositionsNotFound = responseDto.IsPropositionsNotFound;
                offers = responseDto.Offers?.Select(dto => dto.ToOffer()).ToList();
            }
            else
            {
                // Old format - try to deserialize as array
                var offerDtos = JsonSerializer.Deserialize<List<OfferDto>>(offersJson);
                offers = offerDtos?.Select(dto => dto.ToOffer()).ToList();
            }

            // Extract and save proposition types (excluding gift propositions)
            try
            {
                string rawPropositionTypesJson = await _webViewService.GetPropositionTypesJsonAsync();
                string propositionTypesJson = JsonSerializer.Deserialize<string>(rawPropositionTypesJson) ?? "[]";
                var propositionTypeDtos = JsonSerializer.Deserialize<List<PropositionTypeDto>>(propositionTypesJson);
                
                if (propositionTypeDtos != null && propositionTypeDtos.Count > 0)
                {
                    foreach (var propTypeDto in propositionTypeDtos)
                    {
                        if (!string.IsNullOrWhiteSpace(propTypeDto.Title) && !string.IsNullOrWhiteSpace(propTypeDto.Content))
                        {
                            await _dataService.SavePropositionTypeAsync(propTypeDto.Title, propTypeDto.Content);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the whole process
                _logger.LogError($"Error saving proposition types for phone {phoneNumber}", ex);
            }

            bool isPropositionsNotSuitable = false;

            if (offers != null && offers.Count > 0)
            {
                var offer = offers[0];
                
                // Count offer as found only if at least one of these fields is not zero:
                // ActiveDays, Discount, Gift, MinTopUp
                bool hasValidOffer = offer.ActiveDays != 0 || 
                                     offer.Discount != 0 || 
                                     offer.Gift != 0 || 
                                     offer.MinTopUp != 0;
                
                if (hasValidOffer)
                {
                    progressReporter?.ReportOffersFound(1);
                }
                else
                {
                    // If we have offers but no valid offer, and "not found" was not detected,
                    // it means propositions are not suitable
                    if (!isPropositionsNotFound)
                    {
                        isPropositionsNotSuitable = true;
                    }
                }
                
                return (false, offer, isPropositionsNotFound, isPropositionsNotSuitable);
            }
            else
            {
                // No offers found
                if (isPropositionsNotFound)
                {
                    // Explicitly "not found"
                    return (false, null, true, false);
                }
                else
                {
                    // No offers and no "not found" message - means not suitable
                    isPropositionsNotSuitable = true;
                    return (false, null, false, true);
                }
            }
        }

        private async Task SaveOfferResultAsync(
            string phoneNumber,
            Offer offer,
            IProgressReporter? progressReporter,
            bool isPropositionsNotFound = false,
            bool isPropositionsNotSuitable = false)
        {
            // ReportOffersFound is now called in ProcessSearchResultsAsync when offers are found
            int offerId = await _dataService.SavePhoneOfferAsync(phoneNumber, offer, isPropositionsNotFound, isPropositionsNotSuitable);
            await _dataService.SetLastProcessedPhoneIdAsync(offerId);
        }

        public async Task ProcessPhoneOfferAsync(
            PhoneOffer phoneOffer,
            ProcessingConfiguration configuration,
            IProgressReporter? progressReporter = null,
            CancellationToken cancellationToken = default)
        {
            progressReporter?.ReportCurrentNumber(phoneOffer.PhoneNumber);

            // Increment iteration count at the start of processing
            await _dataService.IncrementIterationCountAsync(phoneOffer.Id);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await InputPhoneNumberAsync(phoneOffer.PhoneNumber, configuration, progressReporter, cancellationToken);
                
                cancellationToken.ThrowIfCancellationRequested();
                await ClickSearchButtonAsync(configuration, progressReporter, cancellationToken);
                
                cancellationToken.ThrowIfCancellationRequested();
                var (hasError, offer, isPropositionsNotFound, isPropositionsNotSuitable) = await ProcessSearchResultsAsync(phoneOffer.PhoneNumber, progressReporter, cancellationToken);

                if (hasError)
                {
                    await HandleServerErrorForPhoneOfferAsync(phoneOffer, progressReporter);
                    return;
                }

                if (offer != null)
                {
                    // Save the offer data (this will update the existing PhoneOffer)
                    // ReportOffersFound is now called in ProcessSearchResultsAsync when offers are found
                    await _dataService.SavePhoneOfferAsync(phoneOffer.PhoneNumber, offer, isPropositionsNotFound, isPropositionsNotSuitable);
                }
                else
                {
                    // No offer found, but we still need to save the proposition status
                    var emptyOffer = new Offer
                    {
                        Discount = 0,
                        MinTopUp = 0,
                        Gift = 0,
                        ActiveDays = 0,
                        ValidUntil = null
                    };
                    await _dataService.SavePhoneOfferAsync(phoneOffer.PhoneNumber, emptyOffer, isPropositionsNotFound, isPropositionsNotSuitable);
                }

                // Clear error if it was set previously (successful processing)
                if (phoneOffer.IsError)
                {
                    await _dataService.ClearPhoneOfferErrorAsync(phoneOffer.Id);
                }

                // Mark as processed on success
                await _dataService.MarkPhoneOfferAsProcessedAsync(phoneOffer.Id);
                await _dataService.SetLastProcessedPhoneIdAsync(phoneOffer.Id);
                progressReporter?.ReportProcessed(1);
                
                cancellationToken.ThrowIfCancellationRequested();
                await DelayBeforeNextAsync(configuration, progressReporter, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Re-throw cancellation to be handled by caller
                throw;
            }
            catch (Exception ex) when (ex.Message == "SERVER_ERROR")
            {
                await HandleServerErrorForPhoneOfferAsync(phoneOffer, progressReporter);
            }
            catch (Exception ex)
            {
                // Set error and continue to next number
                await _dataService.SetPhoneOfferErrorAsync(phoneOffer.Id, ex.Message);
                progressReporter?.ReportProcessed(1);
                await _dataService.SetLastProcessedPhoneIdAsync(phoneOffer.Id);
            }
        }

        private async Task HandleServerErrorForPhoneOfferAsync(
            PhoneOffer phoneOffer,
            IProgressReporter? progressReporter)
        {
            progressReporter?.ReportServerErrors(1);
            await _dataService.SetPhoneOfferErrorAsync(phoneOffer.Id, "Server error");
            progressReporter?.ReportProcessed(1);
            await _dataService.SetLastProcessedPhoneIdAsync(phoneOffer.Id);
            await Task.Delay(5000);
        }

        private async Task HandleServerErrorAsync(
            string phoneNumber,
            int index,
            IProgressReporter? progressReporter)
        {
            progressReporter?.ReportServerErrors(1);
            _logger.LogError($"Server error processing phone number: {phoneNumber}");
            progressReporter?.ReportProcessed(1);
            // Progress is now tracked in database via PhoneOffer records
            await Task.Delay(5000);
        }

        private async Task HandleGeneralErrorAsync(
            string phoneNumber,
            int index,
            Exception ex,
            IProgressReporter? progressReporter)
        {
            _logger.LogError($"Error processing phone number: {phoneNumber}", ex);
            progressReporter?.ReportProcessed(1);
            // Progress is now tracked in database via PhoneOffer records
            await Task.CompletedTask;
        }

        private async Task SaveProgressAsync(
            int index,
            IProgressReporter? progressReporter)
        {
            progressReporter?.ReportProcessed(1);
            // Progress is now tracked in database via PhoneOffer records
            await Task.CompletedTask;
        }

        private async Task DelayBeforeNextAsync(
            ProcessingConfiguration configuration,
            IProgressReporter? progressReporter,
            CancellationToken cancellationToken = default)
        {
            int delayNext = GetRandomDelay(configuration.DelayNextMin, configuration.DelayNextMax);
            await DelayWithProgressAsync(delayNext, progressReporter != null ? progress => progressReporter.ReportNextProgress(progress) : null, cancellationToken);
        }

        private async Task DelayWithProgressAsync(int milliseconds, Action<double>? progressCallback, CancellationToken cancellationToken = default)
        {
            if (progressCallback == null)
            {
                await Task.Delay(milliseconds, cancellationToken);
                return;
            }

            int interval = 50;
            int steps = milliseconds / interval;
            for (int i = 0; i <= steps; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progressCallback((i * 100.0) / steps);
                await Task.Delay(interval, cancellationToken);
            }
            progressCallback(0);
        }

        public int GetRandomDelay(double minSeconds, double maxSeconds)
        {
            if (minSeconds > maxSeconds)
            {
                var temp = minSeconds;
                minSeconds = maxSeconds;
                maxSeconds = temp;
            }
            return _rand.Next((int)(minSeconds * 1000), (int)(maxSeconds * 1000) + 1);
        }
    }
}

