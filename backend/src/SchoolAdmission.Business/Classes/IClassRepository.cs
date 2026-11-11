namespace SchoolAdmission.Business.Classes;

public interface IClassRepository
{
    Task<ClassSeats> AddAsync(Guid id, string name, int totalSeats, CancellationToken ct);

    Task<IReadOnlyList<ClassSeats>> ListAsync(CancellationToken ct);

    Task<ClassSeats?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Locks the class row (SELECT ... FOR UPDATE) until the open transaction ends and returns its total seats.
    /// Returns null when the class does not exist. Call it inside an <see cref="IUnitOfWork"/> transaction.
    /// </summary>
    Task<int?> LockAsync(Guid classId, CancellationToken ct);
}
