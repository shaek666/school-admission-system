namespace SchoolAdmission.Business.Tests.Fakes;

/// <summary>One ordered log shared by the fakes, so a test can assert the exact call sequence.</summary>
public sealed class CallLog
{
    public const string Begin = "Begin";
    public const string Lock = "Lock";
    public const string ExpireStale = "ExpireStale";
    public const string HasActive = "HasActive";
    public const string CountActive = "CountActive";
    public const string Insert = "Insert";
    public const string Commit = "Commit";
    public const string Dispose = "Dispose";

    private readonly List<string> _entries = [];

    public IReadOnlyList<string> Entries => _entries;

    public void Add(string call) => _entries.Add(call);

    public int Count(string call) => _entries.Count(entry => entry == call);
}
