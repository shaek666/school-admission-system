namespace SchoolAdmission.DataAccess.Entities;

public sealed class HoldEntity
{
    public Guid Id { get; set; }

    public Guid ClassId { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
