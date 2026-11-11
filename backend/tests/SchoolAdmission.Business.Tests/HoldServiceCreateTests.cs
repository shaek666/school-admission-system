using Microsoft.Extensions.Logging.Abstractions;
using SchoolAdmission.Business.Holds;
using SchoolAdmission.Business.Students;
using SchoolAdmission.Business.Tests.Fakes;

namespace SchoolAdmission.Business.Tests;

public class HoldServiceCreateTests
{
    private const int DurationSeconds = 600;
    private const int Seats = 2;

    private static readonly StudentId Student = new("20152691234567890");

    private readonly CallLog _log = new();
    private readonly FakeClassRepository _classes;
    private readonly FakeHoldRepository _holds;
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly HoldService _service;

    public HoldServiceCreateTests()
    {
        _classes = new FakeClassRepository(_log);
        _holds = new FakeHoldRepository(_log);
        _unitOfWork = new FakeUnitOfWork(_log);
        _service = new HoldService(
            _unitOfWork, _classes, _holds, new HoldOptions { DurationSeconds = DurationSeconds }, NullLogger<HoldService>.Instance);
    }

    private static CancellationToken Ct => CancellationToken.None;

    private async Task<string> SeedClassAsync() =>
        (await _classes.AddAsync(Guid.NewGuid(), "Class 6", Seats, Ct)).Id.ToString();

    // Nothing commits or inserts, and every begun transaction is disposed, so it rolls back.
    private void AssertRolledBack(int expectedBegins)
    {
        Assert.Equal(expectedBegins, _log.Count(CallLog.Begin));
        Assert.Equal(expectedBegins, _log.Count(CallLog.Dispose));
        Assert.Equal(0, _log.Count(CallLog.Commit));
        Assert.Equal(0, _log.Count(CallLog.Insert));
    }

    private async Task AssertRejectedAsync(Task<HoldRecord> call, string code, int expectedBegins = 1)
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => call);
        Assert.Equal(code, ex.Code);
        AssertRolledBack(expectedBegins);
    }

    [Fact]
    public async Task Unknown_class_throws_CLASS_NOT_FOUND() =>
        await AssertRejectedAsync(_service.CreateAsync(Guid.NewGuid().ToString(), Student, Ct), "CLASS_NOT_FOUND");

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    [InlineData("3f2504e04f8911d39a0c0305e82c3301")]
    public async Task Malformed_class_id_throws_CLASS_NOT_FOUND_without_a_transaction(string classId) =>
        await AssertRejectedAsync(_service.CreateAsync(classId, Student, Ct), "CLASS_NOT_FOUND", expectedBegins: 0);

    [Fact]
    public async Task Stale_hold_of_student_is_expired_before_the_active_test_and_the_whole_sequence_is_ordered()
    {
        var classId = await SeedClassAsync();
        _holds.ActiveCount = Seats - 1;

        await _service.CreateAsync(classId, Student, Ct);

        string[] expected =
        [
            CallLog.Begin, CallLog.Lock, CallLog.ExpireStale, CallLog.HasActive,
            CallLog.CountActive, CallLog.Insert, CallLog.Commit, CallLog.Dispose,
        ];
        Assert.Equal(expected, _log.Entries);
    }

    [Fact]
    public async Task Active_hold_of_student_throws_ALREADY_APPLIED()
    {
        var classId = await SeedClassAsync();
        _holds.StudentHasActiveHold = true;

        await AssertRejectedAsync(_service.CreateAsync(classId, Student, Ct), "ALREADY_APPLIED");
    }

    [Fact]
    public async Task Full_class_throws_NO_SEATS_AVAILABLE()
    {
        var classId = await SeedClassAsync();
        _holds.ActiveCount = Seats;

        await AssertRejectedAsync(_service.CreateAsync(classId, Student, Ct), "NO_SEATS_AVAILABLE");
    }

    [Fact]
    public async Task Free_seat_inserts_and_commits_once()
    {
        var classId = await SeedClassAsync();
        _holds.ActiveCount = Seats - 1;

        var hold = await _service.CreateAsync(classId, Student, Ct);

        Assert.Equal(classId, hold.ClassId.ToString());
        Assert.Equal(HoldStatus.Held, hold.Status);
        Assert.Equal(1, _log.Count(CallLog.Begin));
        Assert.Equal(1, _log.Count(CallLog.Commit));
        Assert.Equal(1, _log.Count(CallLog.Insert));
        Assert.Equal(DurationSeconds, _holds.InsertedDurationSeconds);
    }

    [Fact]
    public async Task Nothing_is_committed_on_rejection()
    {
        var classId = await SeedClassAsync();
        _holds.StudentHasActiveHold = true;
        await Assert.ThrowsAsync<AppException>(() => _service.CreateAsync(classId, Student, Ct));

        _holds.StudentHasActiveHold = false;
        _holds.ActiveCount = Seats;
        await Assert.ThrowsAsync<AppException>(() => _service.CreateAsync(classId, Student, Ct));

        AssertRolledBack(expectedBegins: 2);
    }
}
