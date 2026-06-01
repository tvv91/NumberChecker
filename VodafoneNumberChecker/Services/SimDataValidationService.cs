using MySqlConnector;
using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Services
{
    public class SimDataValidationService(string connectionString) : ISimDataValidationService
    {
        private readonly string _connectionString = connectionString;
        private const int BatchSize = 500;

        public async Task<SimDataValidationResult> ValidateNumbersExistAsync(
            IReadOnlyCollection<string> numbers,
            Action<int, int, string?>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            var preparedNumbers = numbers
                .Select(n => n?.Trim() ?? string.Empty)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(original => new
                {
                    Original = original,
                    Normalized = NormalizePhoneNumberForLookup(original)
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Normalized))
                .ToList();

            if (preparedNumbers.Count == 0)
            {
                return new SimDataValidationResult();
            }

            var normalizedNumbers = preparedNumbers
                .Select(x => x.Normalized)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var existingNumbers = new HashSet<string>(StringComparer.Ordinal);
            int processed = 0;

            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            for (int offset = 0; offset < normalizedNumbers.Count; offset += BatchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = normalizedNumbers
                    .Skip(offset)
                    .Take(BatchSize)
                    .ToList();

                await using var command = connection.CreateCommand();
                var parameterNames = new List<string>(batch.Count);

                for (int i = 0; i < batch.Count; i++)
                {
                    string parameterName = $"@p{i}";
                    parameterNames.Add(parameterName);
                    command.Parameters.AddWithValue(parameterName, batch[i]);
                }

                command.CommandText = $"SELECT Number FROM SimDatas WHERE Number IN ({string.Join(", ", parameterNames)});";

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var number = reader.GetString(0)?.Trim();
                    if (!string.IsNullOrWhiteSpace(number))
                    {
                        existingNumbers.Add(number);
                    }
                }

                processed += batch.Count;
                progressCallback?.Invoke(processed, normalizedNumbers.Count, batch.LastOrDefault());
            }

            return new SimDataValidationResult
            {
                MissingNumbers = preparedNumbers
                    .Where(x => !existingNumbers.Contains(x.Normalized))
                    .Select(x => x.Original)
                    .Distinct(StringComparer.Ordinal)
                    .ToList()
            };
        }

        private static string NormalizePhoneNumberForLookup(string value)
        {
            var digitsOnly = new string(value.Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(digitsOnly))
            {
                return string.Empty;
            }

            // Local Ukrainian format 0XXXXXXXXX -> international 380XXXXXXXXX.
            if (digitsOnly.Length == 10 && digitsOnly.StartsWith('0'))
            {
                return $"38{digitsOnly}";
            }

            // Sometimes number can come without leading zero.
            if (digitsOnly.Length == 9)
            {
                return $"380{digitsOnly}";
            }

            return digitsOnly;
        }
    }
}
