using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ExcelDataReader;
using LoanTracker.Application.Common;
using LoanTracker.Application.DTOs.Banking;
using LoanTracker.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace LoanTracker.Infrastructure.Services;

public partial class BankStatementService : IBankStatementService
{
    private readonly ILogger<BankStatementService> _logger;

    static BankStatementService()
    {
        // Required by ExcelDataReader for legacy .xls (BIFF) encodings.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public BankStatementService(ILogger<BankStatementService> logger) => _logger = logger;

    [GeneratedRegex(@"^\d{2}/\d{2}/\d{2,4}$")]
    private static partial Regex DateRegex();

    public Result<BankStatementPreviewDto> ParsePreview(Stream fileStream, string fileName)
    {
        try
        {
            // CreateReader auto-detects xls vs xlsx by content, so an .xlsx file
            // saved with a .xls name (as HDFC does) is handled correctly.
            using var reader = ExcelReaderFactory.CreateReader(fileStream);

            var rows = new List<string?[]>();
            do
            {
                while (reader.Read())
                {
                    var row = new string?[reader.FieldCount];
                    for (var i = 0; i < reader.FieldCount; i++)
                        row[i] = reader.GetValue(i)?.ToString()?.Trim();
                    rows.Add(row);
                }
            } while (reader.NextResult() && rows.Count == 0);

            // Locate the header row: contains "Date" and "Narration".
            var headerIdx = rows.FindIndex(r =>
                r.Length > 1 &&
                string.Equals(r[0], "Date", StringComparison.OrdinalIgnoreCase) &&
                (r[1]?.Contains("Narration", StringComparison.OrdinalIgnoreCase) ?? false));

            if (headerIdx < 0)
                return Result<BankStatementPreviewDto>.BadRequest(
                    "Could not find the transaction header (Date / Narration). Is this an HDFC account statement?");

            // Account info from header block (best effort).
            string? Find(string label) => rows
                .SelectMany(r => r)
                .FirstOrDefault(c => c?.Contains(label, StringComparison.OrdinalIgnoreCase) == true);

            var accountNo = ExtractAfter(Find("Account No"), "Account No");
            var period = CleanLabel(Find("Statement From"));
            var accountName = rows.Skip(1).Take(headerIdx)
                .Select(r => r[0])
                .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c) && c!.StartsWith("MR", StringComparison.OrdinalIgnoreCase));

            var txns = new List<BankTransactionPreviewDto>();
            decimal totalDep = 0, totalWd = 0;

            for (var i = headerIdx + 1; i < rows.Count; i++)
            {
                var r = rows[i];
                var c0 = r.Length > 0 ? r[0] : null;
                if (string.IsNullOrWhiteSpace(c0)) continue;
                if (!DateRegex().IsMatch(c0!)) continue; // skip separators/footers

                var narration = Get(r, 1) ?? "";
                var refNo = Get(r, 2);
                var withdrawal = ParseAmount(Get(r, 4));
                var deposit = ParseAmount(Get(r, 5));
                var balance = ParseAmountNullable(Get(r, 6));
                var (type, category) = Categorize(narration);

                totalDep += deposit;
                totalWd += withdrawal;

                txns.Add(new BankTransactionPreviewDto(
                    RowNumber: i + 1,
                    Date: ParseDate(c0),
                    Narration: narration,
                    ReferenceNo: refNo,
                    ValueDate: ParseDate(Get(r, 3)),
                    Withdrawal: withdrawal,
                    Deposit: deposit,
                    ClosingBalance: balance,
                    Direction: deposit > 0 ? "In" : "Out",
                    GuessedType: type,
                    GuessedCategory: category
                ));
            }

            if (txns.Count == 0)
                return Result<BankStatementPreviewDto>.BadRequest("No transactions found in the statement.");

            var preview = new BankStatementPreviewDto(
                AccountNumber: accountNo,
                AccountName: accountName,
                Bank: "HDFC Bank",
                Period: period,
                TransactionCount: txns.Count,
                TotalDeposit: totalDep,
                TotalWithdrawal: totalWd,
                NetChange: totalDep - totalWd,
                Transactions: txns);

            _logger.LogInformation("Parsed statement {File}: {Count} transactions", fileName, txns.Count);
            return Result<BankStatementPreviewDto>.Success(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse statement {File}", fileName);
            return Result<BankStatementPreviewDto>.BadRequest($"Could not read the file: {ex.Message}");
        }
    }

    private static string? Get(string?[] r, int i) => i < r.Length ? r[i] : null;

    private static decimal ParseAmount(string? s) => ParseAmountNullable(s) ?? 0m;

    private static decimal? ParseAmountNullable(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Replace(",", "").Trim();
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static DateOnly? ParseDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        foreach (var fmt in new[] { "dd/MM/yy", "dd/MM/yyyy", "dd-MM-yy", "dd-MM-yyyy" })
            if (DateTime.TryParseExact(s, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return DateOnly.FromDateTime(d);
        return null;
    }

    private static string? ExtractAfter(string? cell, string label)
    {
        if (string.IsNullOrWhiteSpace(cell)) return null;
        var idx = cell.IndexOf(label, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var rest = cell[(idx + label.Length)..].TrimStart(' ', ':');
        var token = rest.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return token;
    }

    private static string? CleanLabel(string? cell) =>
        string.IsNullOrWhiteSpace(cell) ? null : cell.Replace("Statement From", "From", StringComparison.OrdinalIgnoreCase).Trim();

    /// <summary>Guess a transaction type + category from the HDFC narration.</summary>
    private static (string type, string category) Categorize(string narration)
    {
        var n = narration.ToUpperInvariant();
        if (n.Contains("SALARY") || n.Contains("SAL CREDIT")) return ("Salary", "Income");
        if (n.StartsWith("UPI")) return ("UPI", "UPI Payment");
        if (n.StartsWith("NEFT")) return ("NEFT", "Bank Transfer");
        if (n.StartsWith("IMPS")) return ("IMPS", "Bank Transfer");
        if (n.StartsWith("RTGS")) return ("RTGS", "Bank Transfer");
        if (n.StartsWith("ACH") || n.Contains("BAJAJFIN") || n.Contains("EMI") || n.Contains("LOAN"))
            return ("ACH/EMI", "Loan / EMI");
        if (n.StartsWith("POS") || n.Contains("PURCHASE") || n.StartsWith("ECOM")) return ("POS/Card", "Shopping");
        if (n.StartsWith("ATW") || n.StartsWith("NWD") || n.Contains("ATM") || n.StartsWith("EAW"))
            return ("ATM", "Cash Withdrawal");
        if (n.Contains("INT.") || n.Contains("INTEREST")) return ("Interest", "Interest Income");
        if (n.Contains("CHRG") || n.Contains("CHARGE") || n.Contains("FEE") || n.Contains("GST"))
            return ("Charges", "Bank Charges");
        return ("Other", "Uncategorized");
    }
}
