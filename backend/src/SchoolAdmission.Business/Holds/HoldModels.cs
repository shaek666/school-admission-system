namespace SchoolAdmission.Business.Holds;

/// <summary>A hold with its class name. A HELD hold past its expiry reads as EXPIRED (database time).</summary>
public record HoldRecord(Guid Id, Guid ClassId, string ClassName, string StudentId, string Status, DateTime ExpiresAtUtc);
