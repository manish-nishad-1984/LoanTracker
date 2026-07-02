using LoanTracker.Application.Common;
using LoanTracker.Application.DTOs.Expenses;
using LoanTracker.Application.Interfaces;
using LoanTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LoanTracker.Infrastructure.Services;

public class ExpenseService(IApplicationDbContext db, ILogger<ExpenseService> logger) : IExpenseService
{
    public async Task<PagedResult<ExpenseListItemDto>> GetAllAsync(
        string? category, string? search, DateOnly? from, DateOnly? to,
        int page, int pageSize, CancellationToken ct = default)
    {
        var q = db.Expenses.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(e => e.Category == category);
        if (from.HasValue) q = q.Where(e => e.Date >= from.Value);
        if (to.HasValue) q = q.Where(e => e.Date <= to.Value);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(e => e.Title.ToLower().Contains(search.ToLower())
                          || (e.Vendor != null && e.Vendor.ToLower().Contains(search.ToLower())));

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new ExpenseListItemDto(e.Id, e.Date, e.Title, e.Category, e.Amount,
                e.PaymentMethod, e.Vendor, e.Source, e.Attachments.Count))
            .ToListAsync(ct);
        return PagedResult<ExpenseListItemDto>.Create(items, total, page, pageSize);
    }

    public async Task<Result<ExpenseDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var e = await db.Expenses.AsNoTracking().Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return e is null ? Result<ExpenseDto>.NotFound("Expense", id) : Result<ExpenseDto>.Success(Map(e));
    }

    public async Task<Result<ExpenseDto>> CreateAsync(CreateExpenseRequest r, CancellationToken ct = default)
    {
        var e = new Expense
        {
            Date = r.Date, Title = r.Title.Trim(), Category = r.Category.Trim(), Amount = r.Amount,
            PaymentMethod = r.PaymentMethod?.Trim(), Vendor = r.Vendor?.Trim(), Notes = r.Notes?.Trim(),
            Source = "Manual"
        };
        db.Expenses.Add(e);
        await db.SaveChangesAsync(ct);
        return Result<ExpenseDto>.Created(Map(e));
    }

    public async Task<Result<ExpenseDto>> UpdateAsync(Guid id, UpdateExpenseRequest r, CancellationToken ct = default)
    {
        var e = await db.Expenses.Include(x => x.Attachments).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return Result<ExpenseDto>.NotFound("Expense", id);
        e.Date = r.Date; e.Title = r.Title.Trim(); e.Category = r.Category.Trim(); e.Amount = r.Amount;
        e.PaymentMethod = r.PaymentMethod?.Trim(); e.Vendor = r.Vendor?.Trim(); e.Notes = r.Notes?.Trim();
        await db.SaveChangesAsync(ct);
        return Result<ExpenseDto>.Success(Map(e));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var e = await db.Expenses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return Result.NotFound("Expense", id);
        e.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<ExpenseDto>> ConvertFromTransactionAsync(ConvertTransactionRequest r, CancellationToken ct = default)
    {
        var t = await db.BankTransactions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == r.TransactionId, ct);
        if (t is null) return Result<ExpenseDto>.NotFound("Transaction", r.TransactionId);
        if (t.Withdrawal <= 0) return Result<ExpenseDto>.BadRequest("Only debit (money-out) transactions can be added as an expense.");

        // Avoid duplicates: one expense per bank transaction.
        var exists = await db.Expenses.AnyAsync(x => x.SourceReference == t.Id.ToString(), ct);
        if (exists) return Result<ExpenseDto>.Conflict("This transaction is already saved as an expense.");

        var e = new Expense
        {
            Date = t.TxnDate,
            Title = t.Merchant ?? (t.Narration.Length > 80 ? t.Narration[..80] : t.Narration),
            Category = string.IsNullOrWhiteSpace(r.Category) ? "Other" : r.Category.Trim(),
            Amount = t.Withdrawal,
            PaymentMethod = r.PaymentMethod?.Trim() ?? t.PaymentMethod,
            Vendor = r.Vendor?.Trim() ?? t.Merchant,
            Notes = string.IsNullOrWhiteSpace(r.Notes) ? $"From bank: {t.Narration}" : r.Notes.Trim(),
            Source = "Bank",
            SourceReference = t.Id.ToString()
        };
        db.Expenses.Add(e);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Converted bank txn {Txn} to expense {Exp}", t.Id, e.Id);
        return Result<ExpenseDto>.Created(Map(e));
    }

    public async Task<Result<AttachmentDto>> AddAttachmentAsync(
        Guid expenseId, string fileName, string contentType, string kind, byte[] data, CancellationToken ct = default)
    {
        var e = await db.Expenses.FirstOrDefaultAsync(x => x.Id == expenseId, ct);
        if (e is null) return Result<AttachmentDto>.NotFound("Expense", expenseId);

        var a = new ExpenseAttachment
        {
            ExpenseId = expenseId,
            FileName = fileName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            Kind = string.IsNullOrWhiteSpace(kind) ? "Bill" : kind,
            SizeBytes = data.LongLength,
            Data = data
        };
        db.ExpenseAttachments.Add(a);
        await db.SaveChangesAsync(ct);
        return Result<AttachmentDto>.Created(new AttachmentDto(a.Id, a.FileName, a.ContentType, a.Kind, a.SizeBytes));
    }

    public async Task<(byte[] data, string contentType, string fileName)?> GetAttachmentAsync(Guid attachmentId, CancellationToken ct = default)
    {
        var a = await db.ExpenseAttachments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == attachmentId, ct);
        return a is null ? null : (a.Data, a.ContentType, a.FileName);
    }

    public async Task<Result> DeleteAttachmentAsync(Guid attachmentId, CancellationToken ct = default)
    {
        var a = await db.ExpenseAttachments.FirstOrDefaultAsync(x => x.Id == attachmentId, ct);
        if (a is null) return Result.NotFound("Attachment", attachmentId);
        db.ExpenseAttachments.Remove(a);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<ExpenseSummaryDto> GetSummaryAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var q = db.Expenses.AsNoTracking().AsQueryable();
        if (from.HasValue) q = q.Where(e => e.Date >= from.Value);
        if (to.HasValue) q = q.Where(e => e.Date <= to.Value);
        var list = await q.ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thisMonthStart = new DateOnly(today.Year, today.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        var total = list.Sum(e => e.Amount);
        var byCat = list.GroupBy(e => e.Category)
            .Select(g => new ExpenseCategoryStatDto(g.Key, g.Sum(x => x.Amount),
                total == 0 ? 0 : Math.Round(g.Sum(x => x.Amount) / total * 100, 1), g.Count()))
            .OrderByDescending(c => c.Total).ToList();

        var monthly = list.GroupBy(e => new { e.Date.Year, e.Date.Month })
            .Select(g => new ExpenseMonthlyDto(g.Key.Year, g.Key.Month,
                new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                g.Sum(x => x.Amount), g.Count()))
            .OrderBy(m => m.Year).ThenBy(m => m.Month).ToList();

        var recent = list.OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt).Take(8)
            .Select(e => new ExpenseListItemDto(e.Id, e.Date, e.Title, e.Category, e.Amount,
                e.PaymentMethod, e.Vendor, e.Source, e.Attachments.Count)).ToList();

        var days = list.Count == 0 ? 1 : Math.Max(1, (list.Max(e => e.Date).DayNumber - list.Min(e => e.Date).DayNumber) + 1);

        return new ExpenseSummaryDto(
            list.Count > 0 ? list.Min(e => e.Date) : null,
            list.Count > 0 ? list.Max(e => e.Date) : null,
            total, list.Count,
            Math.Round(total / days, 2),
            list.Count == 0 ? 0 : Math.Round(total / list.Count, 2),
            list.Where(e => e.Date >= thisMonthStart).Sum(e => e.Amount),
            list.Where(e => e.Date >= lastMonthStart && e.Date < thisMonthStart).Sum(e => e.Amount),
            byCat.FirstOrDefault()?.Category,
            byCat, monthly, recent);
    }

    private static ExpenseDto Map(Expense e) => new(
        e.Id, e.Date, e.Title, e.Category, e.Amount, e.PaymentMethod, e.Vendor, e.Notes,
        e.Source, e.SourceReference, e.CreatedAt,
        e.Attachments.Select(a => new AttachmentDto(a.Id, a.FileName, a.ContentType, a.Kind, a.SizeBytes)).ToList());
}
