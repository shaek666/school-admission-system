using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace SchoolAdmission.Business.Students;

/// <summary>A student ID: exactly 17 ASCII digits. Never trimmed. Prints masked so logs never leak it.</summary>
public readonly partial record struct StudentId(string Value)
{
    private const int Length = 17;
    private const int VisibleDigits = 4;

    // \z, not $: "$" also matches before a trailing newline.
    [GeneratedRegex(@"^[0-9]{17}\z", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();

    public static bool TryParse(string? raw, [NotNullWhen(true)] out StudentId id)
    {
        if (raw is not null && raw.Length == Length && Pattern().IsMatch(raw))
        {
            id = new StudentId(raw);
            return true;
        }

        id = default;
        return false;
    }

    /// <summary>All but the last 4 digits replaced by stars.</summary>
    public string Masked => string.Concat(new string('*', Length - VisibleDigits), Value.AsSpan(Length - VisibleDigits));

    public override string ToString() => Masked;
}
