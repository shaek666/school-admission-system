using SchoolAdmission.Business.Classes;

namespace SchoolAdmission.Business.Tests.Fakes;

public sealed class FakeClassRepository(CallLog? log = null) : IClassRepository
{
    private readonly Dictionary<Guid, ClassSeats> _classes = new();

    public Task<ClassSeats> AddAsync(Guid id, string name, int totalSeats, CancellationToken ct)
    {
        var created = new ClassSeats(id, name, totalSeats, 0, 0);
        _classes[id] = created;
        return Task.FromResult(created);
    }

    public Task<IReadOnlyList<ClassSeats>> ListAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ClassSeats>>(_classes.Values.ToList());

    public Task<ClassSeats?> GetAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_classes.GetValueOrDefault(id));

    public Task<int?> LockAsync(Guid classId, CancellationToken ct)
    {
        log?.Add(CallLog.Lock);
        return Task.FromResult(_classes.TryGetValue(classId, out var found) ? found.TotalSeats : (int?)null);
    }
}
