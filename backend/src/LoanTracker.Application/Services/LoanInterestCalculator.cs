using LoanTracker.Domain.Enums;

namespace LoanTracker.Application.Services;

/// <summary>
/// Pure calculation of interest accrued on a loan from its start date up to a given "as of" date.
///
/// Method: interest accrues on the OUTSTANDING principal balance over time. The timeline is
/// split into segments at each principal-reducing payment and each interest-rate change; within
/// each segment the balance and rate are constant, so:
///     segmentInterest = outstandingBalance * (annualRate / 100) * (segmentDays / 365)
/// The segment interests are summed. This correctly reflects a flexible ledger where principal
/// is repaid irregularly and the rate may change mid-term.
///
/// Interest types:
///   - None             : always 0
///   - Flat             : on the ORIGINAL principal for the full elapsed time (does not reduce)
///   - Simple / Reducing / Compound : reducing-balance daily accrual (compound is approximated)
/// </summary>
public static class LoanInterestCalculator
{
    private const decimal DaysPerYear = 365m;

    public readonly record struct PrincipalEvent(DateOnly Date, decimal PrincipalPaid);
    public readonly record struct RateChange(DateOnly EffectiveDate, decimal PreviousRate, decimal NewRate);

    public static decimal AccruedInterestToDate(
        decimal originalPrincipal,
        decimal currentRate,
        InterestCalculationType type,
        DateOnly startDate,
        DateOnly? closureDate,
        IReadOnlyCollection<PrincipalEvent> principalEvents,
        IReadOnlyCollection<RateChange> rateChanges,
        DateOnly asOf)
    {
        if (type == InterestCalculationType.None) return 0m;

        // Accrue only up to closure (if the loan closed before today) or today, whichever is earlier.
        var endDate = closureDate.HasValue && closureDate.Value < asOf ? closureDate.Value : asOf;
        if (endDate <= startDate) return 0m;

        if (type == InterestCalculationType.Flat)
        {
            var years = (endDate.DayNumber - startDate.DayNumber) / DaysPerYear;
            return Math.Round(originalPrincipal * currentRate / 100m * years, 2, MidpointRounding.AwayFromZero);
        }

        var sortedRates = rateChanges.OrderBy(r => r.EffectiveDate).ToList();

        // Breakpoints: start, end, every payment date and rate-change date strictly inside (start, end).
        var breakpoints = new SortedSet<DateOnly> { startDate, endDate };
        foreach (var e in principalEvents)
            if (e.Date > startDate && e.Date < endDate) breakpoints.Add(e.Date);
        foreach (var r in sortedRates)
            if (r.EffectiveDate > startDate && r.EffectiveDate < endDate) breakpoints.Add(r.EffectiveDate);

        var points = breakpoints.ToList();
        decimal total = 0m;

        for (var i = 0; i < points.Count - 1; i++)
        {
            var segStart = points[i];
            var segEnd = points[i + 1];
            var days = segEnd.DayNumber - segStart.DayNumber;
            if (days <= 0) continue;

            var outstanding = originalPrincipal
                - principalEvents.Where(e => e.Date <= segStart).Sum(e => e.PrincipalPaid);
            if (outstanding <= 0) continue;

            var rate = RateAt(segStart, currentRate, sortedRates);
            if (rate <= 0) continue;

            total += outstanding * rate / 100m * days / DaysPerYear;
        }

        return Math.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal RateAt(DateOnly date, decimal currentRate, List<RateChange> sortedRates)
    {
        if (sortedRates.Count == 0) return currentRate;

        // Before the first recorded change, the previous rate applied.
        var rate = sortedRates[0].PreviousRate;
        foreach (var change in sortedRates)
            if (date >= change.EffectiveDate) rate = change.NewRate;

        return rate;
    }
}
