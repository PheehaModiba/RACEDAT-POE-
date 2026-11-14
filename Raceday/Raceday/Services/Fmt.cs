namespace RaceDay.Web.Services;

/// <summary>Small display helpers used by views.</summary>
public static class Fmt
{
    // 1 -> 1st, 2 -> 2nd, 11 -> 11th, 47 -> 47th
    public static string Ordinal(int n)
    {
        if (n % 100 is >= 11 and <= 13) return n + "th";
        return n + (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
    }

    public static string Km(decimal d) => d % 1 == 0 ? $"{d:0}" : $"{d:0.0#}";

    public static string StatusClass(string? status) => (status ?? "").Trim().ToLowerInvariant() switch
    {
        "confirmed" => "status-confirmed",
        "pending" => "status-pending",
        "cancelled" or "canceled" => "status-cancelled",
        _ => "status-other"
    };
}
