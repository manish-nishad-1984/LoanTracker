namespace LoanTracker.Application.Services;

/// <summary>
/// Classifies a bank-statement line into direction, money group (Income/Expense/Transfer),
/// payment method, a spending/income category, and the merchant — from the narration text
/// and the debit/credit amounts. Falls back to intelligent inference when no keyword matches.
/// </summary>
public static class TransactionCategorizer
{
    public readonly record struct Result(
        string Direction, string Group, string PaymentMethod, string Category, string? Merchant);

    public static Result Classify(string narration, decimal deposit, decimal withdrawal)
    {
        var isIn = deposit > 0 && deposit >= withdrawal;
        var direction = isIn ? "In" : "Out";
        var n = (narration ?? string.Empty).ToUpperInvariant();
        var method = PaymentMethod(n);
        var merchant = ExtractMerchant(narration ?? string.Empty);

        bool Has(params string[] keys) => keys.Any(k => n.Contains(k));

        // ---------- INCOME ----------
        if (isIn)
        {
            string cat =
                Has("SALARY", "SAL CR", "SAL-", "SALCREDIT", "NEFT CR-INFO PVT", "PAYROLL") ? "Salary" :
                Has("INT.", "INTEREST", "CREDIT INTEREST", "INT PD", "INT CR") ? "Interest" :
                Has("REFUND", "REVERSAL", "REV-", "FAILED", "RETURN", "CHARGEBACK") ? "Refund" :
                Has("CASH DEP", "CDM", "CMS", "BY CASH", "CASH-") ? "Cash Deposit" :
                Has("DIVIDEND", "REDEMPTION", "MUTUAL", "ZERODHA", "GROWW", "MF ", "SIP REFUND") ? "Investment" :
                Has("CREDIT CARD", "CASHBACK") ? "Refund" :
                "Other Income";
            // generic transfers in
            if (cat == "Other Income" && Has("NEFT", "IMPS", "UPI", "RTGS")) cat = "Transfer In";
            return new Result(direction, "Income", method, cat, merchant);
        }

        // ---------- EXPENSE ----------
        var category =
            Has("SWIGGY", "ZOMATO", "RESTAURANT", "HOTEL", "CAFE", "DOMINO", "MCDONALD", "KFC", "PIZZA", "SANDWICH", "BAKERY", "FOOD", "EATERY", "DHABA") ? "Food" :
            Has("BIGBASKET", "BLINKIT", "ZEPTO", "DMART", "D MART", "GROFER", "GROCER", "RELIANCE FRESH", "RELIANCE SMART", "MORE RETAIL", "SUPERMARKET", "KIRANA") ? "Groceries" :
            Has("AMAZON", "FLIPKART", "MYNTRA", "AJIO", "MEESHO", "SNAPDEAL", "NYKAA", "LIFESTYLE", "SHOPPING", "MALL", "RETAIL", "STORE") ? "Shopping" :
            Has("HPCL", "IOCL", "BPCL", "HP PETRO", "INDIAN OIL", "BHARAT PETRO", "PETROL", "DIESEL", "FUEL", "SHELL", "NAYARA", "FILLING STATION") ? "Fuel" :
            Has("IRCTC", "UBER", "OLA ", "OLACABS", "RAPIDO", "MAKEMYTRIP", "REDBUS", "GOIBIBO", "INDIGO", "SPICEJET", "AIR INDIA", "VISTARA", "FLIGHT", "TRAVEL", "TOLL", "FASTAG", "PARKING") ? "Travel" :
            Has("ELECTRIC", "MSEB", "TORRENT POWER", "ADANI", "POWER BILL", "GAS BILL", "MAHANAGAR GAS", "RECHARGE", "JIO", "AIRTEL", "VODAFONE", " VI ", "BSNL", "BROADBAND", "WIFI", "WATER BILL", "DTH", "TATA SKY", "BILLPAY", "BILLDESK") ? "Utilities" :
            Has("PHARMACY", "MEDICAL", "HOSPITAL", "APOLLO", "PHARMEASY", "1MG", "MEDPLUS", "CLINIC", "DIAGNOSTIC", "LAB", "DOCTOR", "HEALTH") ? "Medical" :
            Has("SCHOOL", "COLLEGE", "UNIVERSITY", "TUITION", "COURSERA", "UDEMY", "BYJU", "UNACADEMY", "EXAM FEE", "ACADEMY", "EDUCATION") ? "Education" :
            Has("NETFLIX", "PRIME VIDEO", "HOTSTAR", "DISNEY", "SPOTIFY", "BOOKMYSHOW", "PVR", "INOX", "YOUTUBE PREMIUM", "GAMING", "STEAM", "PLAYSTATION") ? "Entertainment" :
            Has("INSURANCE", "INSUR", "LIC ", "LICI", "HDFC LIFE", "ICICI PRU", "MAX LIFE", "POLICY", "PREMIUM", "BAJAJ ALLIANZ", "STAR HEALTH") ? "Insurance" :
            Has("EMI", "BAJAJFIN", "BAJAJ FIN", "LOAN", "FINANCE", "HDFCLOAN", "INDUSIND", "HOMELOAN", "AUTOLOAN") ? "EMI" :
            Has("SIP", "MUTUAL", "ZERODHA", "GROWW", "UPSTOX", "COIN", "KUVERA", "NSE", "BSE", "STOCK", "EQUITY", "DEMAT", "ETMONEY", "INDMONEY") ? "Investments" :
            Has("CREDIT CARD", "CC PAYMENT", "CARD PAYMENT", "CCBP", "CRED ", "ONECARD", "RUPAY CC") ? "Credit Card" :
            Has("ATW", "NWD", "ATM", "EAW", "CASH WDL", "CASH WITHDRAW") ? "Cash Withdrawal" :
            Has("RENT") ? "Rent" :
            Has("GST", "INCOME TAX", "TDS", "ITNS", "ADVANCE TAX", "TAX PAY") ? "Tax" :
            Has("DONAT", "TEMPLE", "TRUST", "NGO", "CHARITY", "TIRUPATI") ? "Donation" :
            Has("AWS", "AMAZON WEB", "AZURE", "GOOGLE CLOUD", "DIGITALOCEAN", "GITHUB", "GODADDY", "HOSTINGER", "OFFICE365", "ADOBE") ? "Business" :
            Has("CHRG", "CHARGE", "FEE", "AMC", "GST ", "PENALTY", "SMS CHG", "MIN BAL") ? "Bank Charges" :
            null;

        if (category is null)
        {
            // Inference from payment method when no merchant keyword matched.
            category = method switch
            {
                "NEFT" or "IMPS" or "RTGS" => "Transfer",
                "ATM" => "Cash Withdrawal",
                "UPI" => "UPI",
                "POS/Card" => "Shopping",
                "ACH" => "EMI",
                _ => "Others"
            };
        }

        var group = category == "Transfer" ? "Transfer" : "Expense";
        return new Result(direction, group, method, category, merchant);
    }

    private static string PaymentMethod(string n)
    {
        if (n.StartsWith("UPI")) return "UPI";
        if (n.StartsWith("NEFT")) return "NEFT";
        if (n.StartsWith("IMPS")) return "IMPS";
        if (n.StartsWith("RTGS")) return "RTGS";
        if (n.StartsWith("POS") || n.StartsWith("ECOM") || n.Contains("PURCHASE")) return "POS/Card";
        if (n.StartsWith("ATW") || n.StartsWith("NWD") || n.StartsWith("EAW") || n.Contains("ATM")) return "ATM";
        if (n.StartsWith("ACH") || n.StartsWith("NACH")) return "ACH";
        if (n.StartsWith("CHQ") || n.Contains("CHEQUE") || n.Contains("CLG")) return "Cheque";
        return "Other";
    }

    /// <summary>Pull a human merchant/counterparty out of the narration.</summary>
    private static string? ExtractMerchant(string narration)
    {
        if (string.IsNullOrWhiteSpace(narration)) return null;
        var parts = narration.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        // HDFC UPI: "UPI-<MERCHANT>-<vpa>-<bank>-<ref>-<note>"
        if (parts.Length >= 2 && parts[0].StartsWith("UPI", StringComparison.OrdinalIgnoreCase))
            return Clean(parts[1]);
        // NEFT/IMPS: "NEFT CR-<IFSC>-<NAME>-..."  → name is usually the 3rd token
        if (parts.Length >= 3 && (parts[0].StartsWith("NEFT") || parts[0].StartsWith("IMPS") || parts[0].StartsWith("RTGS")))
            return Clean(parts[2]);
        if (parts.Length >= 2) return Clean(parts[1]);
        return Clean(parts[0]);
    }

    private static string? Clean(string s)
    {
        s = s.Trim();
        if (s.Length > 40) s = s[..40];
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
