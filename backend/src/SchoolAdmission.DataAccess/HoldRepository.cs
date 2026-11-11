using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Business.Holds;
using SchoolAdmission.Business.Students;

namespace SchoolAdmission.DataAccess;

/// <summary>
/// Raw SQL on the context's open transaction. Every time comparison uses the database statement_timestamp(): the time the statement started,
/// not the transaction, so a wait for the class lock never shortens or kills a hold.
/// Placeholders {0}, {1} are bound as parameters by EF, so no value is ever concatenated into the SQL.
/// </summary>
public sealed class HoldRepository(AdmissionDbContext context) : IHoldRepository
{
    private sealed record HoldRow
    {
        public Guid Id { get; init; }

        public Guid ClassId { get; init; }

        public string ClassName { get; init; } = string.Empty;

        public string StudentId { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public DateTime ExpiresAtUtc { get; init; }

        public HoldRecord ToRecord() =>
            new(Id, ClassId, ClassName, StudentId, Status, DateTime.SpecifyKind(ExpiresAtUtc, DateTimeKind.Utc));
    }


    // The sweep. The per-student version below adds a filter to the same statement.
    private const string ExpireAllStaleSql = $$"""
        UPDATE holds {{HoldSql.Alias}}
        SET status = '{{HoldStatus.Expired}}'
        WHERE {{HoldSql.IsStaleHeld}}
        """;

    private const string ExpireStaleSql = ExpireAllStaleSql + $$"""

          AND {{HoldSql.Alias}}.class_id = {0} AND {{HoldSql.Alias}}.student_id = {1}
        """;

    private const string HasActiveSql = $$"""
        SELECT EXISTS (
            SELECT 1 FROM holds {{HoldSql.Alias}}
            WHERE {{HoldSql.Alias}}.class_id = {0} AND {{HoldSql.Alias}}.student_id = {1} AND {{HoldSql.IsLive}}
        ) AS "Value"
        """;

    private const string CountActiveSql = $$"""
        SELECT COUNT(*)::int AS "Value"
        FROM holds {{HoldSql.Alias}}
        WHERE {{HoldSql.Alias}}.class_id = {0} AND {{HoldSql.IsLive}}
        """;

    // A data-modifying CTE, so the new row and the class name come back in one round trip.
    // The status is derived the same way as on every read, so an already expired hold never reads as HELD.
    private const string InsertSql = $$"""
        WITH inserted AS (
            INSERT INTO holds (id, class_id, student_id, status, expires_at, created_at)
            VALUES (gen_random_uuid(), {0}, {1}, '{{HoldStatus.Held}}',
                    date_trunc('second', statement_timestamp()) + make_interval(secs => {2}::double precision), statement_timestamp())
            RETURNING id, class_id, student_id, status, expires_at
        )
        SELECT {{HoldSql.Alias}}.id AS "Id", {{HoldSql.Alias}}.class_id AS "ClassId", c.name AS "ClassName",
               {{HoldSql.Alias}}.student_id AS "StudentId", {{HoldSql.DerivedStatus}} AS "Status",
               {{HoldSql.Alias}}.expires_at AS "ExpiresAtUtc"
        FROM inserted {{HoldSql.Alias}}
        JOIN classes c ON c.id = {{HoldSql.Alias}}.class_id
        """;

    private const string LockClassSql = $$"""
        SELECT c.total_seats AS "Value"
        FROM classes c
        JOIN holds {{HoldSql.Alias}} ON {{HoldSql.Alias}}.class_id = c.id
        WHERE {{HoldSql.Alias}}.id = {0}
        FOR UPDATE OF c
        """;

    // Moves an open HELD hold owned by the student to the status in {2}. Confirm and cancel share it.
    private const string TransitionSql = $$"""
        UPDATE holds {{HoldSql.Alias}}
        SET status = {2}
        WHERE {{HoldSql.Alias}}.id = {0} AND {{HoldSql.Alias}}.student_id = {1} AND {{HoldSql.IsOpenHeld}}
        """;

    private const string SelectSql = $$"""
        SELECT {{HoldSql.Alias}}.id AS "Id", {{HoldSql.Alias}}.class_id AS "ClassId", c.name AS "ClassName",
               {{HoldSql.Alias}}.student_id AS "StudentId", {{HoldSql.DerivedStatus}} AS "Status",
               {{HoldSql.Alias}}.expires_at AS "ExpiresAtUtc"
        FROM holds {{HoldSql.Alias}}
        JOIN classes c ON c.id = {{HoldSql.Alias}}.class_id
        """;

    private const string GetSql = SelectSql + $$"""

        WHERE {{HoldSql.Alias}}.id = {0}
        """;

    private const string ListForStudentSql = SelectSql + $$"""

        WHERE {{HoldSql.Alias}}.student_id = {0}
        ORDER BY {{HoldSql.Alias}}.created_at DESC, {{HoldSql.Alias}}.id DESC
        """;

    public async Task ExpireStaleForStudentAsync(Guid classId, StudentId student, CancellationToken ct) =>
        await context.Database.ExecuteSqlRawAsync(ExpireStaleSql, [classId, student.Value], ct);

    public async Task<int> ExpireAllStaleAsync(CancellationToken ct) =>
        await context.Database.ExecuteSqlRawAsync(ExpireAllStaleSql, ct);

    public async Task<bool> HasActiveAsync(Guid classId, StudentId student, CancellationToken ct)
    {
        var rows = await context.Database
            .SqlQueryRaw<bool>(HasActiveSql, classId, student.Value)
            .ToListAsync(ct);
        return rows.Single();
    }

    public async Task<int> CountActiveAsync(Guid classId, CancellationToken ct)
    {
        var rows = await context.Database
            .SqlQueryRaw<int>(CountActiveSql, classId)
            .ToListAsync(ct);
        return rows.Single();
    }

    public async Task<HoldRecord> InsertAsync(Guid classId, StudentId student, int durationSeconds, CancellationToken ct)
    {
        var rows = await context.Database
            .SqlQueryRaw<HoldRow>(InsertSql, classId, student.Value, durationSeconds)
            .ToListAsync(ct);
        return rows.Single().ToRecord();
    }

    public async Task<bool> LockClassAsync(Guid holdId, CancellationToken ct)
    {
        // FOR UPDATE OF c locks only the class row, the same row a create locks, so the two are ordered.
        var rows = await context.Database
            .SqlQueryRaw<int>(LockClassSql, holdId)
            .ToListAsync(ct);
        return rows.Count == 1;
    }

    public Task<bool> TryConfirmAsync(Guid id, StudentId student, CancellationToken ct) =>
        TransitionAsync(id, student, HoldStatus.Confirmed, ct);

    public Task<bool> TryCancelAsync(Guid id, StudentId student, CancellationToken ct) =>
        TransitionAsync(id, student, HoldStatus.Cancelled, ct);

    private async Task<bool> TransitionAsync(Guid id, StudentId student, string status, CancellationToken ct) =>
        await context.Database.ExecuteSqlRawAsync(TransitionSql, [id, student.Value, status], ct) == 1;

    public async Task<HoldRecord?> GetAsync(Guid id, CancellationToken ct)
    {
        var rows = await context.Database.SqlQueryRaw<HoldRow>(GetSql, id).ToListAsync(ct);
        return rows.SingleOrDefault()?.ToRecord();
    }

    public async Task<IReadOnlyList<HoldRecord>> ListForStudentAsync(StudentId student, CancellationToken ct)
    {
        var rows = await context.Database.SqlQueryRaw<HoldRow>(ListForStudentSql, student.Value).ToListAsync(ct);
        return rows.ConvertAll(r => r.ToRecord());
    }
}
