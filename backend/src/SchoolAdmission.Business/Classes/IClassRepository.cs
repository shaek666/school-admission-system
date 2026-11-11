namespace SchoolAdmission.Business.Classes;

public interface IClassRepository
{
    Task<ClassSeats> AddAsync(Guid id, string name, int totalSeats, CancellationToken ct);

    Task<IReadOnlyList<ClassSeats>> ListAsync(CancellationToken ct);

    Task<ClassSeats?> GetAsync(Guid id, CancellationToken ct);
}
