namespace SchoolAdmission.DataAccess.Entities;

public sealed class ClassEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int TotalSeats { get; set; }
}
