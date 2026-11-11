using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Business.Holds;
using SchoolAdmission.Business.Students;

namespace SchoolAdmission.DataAccess;

/// <summary>
/// Raw SQL on the context's open transaction. Every time comparison uses the database now().
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

    private sealed record Scalar<T>
    {
        public T Value { get; init; } = default!;
    }

    private const string ExpireStaleSql = $$"""
        UPDATE holds
        SET status = '{{HoldStatus.Expired}}'
        WHERE class_id = {0} AND student_id = {1}
          AND status = '{{HoldStatus.Held}}' AND expires_at <= now()
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
                    date_trunc('second', now()) + make_interval(secs => {2}::double precision), now())
            RETURNING id, class_id, student_id, status, expires_at
        )
        SELECT {{HoldSql.Alias}}.id AS "Id", {{HoldSql.Alias}}.class_id AS "ClassId", c.name AS "ClassName",
               {{HoldSql.Alias}}.student_id AS "StudentId", {{HoldSql.DerivedStatus}} AS "Status",
               {{HoldSql.Alias}}.expires_at AS "ExpiresAtUtc"
        FROM inserted {{HoldSql.Alias}}
        JOIN classes c ON c.id = {{HoldSql.Alias}}.class_id
        """;

    private const string ConfirmSql = $$"""
        UPDATE holds
        SET status = '{{HoldStatus.Confirmed}}'
        WHERE id = {0} AND status = '{{HoldStatus.Held}}' AND expires_at > now() AND student_id = {1}
        """;

    private const string CancelSql = $$"""
        UPDATE holds
        SET status = '{{HoldStatus.Cancelled}}'
        WHERE id = {0} AND status = '{{HoldStatus.Held}}' AND expires_at > now() AND student_id = {1}
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

    public async Task<bool> HasActiveAsync(Guid classId, StudentId student, CancellationToken ct)
    {
        var rows = await context.Database
            .SqlQueryRaw<Scalar<bool>>(HasActiveSql, classId, student.Value)
            .ToListAsync(ct);
        return rows.Single().Value;
    }

    public async Task<int> CountActiveAsync(Guid classId, CancellationToken ct)
    {
        var rows = await context.Database
            .SqlQueryRaw<Scalar<int>>(CountActiveSql, classId)
            .ToListAsync(ct);
        return rows.Single().Value;
    }

    public async Task<HoldRecord> InsertAsync(Guid classId, StudentId student, int durationSeconds, CancellationToken ct)
    {
        var rows = await context.Database
            .SqlQueryRaw<HoldRow>(InsertSql, classId, student.Value, durationSeconds)
            .ToListAsync(ct);
        return rows.Single().ToRecord();
    }

    public async Task<bool> TryConfirmAsync(Guid id, StudentId student, CancellationToken ct) =>
        await context.Database.ExecuteSqlRawAsync(ConfirmSql, [id, student.Value], ct) == 1;

    public async Task<bool> TryCancelAsync(Guid id, StudentId student, CancellationToken ct) =>
        await context.Database.ExecuteSqlRawAsync(CancelSql, [id, student.Value], ct) == 1;

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
