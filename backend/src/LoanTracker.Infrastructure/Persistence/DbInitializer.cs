using LoanTracker.Domain.Entities;
using LoanTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LoanTracker.Infrastructure.Persistence;

public static class DbInitializer
{
    /// <summary>
    /// Seeds sample data (2 person lenders, 2 bank lenders, 4 loans, multiple payments)
    /// only when the database has no lenders yet. Safe to call on every startup.
    /// </summary>
    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger, CancellationToken ct = default)
    {
        if (await db.Lenders.IgnoreQueryFilters().AnyAsync(ct))
        {
            logger.LogInformation("Database already seeded — skipping.");
            return;
        }

        logger.LogInformation("Seeding sample data...");

        // ── Lenders ──────────────────────────────────────────────
        var rajesh = new Lender
        {
            LenderType = LenderType.Person,
            Name = "Rajesh Sharma",
            Phone = "+91-98765-43210",
            Email = "rajesh.sharma@gmail.com",
            Address = "B-12, Vastrapur, Ahmedabad, Gujarat - 380015",
            Notes = "Family friend. Lends money informally at low interest."
        };
        var priya = new Lender
        {
            LenderType = LenderType.Person,
            Name = "Priya Mehta",
            Phone = "+91-91234-56789",
            Email = "priya.mehta@yahoo.com",
            Address = "C-45, Satellite, Ahmedabad, Gujarat - 380015",
            Notes = "Colleague. Short-term lending only."
        };
        var hdfc = new Lender
        {
            LenderType = LenderType.Bank,
            Name = "HDFC Bank — Home Loan",
            ContactName = "Amit Joshi (Relationship Manager)",
            Phone = "+91-79-4000-1111",
            Email = "homeloan.ahmedabad@hdfcbank.com",
            Address = "SG Highway Branch, Ahmedabad, Gujarat",
            BankName = "HDFC Bank Ltd",
            AccountNumber = "HDFC00123456",
            IfscCode = "HDFC0001234",
            Notes = "Home loan account. Fixed-rate product."
        };
        var sbi = new Lender
        {
            LenderType = LenderType.Bank,
            Name = "SBI — Personal Loan",
            ContactName = "Branch Manager",
            Phone = "+91-79-2656-7890",
            Email = "sbi.navrangpura@sbi.co.in",
            Address = "Navrangpura Branch, Ahmedabad, Gujarat",
            BankName = "State Bank of India",
            AccountNumber = "SBI987654321",
            IfscCode = "SBIN0050123",
            Notes = "SBI personal loan — reducing balance interest."
        };

        db.Lenders.AddRange(rajesh, priya, hdfc, sbi);

        // ── Loans ────────────────────────────────────────────────
        var rajeshLoan = new Loan
        {
            Lender = rajesh,
            LoanNumber = "RAJESH/2023/01",
            Description = "Emergency home repair loan from Rajesh bhai",
            PrincipalAmount = 200000m,
            CurrentInterestRate = 9.0m,
            InterestCalculationType = InterestCalculationType.Simple,
            PaymentFrequency = PaymentFrequency.Irregular,
            IsEmiLoan = false,
            LoanStartDate = new DateOnly(2023, 6, 1),
            Status = LoanStatus.Active,
            Notes = "Interest to be paid monthly; principal whenever possible."
        };
        var priyaLoan = new Loan
        {
            Lender = priya,
            LoanNumber = "PRIYA/2024/01",
            Description = "Short-term loan from Priya — office laptop",
            PrincipalAmount = 50000m,
            CurrentInterestRate = 0m,
            InterestCalculationType = InterestCalculationType.None,
            PaymentFrequency = PaymentFrequency.LumpSum,
            IsEmiLoan = false,
            LoanStartDate = new DateOnly(2024, 1, 15),
            LoanEndDate = new DateOnly(2024, 7, 15),
            Status = LoanStatus.Closed,
            ClosureDate = new DateOnly(2024, 7, 14),
            Notes = "Zero interest friendly loan. Closed on time."
        };
        var hdfcLoan = new Loan
        {
            Lender = hdfc,
            LoanNumber = "HDFC/HL/AHM/2022/5678",
            Description = "HDFC Home Loan — 3BHK apartment purchase",
            PrincipalAmount = 3500000m,
            CurrentInterestRate = 8.5m,
            InterestCalculationType = InterestCalculationType.ReducingBalance,
            PaymentFrequency = PaymentFrequency.Monthly,
            IsEmiLoan = true,
            EmiAmount = 32000m,
            LoanStartDate = new DateOnly(2022, 3, 1),
            LoanEndDate = new DateOnly(2042, 3, 1),
            Status = LoanStatus.Active,
            Notes = "20-year home loan. EMI auto-debit on 5th of each month."
        };
        var sbiLoan = new Loan
        {
            Lender = sbi,
            LoanNumber = "SBI/PL/2024/9012",
            Description = "SBI Personal Loan — vehicle purchase",
            PrincipalAmount = 400000m,
            CurrentInterestRate = 12.0m,
            InterestCalculationType = InterestCalculationType.ReducingBalance,
            PaymentFrequency = PaymentFrequency.Monthly,
            IsEmiLoan = true,
            EmiAmount = 8900m,
            LoanStartDate = new DateOnly(2024, 3, 1),
            LoanEndDate = new DateOnly(2028, 3, 1),
            Status = LoanStatus.Active,
            Notes = "4-year personal loan. EMI on 1st of each month."
        };

        db.Loans.AddRange(rajeshLoan, priyaLoan, hdfcLoan, sbiLoan);

        // ── Interest rate history ────────────────────────────────
        db.LoanInterestRateHistories.Add(new LoanInterestRateHistory
        {
            Loan = hdfcLoan,
            PreviousRate = 8.75m,
            NewRate = 8.5m,
            EffectiveDate = new DateOnly(2023, 10, 1),
            Reason = "RBI repo rate cut — HDFC passed on benefit to floating rate borrowers."
        });

        // ── Payments ─────────────────────────────────────────────
        LoanPayment P(Loan loan, DateOnly date, decimal principal, decimal interest,
            PaymentMode mode, string? remarks = null, string? reference = null, decimal penalty = 0)
            => new()
            {
                Loan = loan,
                PaymentDate = date,
                PrincipalAmount = principal,
                InterestAmount = interest,
                PenaltyAmount = penalty,
                TotalAmount = principal + interest + penalty,
                PaymentMode = mode,
                ReferenceNumber = reference,
                Remarks = remarks
            };

        db.LoanPayments.AddRange(
            // Rajesh loan — mix of interest-only, principal-only, and mixed payments
            P(rajeshLoan, new DateOnly(2023, 7, 1), 0, 1500, PaymentMode.Cash, "July interest @ 9% p.a. on 2L"),
            P(rajeshLoan, new DateOnly(2023, 8, 1), 0, 1500, PaymentMode.Cash, "August interest"),
            P(rajeshLoan, new DateOnly(2023, 9, 1), 20000, 1500, PaymentMode.BankTransfer, "September — 20k principal + interest"),
            P(rajeshLoan, new DateOnly(2023, 10, 3), 0, 1350, PaymentMode.Upi, "October interest on 180000 outstanding"),
            P(rajeshLoan, new DateOnly(2023, 11, 15), 30000, 0, PaymentMode.BankTransfer, "Principal repayment only"),
            P(rajeshLoan, new DateOnly(2024, 1, 1), 10000, 1125, PaymentMode.Cash, "Jan — 10k principal + interest on 150k"),
            P(rajeshLoan, new DateOnly(2024, 3, 1), 0, 1050, PaymentMode.Upi, "March interest on 140k outstanding"),
            P(rajeshLoan, new DateOnly(2024, 6, 1), 40000, 0, PaymentMode.BankTransfer, "Large principal payment — bonus received"),

            // Priya loan — fully repaid
            P(priyaLoan, new DateOnly(2024, 4, 10), 25000, 0, PaymentMode.BankTransfer, "First partial repayment"),
            P(priyaLoan, new DateOnly(2024, 7, 14), 25000, 0, PaymentMode.Upi, "Final repayment — loan closed"),

            // HDFC home loan — 6 EMIs
            P(hdfcLoan, new DateOnly(2024, 10, 5), 7258, 24742, PaymentMode.BankTransfer, "October EMI", "HDFC10OCT24"),
            P(hdfcLoan, new DateOnly(2024, 11, 5), 7310, 24690, PaymentMode.BankTransfer, "November EMI", "HDFC10NOV24"),
            P(hdfcLoan, new DateOnly(2024, 12, 5), 7363, 24637, PaymentMode.BankTransfer, "December EMI", "HDFC10DEC24"),
            P(hdfcLoan, new DateOnly(2025, 1, 5), 7415, 24585, PaymentMode.BankTransfer, "January EMI", "HDFC10JAN25"),
            P(hdfcLoan, new DateOnly(2025, 2, 5), 7468, 24532, PaymentMode.BankTransfer, "February EMI", "HDFC10FEB25"),
            P(hdfcLoan, new DateOnly(2025, 3, 5), 7521, 24479, PaymentMode.BankTransfer, "March EMI", "HDFC10MAR25"),

            // SBI personal loan — 5 EMIs
            P(sbiLoan, new DateOnly(2024, 4, 1), 4900, 4000, PaymentMode.BankTransfer, "April EMI", "SBI01APR24"),
            P(sbiLoan, new DateOnly(2024, 5, 1), 4949, 3951, PaymentMode.BankTransfer, "May EMI", "SBI01MAY24"),
            P(sbiLoan, new DateOnly(2024, 6, 1), 4998, 3902, PaymentMode.BankTransfer, "June EMI", "SBI01JUN24"),
            P(sbiLoan, new DateOnly(2024, 7, 1), 5048, 3852, PaymentMode.BankTransfer, "July EMI", "SBI01JUL24"),
            P(sbiLoan, new DateOnly(2024, 8, 1), 5099, 3801, PaymentMode.BankTransfer, "August EMI", "SBI01AUG24")
        );

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seed data inserted successfully.");
    }
}
