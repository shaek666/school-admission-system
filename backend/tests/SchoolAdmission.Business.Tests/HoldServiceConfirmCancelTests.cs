using Microsoft.Extensions.Logging.Abstractions;
using SchoolAdmission.Business.Holds;
using SchoolAdmission.Business.Students;
using SchoolAdmission.Business.Tests.Fakes;

namespace SchoolAdmission.Business.Tests;

public class HoldServiceConfirmCancelTests
{
    private static readonly StudentId Owner = new("20152691234567890");
    private static readonly StudentId Other = new("20152691234567891");

    private readonly CallLog _log = new();
    private readonly FakeHoldRepository _holds;
    private readonly HoldService _service;
    private readonly Guid _holdId = Guid.NewGuid();

    public HoldServiceConfirmCancelTests()
    {
        _holds = new FakeHoldRepository(_log);
        _service = new HoldService(
            new FakeUnitOfWork(_log), new FakeClassRepository(_log), _holds,
            new HoldOptions { DurationSeconds = 600 }, NullLogger<HoldService>.Instance);
    }

    private static CancellationToken Ct => CancellationToken.None;

    private string HoldId => _holdId.ToString();

    private void Store(string status, StudentId? owner = null) =>
        _holds.Stored = new HoldRecord(_holdId, Guid.NewGuid(), "Class 6", (owner ?? Owner).Value, status, DateTime.UtcNow);

    private async Task AssertCodeAsync(Func<Task> call, string code)
    {
        var ex = await Assert.ThrowsAsync<AppException>(call);
        Assert.Equal(code, ex.Code);
        Assert.Equal(0, _log.Count(CallLog.Commit));
        Assert.Equal(_log.Count(CallLog.Begin), _log.Count(CallLog.Dispose));
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    public async Task Malformed_id_is_HOLD_NOT_FOUND_without_a_transaction(string id)
    {
        await AssertCodeAsync(() => _service.ConfirmAsync(id, Owner, Ct), "HOLD_NOT_FOUND");
        await AssertCodeAsync(() => _service.CancelAsync(id, Owner, Ct), "HOLD_NOT_FOUND");
        Assert.Equal(0, _log.Count(CallLog.Begin));
    }

    [Fact]
    public async Task Confirm_matching_update_commits_without_the_table()
    {
        _holds.UpdateMatches = true;
        Store(HoldStatus.Confirmed);

        var hold = await _service.ConfirmAsync(HoldId, Owner, Ct);

        Assert.Equal(HoldStatus.Confirmed, hold.Status);
        Assert.Equal(1, _log.Count(CallLog.Commit));
    }

    [Fact]
    public Task Confirm_not_found() =>
        AssertCodeAsync(() => _service.ConfirmAsync(HoldId, Owner, Ct), "HOLD_NOT_FOUND");

    [Theory]
    [InlineData(HoldStatus.Held)]
    [InlineData(HoldStatus.Confirmed)]
    [InlineData(HoldStatus.Cancelled)]
    [InlineData(HoldStatus.Expired)]
    public Task Confirm_other_owner_is_NOT_HOLD_OWNER_in_every_state(string status)
    {
        Store(status, Other);
        return AssertCodeAsync(() => _service.ConfirmAsync(HoldId, Owner, Ct), "NOT_HOLD_OWNER");
    }

    [Fact]
    public async Task Confirm_confirmed_same_owner_returns_it_without_commit()
    {
        Store(HoldStatus.Confirmed);

        var hold = await _service.ConfirmAsync(HoldId, Owner, Ct);

        Assert.Equal(HoldStatus.Confirmed, hold.Status);
        Assert.Equal(0, _log.Count(CallLog.Commit));
    }

    [Theory]
    [InlineData(HoldStatus.Cancelled, "HOLD_CANCELLED")]
    [InlineData(HoldStatus.Expired, "HOLD_EXPIRED")]
    public Task Confirm_dead_states(string status, string code)
    {
        Store(status);
        return AssertCodeAsync(() => _service.ConfirmAsync(HoldId, Owner, Ct), code);
    }

    [Fact]
    public async Task Cancel_matching_update_commits()
    {
        _holds.UpdateMatches = true;

        await _service.CancelAsync(HoldId, Owner, Ct);

        Assert.Equal(1, _log.Count(CallLog.Commit));
        Assert.Equal(0, _log.Count(CallLog.Get));
    }

    [Fact]
    public Task Cancel_not_found() =>
        AssertCodeAsync(() => _service.CancelAsync(HoldId, Owner, Ct), "HOLD_NOT_FOUND");

    [Theory]
    [InlineData(HoldStatus.Held)]
    [InlineData(HoldStatus.Confirmed)]
    [InlineData(HoldStatus.Cancelled)]
    [InlineData(HoldStatus.Expired)]
    public Task Cancel_other_owner_is_NOT_HOLD_OWNER_in_every_state(string status)
    {
        Store(status, Other);
        return AssertCodeAsync(() => _service.CancelAsync(HoldId, Owner, Ct), "NOT_HOLD_OWNER");
    }

    [Fact]
    public async Task Cancel_cancelled_same_owner_is_no_error()
    {
        Store(HoldStatus.Cancelled);

        await _service.CancelAsync(HoldId, Owner, Ct);

        Assert.Equal(0, _log.Count(CallLog.Commit));
    }

    [Theory]
    [InlineData(HoldStatus.Confirmed, "HOLD_CONFIRMED")]
    [InlineData(HoldStatus.Expired, "HOLD_EXPIRED")]
    public Task Cancel_dead_states(string status, string code)
    {
        Store(status);
        return AssertCodeAsync(() => _service.CancelAsync(HoldId, Owner, Ct), code);
    }
}
