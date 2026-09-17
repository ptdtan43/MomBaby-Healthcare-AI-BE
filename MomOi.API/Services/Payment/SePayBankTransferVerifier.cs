using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MomOi.API.Models.Identity;
using MomOi.API.Options;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace MomOi.API.Services.Payment
{
    public record SePayMatchedTransaction(
        string ProviderTxnNo,
        decimal AmountIn,
        DateTime TransactionDate,
        string TransactionContent,
        string RawPayload);

    public class SePayBankTransferVerifier
    {
        private readonly HttpClient _httpClient;
        private readonly BankTransferOptions _options;
        private readonly ILogger<SePayBankTransferVerifier> _logger;

        public SePayBankTransferVerifier(
            HttpClient httpClient,
            IOptions<BankTransferOptions> options,
            ILogger<SePayBankTransferVerifier> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_options.SepayApiToken)
            && !string.IsNullOrWhiteSpace(_options.SepayTransactionsUrl);

        public async Task<SePayMatchedTransaction?> FindMatchAsync(PaymentTransaction txn, string transferMemo)
        {
            if (!IsConfigured)
                return null;

            using var request = new HttpRequestMessage(HttpMethod.Get, _options.SepayTransactionsUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SepayApiToken);

            using var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("SePay returned {StatusCode} while checking {OrderCode}: {Body}",
                    (int)response.StatusCode, txn.OrderCode, body);
                return null;
            }

            using var doc = JsonDocument.Parse(body);
            if (!TryGetTransactions(doc.RootElement, out var transactions))
                return null;

            foreach (var tx in transactions.EnumerateArray())
            {
                var providerTxnNo = GetString(tx, "id", "transaction_id", "reference_number", "bank_tran_id");
                if (string.IsNullOrWhiteSpace(providerTxnNo))
                    continue;

                var amountIn = GetDecimal(tx, "amount_in", "amountIn");
                if (amountIn != txn.Amount)
                    continue;

                if (!TryGetDate(tx, out var transactionDate) || IsTooOldForOrder(transactionDate, txn.CreatedAt))
                    continue;

                var content = GetString(tx, "transaction_content", "content", "description") ?? string.Empty;
                var compactContent = Normalize(content);
                if (!compactContent.Contains(Normalize(txn.OrderCode), StringComparison.OrdinalIgnoreCase))
                    continue;

                return new SePayMatchedTransaction(
                    providerTxnNo,
                    amountIn,
                    transactionDate,
                    content,
                    tx.GetRawText());
            }

            return null;
        }

        private static bool TryGetTransactions(JsonElement root, out JsonElement transactions)
        {
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("transactions", out transactions) && transactions.ValueKind == JsonValueKind.Array)
                    return true;

                if (root.TryGetProperty("data", out var data))
                {
                    if (data.ValueKind == JsonValueKind.Array)
                    {
                        transactions = data;
                        return true;
                    }

                    if (data.ValueKind == JsonValueKind.Object
                        && data.TryGetProperty("transactions", out transactions)
                        && transactions.ValueKind == JsonValueKind.Array)
                        return true;
                }
            }

            transactions = default;
            return false;
        }

        private static string? GetString(JsonElement element, params string[] names)
        {
            foreach (var name in names)
            {
                if (!element.TryGetProperty(name, out var value))
                    continue;

                return value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString(),
                    JsonValueKind.Number => value.GetRawText(),
                    _ => value.ToString()
                };
            }

            return null;
        }

        private static decimal GetDecimal(JsonElement element, params string[] names)
        {
            var raw = GetString(element, names);
            if (string.IsNullOrWhiteSpace(raw))
                return 0m;

            raw = raw.Replace(",", string.Empty);
            return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0m;
        }

        private static bool TryGetDate(JsonElement element, out DateTime date)
        {
            var raw = GetString(element, "transaction_date", "transactionDate", "created_at", "createdAt");
            if (string.IsNullOrWhiteSpace(raw))
            {
                date = default;
                return false;
            }

            raw = raw.Replace(' ', 'T');
            if (!DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return false;

            if (date.Kind == DateTimeKind.Utc)
                return true;

            if (raw.EndsWith("Z", StringComparison.OrdinalIgnoreCase) || raw.Contains('+'))
            {
                date = date.ToUniversalTime();
                return true;
            }

            date = DateTime.SpecifyKind(date, DateTimeKind.Unspecified).AddHours(-7);
            return true;
        }

        private static bool IsTooOldForOrder(DateTime transactionDate, DateTime orderCreatedAt)
        {
            var normalizedTransactionDate = transactionDate.Kind == DateTimeKind.Utc
                ? transactionDate
                : DateTime.SpecifyKind(transactionDate, DateTimeKind.Utc);

            var normalizedOrderCreatedAt = orderCreatedAt.Kind == DateTimeKind.Utc
                ? orderCreatedAt
                : DateTime.SpecifyKind(orderCreatedAt, DateTimeKind.Utc);

            // Bank/SePay timestamps may be rounded to the minute, while orders are stored with seconds.
            // Keep the "newer than order" safety check, but allow a small clock/precision tolerance.
            return normalizedTransactionDate < normalizedOrderCreatedAt.AddMinutes(-5);
        }

        private static string Normalize(string value) =>
            new(value
                .Where(char.IsLetterOrDigit)
                .Select(char.ToUpperInvariant)
                .ToArray());
    }
}
