using SchoolAdmission.Business.Holds;

namespace SchoolAdmission.DataAccess;

/// <summary>The one definition of a live hold. Class seat counts and the hold repository both use it.</summary>
internal static class HoldSql
{
    /// <summary>Alias of the holds table in every query that uses <see cref="IsLive"/>.</summary>
    public const string Alias = "h";

    /// <summary>CONFIRMED, or HELD and not yet expired by database time.</summary>
    public const string IsLive = $"""
        ({Alias}.status = '{HoldStatus.Confirmed}'
         OR ({Alias}.status = '{HoldStatus.Held}' AND {Alias}.expires_at > now()))
        """;

    /// <summary>The status as clients see it: a HELD hold past its expiry reads as EXPIRED, by database time.</summary>
    public const string DerivedStatus = $"""
        CASE WHEN {Alias}.status = '{HoldStatus.Held}' AND {Alias}.expires_at <= now()
             THEN '{HoldStatus.Expired}' ELSE {Alias}.status END
        """;
}
