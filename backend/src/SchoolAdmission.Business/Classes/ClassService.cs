namespace SchoolAdmission.Business.Classes;

public sealed class ClassService(IClassRepository repository)
{
    private const int MaxNameLength = 200;
    private const int GuidLength = 36;
    private const int MinSeats = 1;
    private const int MaxSeats = 100_000;

    public async Task<ClassSeats> CreateAsync(CreateClassRequest request, CancellationToken ct)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Any(char.IsControl) || name.EnumerateRunes().Count() > MaxNameLength)
            throw Errors.Validation($"name must be 1 to {MaxNameLength} characters.");

        if (request.TotalSeats is not (>= MinSeats and <= MaxSeats and var seats))
            throw Errors.Validation($"totalSeats must be a whole number from {MinSeats} to {MaxSeats}.");

        return await repository.AddAsync(Guid.NewGuid(), name, seats, ct);
    }

    public Task<IReadOnlyList<ClassSeats>> ListAsync(CancellationToken ct) => repository.ListAsync(ct);

    public async Task<ClassSeats> GetAsync(string id, CancellationToken ct)
    {
        // TryParseExact trims surrounding white space, so the length is checked too.
        if (id.Length != GuidLength || !Guid.TryParseExact(id, "D", out var classId))
            throw Errors.ClassNotFound();

        return await repository.GetAsync(classId, ct) ?? throw Errors.ClassNotFound();
    }
}
