using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Business.Classes;
using SchoolAdmission.DataAccess.Entities;

namespace SchoolAdmission.DataAccess;

public sealed class ClassRepository(AdmissionDbContext context) : IClassRepository
{
    // Seat counts use the database clock, so an expired hold frees its seat before the sweep runs.
    private sealed class SeatRow
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int TotalSeats { get; set; }

        public int HeldSeats { get; set; }

        public int ConfirmedSeats { get; set; }

        public ClassSeats ToSeats() => new(Id, Name, TotalSeats, HeldSeats, ConfirmedSeats);
    }

    public async Task<ClassSeats> AddAsync(Guid id, string name, int totalSeats, CancellationToken ct)
    {
        context.Classes.Add(new ClassEntity { Id = id, Name = name, TotalSeats = totalSeats });
        await context.SaveChangesAsync(ct);
        return new ClassSeats(id, name, totalSeats, 0, 0);
    }

    public async Task<IReadOnlyList<ClassSeats>> ListAsync(CancellationToken ct)
    {
        var rows = await context.Database.SqlQuery<SeatRow>($"""
            SELECT c.id AS "Id", c.name AS "Name", c.total_seats AS "TotalSeats",
                   (COUNT(h.id) FILTER (WHERE h.status = 'HELD' AND h.expires_at > now()))::int AS "HeldSeats",
                   (COUNT(h.id) FILTER (WHERE h.status = 'CONFIRMED'))::int AS "ConfirmedSeats"
            FROM classes c
            LEFT JOIN holds h ON h.class_id = c.id
            GROUP BY c.id, c.name, c.total_seats
            ORDER BY c.name, c.id
            """).ToListAsync(ct);
        return rows.ConvertAll(row => row.ToSeats());
    }

    public async Task<ClassSeats?> GetAsync(Guid id, CancellationToken ct)
    {
        var rows = await context.Database.SqlQuery<SeatRow>($"""
            SELECT c.id AS "Id", c.name AS "Name", c.total_seats AS "TotalSeats",
                   (COUNT(h.id) FILTER (WHERE h.status = 'HELD' AND h.expires_at > now()))::int AS "HeldSeats",
                   (COUNT(h.id) FILTER (WHERE h.status = 'CONFIRMED'))::int AS "ConfirmedSeats"
            FROM classes c
            LEFT JOIN holds h ON h.class_id = c.id
            WHERE c.id = {id}
            GROUP BY c.id, c.name, c.total_seats
            """).ToListAsync(ct);
        return rows.Count == 0 ? null : rows[0].ToSeats();
    }
}
