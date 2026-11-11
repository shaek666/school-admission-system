namespace SchoolAdmission.Business;

/// <summary>Strict parsing for route ids.</summary>
public static class Ids
{
    private const int GuidLength = 36;

    /// <summary>
    /// Accepts only the dashed 36 character form. TryParseExact trims surrounding white space,
    /// so the length is checked first.
    /// </summary>
    public static bool TryParse(string? raw, out Guid id)
    {
        if (raw is { Length: GuidLength } && Guid.TryParseExact(raw, "D", out id))
        {
            return true;
        }

        id = default;
        return false;
    }
}
