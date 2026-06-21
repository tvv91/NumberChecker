using MySqlConnector;
using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Services
{
    public class TopUpSyncService(
        string connectionString,
        IDataService dataService,
        ILoggerService logger) : ITopUpSyncService
    {
        private const int MaxAttempts = 3;
        private const int RetryDelayMs = 1000;

        private readonly string _connectionString = connectionString;
        private readonly IDataService _dataService = dataService;
        private readonly ILoggerService _logger = logger;

        public async Task TrySyncAsync(
            PhoneOffer offer,
            ProcessingConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            if (!configuration.ShouldTopUpNumbers)
            {
                return;
            }

            if (!TopUpRuleMatcher.MatchesAnyRule(offer, configuration.TopUpRules))
            {
                return;
            }

            Exception? lastException = null;

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await SyncToExternalDatabaseAsync(offer, cancellationToken);
                    await _dataService.SetTopUpSyncSuccessAsync(offer.Id);
                    return;
                }
                catch (TopUpSyncDuplicateException)
                {
                    await _dataService.SetTopUpSyncSuccessAsync(offer.Id);
                    return;
                }
                catch (Exception ex) when (attempt < MaxAttempts && IsTransientError(ex))
                {
                    lastException = ex;
                    _logger.LogWarning(
                        $"Top-up sync attempt {attempt}/{MaxAttempts} failed for {offer.PhoneNumber}: {ex.Message}");
                    await Task.Delay(RetryDelayMs * attempt, cancellationToken);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    break;
                }
            }

            var errorMessage = lastException?.Message ?? "Неизвестная ошибка синхронизации пополнения";
            _logger.LogError($"Top-up sync failed for {offer.PhoneNumber}", lastException);
            await _dataService.SetTopUpSyncFailureAsync(offer.Id, errorMessage);
        }

        private async Task SyncToExternalDatabaseAsync(PhoneOffer offer, CancellationToken cancellationToken)
        {
            var normalizedNumber = NormalizePhoneNumberForLookup(offer.PhoneNumber);
            if (string.IsNullOrWhiteSpace(normalizedNumber))
            {
                throw new InvalidOperationException("Некорректный формат номера телефона");
            }

            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            int? simDataId = await GetSimDataIdAsync(connection, normalizedNumber, cancellationToken);
            if (simDataId == null)
            {
                throw new InvalidOperationException("Номер не найден в SimDatas");
            }

            if (await IsAlreadyInReplenishmentQueueAsync(connection, simDataId.Value, cancellationToken))
            {
                return;
            }

            var amount = Math.Max(1, (int)Math.Round(offer.MinTopupAmount));
            await InsertReplenishmentRequestAsync(connection, simDataId.Value, amount, cancellationToken);
        }

        private static async Task<int?> GetSimDataIdAsync(
            MySqlConnection connection,
            string normalizedNumber,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT Id FROM SimDatas WHERE Number = @number LIMIT 1;";
            command.Parameters.AddWithValue("@number", normalizedNumber);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result == null || result == DBNull.Value)
            {
                return null;
            }

            return Convert.ToInt32(result);
        }

        private static async Task<bool> IsAlreadyInReplenishmentQueueAsync(
            MySqlConnection connection,
            int simDataId,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT COUNT(*)
                FROM ReplenishmentRequests
                WHERE SimDataId = @simDataId AND ExecutionDate IS NULL;
                """;
            command.Parameters.AddWithValue("@simDataId", simDataId);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            return count > 0;
        }

        private static async Task InsertReplenishmentRequestAsync(
            MySqlConnection connection,
            int simDataId,
            int amount,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO ReplenishmentRequests
                    (SimDataId, Status, Bank, Provider, Amount, AddingDate)
                VALUES
                    (@simDataId, 0, 0, NULL, @amount, @addingDate);
                """;
            command.Parameters.AddWithValue("@simDataId", simDataId);
            command.Parameters.AddWithValue("@amount", amount);
            command.Parameters.AddWithValue("@addingDate", DateTime.Now);

            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (MySqlException ex) when (IsDuplicateKeyError(ex))
            {
                throw new TopUpSyncDuplicateException(ex.Message, ex);
            }
        }

        private static bool IsDuplicateKeyError(MySqlException ex) =>
            ex.Number is 1062 or 1586;

        private static bool IsTransientError(Exception ex) =>
            ex is MySqlException mysqlEx &&
            mysqlEx.Number is 2006 or 2013 or 1205 or 1213;

        private static string NormalizePhoneNumberForLookup(string value)
        {
            var digitsOnly = new string(value.Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(digitsOnly))
            {
                return string.Empty;
            }

            if (digitsOnly.Length == 10 && digitsOnly.StartsWith('0'))
            {
                return $"38{digitsOnly}";
            }

            if (digitsOnly.Length == 9)
            {
                return $"380{digitsOnly}";
            }

            return digitsOnly;
        }

        private sealed class TopUpSyncDuplicateException(string message, Exception innerException)
            : Exception(message, innerException);
    }
}
