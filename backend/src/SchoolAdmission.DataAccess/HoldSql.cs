using SchoolAdmission.Business.Holds;

namespace SchoolAdmission.DataAccess;

/// <summary>The one definition of a live hold. Class seat counts and the hold repository both use it.</summary>
internal static class HoldSql
{
    /// <summary>Alias of the holds table in every query that uses <see cref="IsLive"/>.</summary>
    public const string Alias = "h";

    /// <summary>HELD and not yet expired by database time. Only such a hold can be confirmed or cancelled.</summary>
    public const string IsOpenHeld = $"({Alias}.status = '{HoldStatus.Held}' AND {Alias}.expires_at > statement_timestamp())";

    /// <summary>HELD but past its expiry by database time. The complement of <see cref="IsOpenHeld"/> among HELD rows.</summary>
    public const string IsStaleHeld = $"({Alias}.status = '{HoldStatus.Held}' AND {Alias}.expires_at <= statement_timestamp())";

    /// <summary>CONFIRMED, or HELD and not yet expired by database time.</summary>
    public const string IsLive = $"({Alias}.status = '{HoldStatus.Confirmed}' OR {IsOpenHeld})";

    /// <summary>The status as clients see it: a HELD hold past its expiry reads as EXPIRED, by database time.</summary>
    public const string DerivedStatus = $"CASE WHEN {IsStaleHeld} THEN '{HoldStatus.Expired}' ELSE {Alias}.status END";
}
