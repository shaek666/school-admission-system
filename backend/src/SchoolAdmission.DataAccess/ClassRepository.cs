using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Business.Classes;
using SchoolAdmission.Business.Holds;
using SchoolAdmission.DataAccess.Entities;

namespace SchoolAdmission.DataAccess;

public sealed class ClassRepository(AdmissionDbContext context) : IClassRepository
{
    // Seat counts use the database clock, so an expired hold frees its seat before the sweep runs.
    private sealed record SeatRow
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public int TotalSeats { get; init; }

        public int HeldSeats { get; init; }

        public int ConfirmedSeats { get; init; }

        public ClassSeats ToSeats() => new(Id, Name, TotalSeats, HeldSeats, ConfirmedSeats);
    }

    public async Task<ClassSeats> AddAsync(Guid id, string name, int totalSeats, CancellationToken ct)
    {
        context.Classes.Add(new ClassEntity { Id = id, Name = name, TotalSeats = totalSeats });
        await context.SaveChangesAsync(ct);
        return new ClassSeats(id, name, totalSeats, 0, 0);
    }

    // SeatSelect must not contain '{' or '}' because it becomes a composite format string.
    // Only live holds are joined, so expired and cancelled rows are never grouped.
    private const string SeatSelect = $"""
        SELECT c.id AS "Id", c.name AS "Name", c.total_seats AS "TotalSeats",
               (COUNT(h.id) FILTER (WHERE h.status = '{HoldStatus.Held}'))::int AS "HeldSeats",
               (COUNT(h.id) FILTER (WHERE h.status = '{HoldStatus.Confirmed}'))::int AS "ConfirmedSeats"
        FROM classes c
        LEFT JOIN holds h ON h.class_id = c.id AND {HoldSql.IsLive}
        """;

    private const string SeatGroupBy = "GROUP BY c.id, c.name, c.total_seats";

    private static FormattableString Query(string whereClause, string orderBy, params object[] args) =>
        FormattableStringFactory.Create($"{SeatSelect}\n{whereClause}\n{SeatGroupBy}\n{orderBy}", args);

    public async Task<IReadOnlyList<ClassSeats>> ListAsync(CancellationToken ct)
    {
        var rows = await context.Database
            .SqlQuery<SeatRow>(Query(string.Empty, "ORDER BY c.name, c.id"))
            .ToListAsync(ct);
        return rows.ConvertAll(row => row.ToSeats());
    }

    public async Task<ClassSeats?> GetAsync(Guid id, CancellationToken ct)
    {
        var rows = await context.Database
            .SqlQuery<SeatRow>(Query("WHERE c.id = {0}", string.Empty, id))
            .ToListAsync(ct);
        return rows.SingleOrDefault()?.ToSeats();
    }

    // Runs on the context's open transaction. The lock lasts until it ends. No aggregate runs under the lock.
    public async Task<int?> LockAsync(Guid classId, CancellationToken ct)
    {
        var rows = await context.Database
            .SqlQueryRaw<int>("SELECT total_seats AS \"Value\" FROM classes WHERE id = {0} FOR UPDATE", classId)
            .ToListAsync(ct);
        return rows.Count == 0 ? null : rows[0];
    }
}
