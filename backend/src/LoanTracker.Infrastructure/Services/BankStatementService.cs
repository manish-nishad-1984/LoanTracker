using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ExcelDataReader;
using LoanTracker.Application.Common;
using LoanTracker.Application.DTOs.Banking;
using LoanTracker.Application.Interfaces;
using LoanTracker.Application.Services;
using LoanTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LoanTracker.Infrastructure.Services;

public partial class BankStatementService(IApplicationDbContext db, ILogger<BankStatementService> logger)
    : IBankStatementService
{
    static BankStatementService() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    [GeneratedRegex(@"^\d{2}/\d{2}/\d{2,4}$")]
    private static partial Regex DateRegex();

    private record ParsedRow(int Seq, DateOnly? Date, DateOnly? ValueDate, string Narration,
        string? Ref, decimal Withdrawal, decimal Deposit, decimal? Balance);
    private record ParsedStatement(string? AccountNumber, string? AccountName, string? Bank,
        string? Period, DateOnly? From, DateOnly? To, List<ParsedRow> Rows);

    // ───────────────────────── Parsing ─────────────────────────

    private ParsedStatement ParseFile(Stream stream)
    {
        using var reader = ExcelReaderFactory.CreateReader(stream);
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

        var headerIdx = rows.FindIndex(r =>
            r.Length > 1 && string.Equals(r[0], "Date", StringComparison.OrdinalIgnoreCase) &&
            (r[1]?.Contains("Narration", StringComparison.OrdinalIgnoreCase) ?? false));
        if (headerIdx < 0)
            throw new InvalidOperationException("Could not find the transaction header (Date / Narration).");

        string? Find(string label) => rows.SelectMany(r => r)
            .FirstOrDefault(c => c?.Contains(label, StringComparison.OrdinalIgnoreCase) == true);

        var accountNo = ExtractAfter(Find("Account No"), "Account No");
        var accountName = rows.Skip(1).Take(headerIdx).Select(r => r[0])
            .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c) && c!.StartsWith("MR", StringComparison.OrdinalIgnoreCase));
        var period = Find("Statement From");

        var parsed = new List<ParsedRow>();
        for (var i = headerIdx + 1; i < rows.Count; i++)
        {
            var r = rows[i];
            var c0 = r.Length > 0 ? r[0] : null;
            if (string.IsNullOrWhiteSpace(c0) || !DateRegex().IsMatch(c0!)) continue;
            parsed.Add(new ParsedRow(
                Seq: i,
                Date: ParseDate(c0),
                ValueDate: ParseDate(Get(r, 3)),
                Narration: Get(r, 1) ?? "",
                Ref: Get(r, 2),
                Withdrawal: ParseAmount(Get(r, 4)),
                Deposit: ParseAmount(Get(r, 5)),
                Balance: ParseAmountNullable(Get(r, 6))));
        }

        var dates = parsed.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToList();
        return new ParsedStatement(accountNo, accountName, "HDFC Bank", period,
            dates.Count > 0 ? dates.Min() : null, dates.Count > 0 ? dates.Max() : null, parsed);
    }

    public Result<BankStatementPreviewDto> ParsePreview(Stream fileStream, string fileName)
    {
        try
        {
            var st = ParseFile(fileStream);
            if (st.Rows.Count == 0) return Result<BankStatementPreviewDto>.BadRequest("No transactions found.");

            decimal totalDep = 0, totalWd = 0;
            var txns = st.Rows.Select(r =>
            {
                var c = TransactionCategorizer.Classify(r.Narration, r.Deposit, r.Withdrawal);
                totalDep += r.Deposit; totalWd += r.Withdrawal;
                return new BankTransactionPreviewDto(r.Seq + 1, r.Date, r.Narration, r.Ref, r.ValueDate,
                    r.Withdrawal, r.Deposit, r.Balance, c.Direction, c.PaymentMethod, c.Category);
            }).ToList();

            return Result<BankStatementPreviewDto>.Success(new BankStatementPreviewDto(
                st.AccountNumber, st.AccountName, st.Bank, CleanLabel(st.Period),
                txns.Count, totalDep, totalWd, totalDep - totalWd, txns));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Preview parse failed for {File}", fileName);
            return Result<BankStatementPreviewDto>.BadRequest($"Could not read the file: {ex.Message}");
        }
    }

    // ───────────────────────── Import ─────────────────────────

    public async Task<Result<ImportResultDto>> ImportAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        ParsedStatement st;
        try { st = ParseFile(fileStream); }
        catch (Exception ex) { return Result<ImportResultDto>.BadRequest($"Could not read the file: {ex.Message}"); }

        if (st.Rows.Count == 0) return Result<ImportResultDto>.BadRequest("No transactions found.");
        var acctNo = string.IsNullOrWhiteSpace(st.AccountNumber) ? "UNKNOWN" : st.AccountNumber!;

        var account = await db.BankAccounts.FirstOrDefaultAsync(a => a.AccountNumber == acctNo, ct);
        if (account is null)
        {
            account = new BankAccount { AccountNumber = acctNo, AccountName = st.AccountName, Bank = st.Bank };
            db.BankAccounts.Add(account);
            await db.SaveChangesAsync(ct);
        }

        var existing = await db.BankTransactions
            .Where(t => t.BankAccountId == account.Id)
            .Select(t => t.DedupKey).ToListAsync(ct);
        var seen = new HashSet<string>(existing);

        int imported = 0, skipped = 0;
        foreach (var r in st.Rows)
        {
            var key = $"{r.Date:yyyyMMdd}|{r.Ref}|{r.Withdrawal}|{r.Deposit}|{r.Balance}";
            if (!seen.Add(key)) { skipped++; continue; }

            var c = TransactionCategorizer.Classify(r.Narration, r.Deposit, r.Withdrawal);
            db.BankTransactions.Add(new BankTransaction
            {
                BankAccountId = account.Id,
                Seq = r.Seq,
                TxnDate = r.Date ?? default,
                ValueDate = r.ValueDate,
                Narration = r.Narration.Length > 500 ? r.Narration[..500] : r.Narration,
                ReferenceNo = r.Ref,
                Withdrawal = r.Withdrawal,
                Deposit = r.Deposit,
                ClosingBalance = r.Balance,
                Direction = c.Direction,
                Group = c.Group,
                PaymentMethod = c.PaymentMethod,
                Category = c.Category,
                Merchant = c.Merchant,
                DedupKey = key
            });
            imported++;
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Imported {Imp} txns ({Skip} dupes) into account {Acct}", imported, skipped, acctNo);
        return Result<ImportResultDto>.Created(new ImportResultDto(
            account.Id, account.AccountNumber, account.AccountName, st.Rows.Count, imported, skipped));
    }

    public async Task<IReadOnlyList<BankAccountDto>> GetAccountsAsync(CancellationToken ct = default) =>
        await db.BankAccounts.AsNoTracking()
            .Select(a => new BankAccountDto(a.Id, a.AccountNumber, a.AccountName, a.Bank, a.Transactions.Count))
            .ToListAsync(ct);

    // ───────────────────────── Analytics ─────────────────────────

    public async Task<Result<BankDashboardDto>> GetDashboardAsync(Guid? accountId, DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var q = db.BankTransactions.AsNoTracking().AsQueryable();
        if (accountId.HasValue) q = q.Where(t => t.BankAccountId == accountId.Value);
        if (from.HasValue) q = q.Where(t => t.TxnDate >= from.Value);
        if (to.HasValue) q = q.Where(t => t.TxnDate <= to.Value);

        var txns = await q.OrderBy(t => t.TxnDate).ThenBy(t => t.Seq).ToListAsync(ct);
        if (txns.Count == 0)
            return Result<BankDashboardDto>.BadRequest("No transactions yet. Import a statement first.");

        var account = await db.BankAccounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == (accountId ?? txns[0].BankAccountId), ct);

        var credits = txns.Where(t => t.Deposit > 0).ToList();
        var debits = txns.Where(t => t.Withdrawal > 0).ToList();
        decimal totalCredits = credits.Sum(t => t.Deposit);
        decimal totalDebits = debits.Sum(t => t.Withdrawal);

        var fromDate = txns.First().TxnDate;
        var toDate = txns.Last().TxnDate;
        var days = Math.Max(1, toDate.DayNumber - fromDate.DayNumber + 1);

        var first = txns.First();
        var opening = (first.ClosingBalance ?? 0) - first.Deposit + first.Withdrawal;
        var closing = txns.Last(t => t.ClosingBalance != null).ClosingBalance ?? opening + totalCredits - totalDebits;

        // Expense categories (exclude pure transfers from "spending" ratios but still show them)
        var expenses = txns.Where(t => t.Group == "Expense").ToList();
        var incomes = txns.Where(t => t.Group == "Income").ToList();

        List<CategoryStatDto> CatStats(List<BankTransaction> list, Func<BankTransaction, decimal> amt)
        {
            var total = list.Sum(amt);
            return list.GroupBy(t => t.Category)
                .Select(g =>
                {
                    var amts = g.Select(amt).ToList();
                    return new CategoryStatDto(g.Key, amts.Sum(),
                        total == 0 ? 0 : Math.Round(amts.Sum() / total * 100, 1),
                        g.Count(), Math.Round(amts.Average(), 2), amts.Max(), amts.Min());
                })
                .OrderByDescending(s => s.Total).ToList();
        }

        var expenseByCat = CatStats(expenses, t => t.Withdrawal);
        var incomeByCat = CatStats(incomes, t => t.Deposit);

        var topMerchants = debits.Where(t => !string.IsNullOrWhiteSpace(t.Merchant))
            .GroupBy(t => t.Merchant!)
            .Select(g => new MerchantStatDto(g.Key, g.Sum(t => t.Withdrawal), g.Count()))
            .OrderByDescending(m => m.Total).Take(10).ToList();

        var methodTotal = debits.Sum(t => t.Withdrawal);
        var paymentMethods = debits.GroupBy(t => t.PaymentMethod)
            .Select(g => new PaymentMethodStatDto(g.Key, g.Sum(t => t.Withdrawal), g.Count(),
                methodTotal == 0 ? 0 : Math.Round(g.Sum(t => t.Withdrawal) / methodTotal * 100, 1)))
            .OrderByDescending(m => m.Total).ToList();

        var monthly = txns.GroupBy(t => new { t.TxnDate.Year, t.TxnDate.Month })
            .Select(g => new MonthlyCashflowDto(g.Key.Year, g.Key.Month,
                new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                g.Sum(t => t.Deposit), g.Sum(t => t.Withdrawal),
                g.Sum(t => t.Deposit) - g.Sum(t => t.Withdrawal),
                g.OrderBy(t => t.Seq).Last().ClosingBalance))
            .OrderBy(m => m.Year).ThenBy(m => m.Month).ToList();

        var dailySpend = debits.GroupBy(t => t.TxnDate)
            .Select(g => new DailySpendDto(g.Key, g.Sum(t => t.Withdrawal)))
            .OrderBy(d => d.Date).ToList();

        bool IsWeekend(DateOnly d) => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var weekend = new WeekendWeekdayDto(
            debits.Where(t => IsWeekend(t.TxnDate)).Sum(t => t.Withdrawal),
            debits.Where(t => !IsWeekend(t.TxnDate)).Sum(t => t.Withdrawal),
            debits.Count(t => IsWeekend(t.TxnDate)), debits.Count(t => !IsWeekend(t.TxnDate)));

        var recurring = debits.Where(t => !string.IsNullOrWhiteSpace(t.Merchant))
            .GroupBy(t => t.Merchant!)
            .Where(g => g.Count() >= 3)
            .Select(g => new RecurringDto(g.Key, g.Count(), Math.Round(g.Average(t => t.Withdrawal), 2),
                g.First().Category,
                g.First().Category is "Entertainment" or "Utilities" or "Insurance" or "Business"
                    && g.Select(t => Math.Round(t.Withdrawal)).Distinct().Count() <= 2))
            .OrderByDescending(r => r.Count).Take(10).ToList();

        TxnLineDto Line(BankTransaction t, decimal amt, string dir) =>
            new(t.Id, t.TxnDate, t.Narration, t.Merchant, amt, dir, t.Category, t.PaymentMethod);

        var largestExpenses = debits.OrderByDescending(t => t.Withdrawal).Take(10)
            .Select(t => Line(t, t.Withdrawal, "Out")).ToList();
        var largestIncomes = credits.OrderByDescending(t => t.Deposit).Take(5)
            .Select(t => Line(t, t.Deposit, "In")).ToList();

        var savingsRate = totalCredits == 0 ? 0 : Math.Round((totalCredits - totalDebits) / totalCredits * 100, 1);
        var expenseRatio = totalCredits == 0 ? 0 : Math.Round(totalDebits / totalCredits * 100, 1);

        var (score, label) = HealthScore(savingsRate, expenseRatio, recurring.Count, closing);
        var largestMerchant = topMerchants.FirstOrDefault()?.Merchant;
        var mostFrequentMerchant = debits.Where(t => t.Merchant != null)
            .GroupBy(t => t.Merchant!).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;
        var mostUsedMethod = paymentMethods.OrderByDescending(m => m.Count).FirstOrDefault()?.Method;

        var insights = BuildInsights(totalCredits, totalDebits, expenseByCat, incomeByCat, monthly,
            paymentMethods, recurring, savingsRate, expenseRatio, days, largestMerchant, weekend, score, label);

        return Result<BankDashboardDto>.Success(new BankDashboardDto(
            account?.AccountNumber, account?.AccountName, account?.Bank,
            fromDate, toDate, days,
            opening, closing, totalCredits, totalDebits, totalCredits - totalDebits,
            Math.Round(totalCredits / days, 2), Math.Round(totalDebits / days, 2),
            credits.Count > 0 ? credits.Max(t => t.Deposit) : 0,
            debits.Count > 0 ? debits.Max(t => t.Withdrawal) : 0,
            txns.Count, Math.Round(txns.Average(t => t.Deposit + t.Withdrawal), 2),
            largestMerchant, mostFrequentMerchant, mostUsedMethod,
            savingsRate, expenseRatio, score, label,
            incomeByCat, expenseByCat, topMerchants, paymentMethods, monthly, dailySpend,
            weekend, recurring, largestExpenses, largestIncomes, insights));
    }

    public async Task<IReadOnlyList<GroupSummaryDto>> GetGroupedSummaryAsync(
        string groupBy, Guid? accountId, string? category, string? direction, string? search,
        DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var list = await FilterTxns(accountId, category, direction, search, null, null, from, to)
            .ToListAsync(ct);

        Func<Domain.Entities.BankTransaction, string> sel = groupBy.ToLowerInvariant() switch
        {
            "merchant" => t => string.IsNullOrWhiteSpace(t.Merchant) ? "(Unknown)" : t.Merchant!,
            "paymentmethod" or "method" => t => t.PaymentMethod,
            "month" => t => new DateTime(t.TxnDate.Year, t.TxnDate.Month, 1).ToString("MMM yyyy"),
            "direction" => t => t.Direction == "In" ? "Money In" : "Money Out",
            _ => t => t.Category
        };

        var groups = list.GroupBy(sel)
            .Select(g => new GroupSummaryDto(g.Key, g.Count(),
                g.Sum(x => x.Deposit), g.Sum(x => x.Withdrawal),
                g.Sum(x => x.Deposit) - g.Sum(x => x.Withdrawal)))
            .ToList();

        return groupBy.Equals("month", StringComparison.OrdinalIgnoreCase)
            ? groups.OrderBy(g => DateTime.ParseExact(g.Key, "MMM yyyy", CultureInfo.InvariantCulture)).ToList()
            : groups.OrderByDescending(g => g.TotalIn + g.TotalOut).ToList();
    }

    private static (int score, string label) HealthScore(decimal savingsRate, decimal expenseRatio, int recurring, decimal closing)
    {
        var s = 50;
        s += savingsRate switch { >= 30 => 30, >= 20 => 22, >= 10 => 14, >= 0 => 6, _ => -15 };
        s += expenseRatio switch { <= 70 => 15, <= 90 => 8, <= 100 => 2, _ => -10 };
        s += closing > 0 ? 5 : -10;
        s = Math.Clamp(s, 0, 100);
        var label = s >= 80 ? "Excellent" : s >= 65 ? "Good" : s >= 45 ? "Fair" : "Needs Attention";
        return (s, label);
    }

    private static List<string> BuildInsights(
        decimal credits, decimal debits, List<CategoryStatDto> exp, List<CategoryStatDto> inc,
        List<MonthlyCashflowDto> monthly, List<PaymentMethodStatDto> methods, List<RecurringDto> recurring,
        decimal savingsRate, decimal expenseRatio, int days, string? largestMerchant,
        WeekendWeekdayDto weekend, int score, string label)
    {
        var ins = new List<string>();
        string Cur(decimal v) => "₹" + v.ToString("N0", CultureInfo.InvariantCulture);

        if (exp.Count > 0)
            ins.Add($"Your highest expense category is {exp[0].Category} ({exp[0].Percentage}% of spending, {Cur(exp[0].Total)}).");
        var food = exp.FirstOrDefault(e => e.Category == "Food");
        if (food is not null) ins.Add($"You spend {food.Percentage}% of expenses on Food ({Cur(food.Total)}).");
        var upi = methods.FirstOrDefault(m => m.Method == "UPI");
        if (upi is not null && upi.Count > 0)
            ins.Add($"Average UPI payment is {Cur(Math.Round(upi.Total / upi.Count))} across {upi.Count} UPI debits.");
        if (credits > 0)
            ins.Add($"Income covers {(debits == 0 ? 100 : Math.Round(credits / debits * 100))}% of your expenses; savings rate is {savingsRate}%.");
        if (savingsRate < 0) ins.Add("You spent more than you earned in this period — net cash flow is negative.");
        if (monthly.Count >= 2)
        {
            var last = monthly[^1]; var prev = monthly[^2];
            if (prev.Expense > 0)
            {
                var change = Math.Round((last.Expense - prev.Expense) / prev.Expense * 100, 1);
                ins.Add($"Spending {(change >= 0 ? "increased" : "decreased")} by {Math.Abs(change)}% in {last.MonthName} vs {prev.MonthName}.");
            }
            var best = monthly.OrderByDescending(m => m.Net).First();
            var worst = monthly.OrderByDescending(m => m.Expense).First();
            ins.Add($"Highest savings month: {best.MonthName} (net {Cur(best.Net)}). Highest spending month: {worst.MonthName} ({Cur(worst.Expense)}).");
        }
        if (recurring.Count > 0)
        {
            ins.Add($"Detected {recurring.Count} recurring payee(s); {recurring.Count(r => r.LikelySubscription)} look like subscriptions.");
            var subs = recurring.Where(r => r.LikelySubscription).Take(3).Select(r => r.Merchant);
            if (subs.Any()) ins.Add($"Possible subscriptions: {string.Join(", ", subs)}.");
        }
        if (largestMerchant is not null) ins.Add($"Largest merchant by spend: {largestMerchant}.");
        ins.Add($"Weekend spending {Cur(weekend.WeekendSpend)} vs weekday {Cur(weekend.WeekdaySpend)}.");
        if (expenseRatio > 90) ins.Add("Expense ratio is high (>90%) — consider trimming discretionary categories like Shopping/Food.");
        ins.Add($"Overall financial health score: {score}/100 ({label}).");
        return ins;
    }

    private IQueryable<Domain.Entities.BankTransaction> FilterTxns(
        Guid? accountId, string? category, string? direction, string? search,
        string? merchant, string? paymentMethod, DateOnly? from, DateOnly? to)
    {
        var q = db.BankTransactions.AsNoTracking().AsQueryable();
        if (accountId.HasValue) q = q.Where(t => t.BankAccountId == accountId.Value);
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(t => t.Category == category);
        if (!string.IsNullOrWhiteSpace(direction)) q = q.Where(t => t.Direction == direction);
        if (!string.IsNullOrWhiteSpace(merchant)) q = q.Where(t => t.Merchant == merchant);
        if (!string.IsNullOrWhiteSpace(paymentMethod)) q = q.Where(t => t.PaymentMethod == paymentMethod);
        if (from.HasValue) q = q.Where(t => t.TxnDate >= from.Value);
        if (to.HasValue) q = q.Where(t => t.TxnDate <= to.Value);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(t => t.Narration.ToLower().Contains(search.ToLower())
                          || (t.Merchant != null && t.Merchant.ToLower().Contains(search.ToLower())));
        return q;
    }

    public async Task<BankTxnListDto> GetTransactionsAsync(
        Guid? accountId, string? category, string? direction, string? search,
        string? merchant, string? paymentMethod,
        DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default)
    {
        var q = FilterTxns(accountId, category, direction, search, merchant, paymentMethod, from, to);

        var total = await q.CountAsync(ct);
        var totalIn = await q.SumAsync(t => (decimal?)t.Deposit, ct) ?? 0;
        var totalOut = await q.SumAsync(t => (decimal?)t.Withdrawal, ct) ?? 0;
        var items = await q.OrderByDescending(t => t.TxnDate).ThenByDescending(t => t.Seq)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new TxnLineDto(t.Id, t.TxnDate, t.Narration, t.Merchant,
                t.Direction == "In" ? t.Deposit : t.Withdrawal, t.Direction, t.Category, t.PaymentMethod))
            .ToListAsync(ct);
        return new BankTxnListDto(items, total, page, pageSize, totalIn, totalOut);
    }

    public async Task<Result> UpdateCategoryAsync(Guid transactionId, string category, CancellationToken ct = default)
    {
        var t = await db.BankTransactions.FirstOrDefaultAsync(x => x.Id == transactionId, ct);
        if (t is null) return Result.NotFound("Transaction", transactionId);
        t.Category = category.Trim();
        t.Group = t.Direction == "In" ? "Income" : (t.Category == "Transfer" ? "Transfer" : "Expense");
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ───────────────────────── helpers ─────────────────────────
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
        return rest.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    }
    private static string? CleanLabel(string? cell) =>
        string.IsNullOrWhiteSpace(cell) ? null : cell.Replace("Statement From", "From", StringComparison.OrdinalIgnoreCase).Trim();
}
