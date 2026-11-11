using SchoolAdmission.Business.Classes;
using SchoolAdmission.Business.Tests.Fakes;

namespace SchoolAdmission.Business.Tests;

public class ClassServiceTests
{
    private readonly FakeClassRepository _repository = new();
    private readonly ClassService _service;

    public ClassServiceTests() => _service = new ClassService(_repository);

    private static CancellationToken Ct => CancellationToken.None;

    private static async Task AssertValidationAsync(Task<ClassSeats> call)
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => call);
        Assert.Equal(ErrorKind.Validation, ex.Kind);
        Assert.Equal("VALIDATION_ERROR", ex.Code);
    }

    [Fact]
    public async Task Name_is_trimmed_before_save()
    {
        var created = await _service.CreateAsync(new CreateClassRequest("  Class 6  ", 30), Ct);

        Assert.Equal("Class 6", created.Name);
        Assert.Equal(30, created.AvailableSeats);
    }

    [Theory, InlineData(null), InlineData(""), InlineData("   ")]
    public Task Missing_name_is_rejected(string? name) =>
        AssertValidationAsync(_service.CreateAsync(new CreateClassRequest(name, 10), Ct));

    [Fact]
    public Task Name_of_201_chars_is_rejected() =>
        AssertValidationAsync(_service.CreateAsync(new CreateClassRequest(new string('a', 201), 10), Ct));

    [Fact]
    public async Task Name_of_200_chars_is_accepted()
    {
        var created = await _service.CreateAsync(new CreateClassRequest(new string('a', 200), 10), Ct);

        Assert.Equal(200, created.Name.Length);
    }

    [Theory, InlineData(null), InlineData(0), InlineData(-1), InlineData(100001)]
    public Task Bad_seats_are_rejected(int? seats) =>
        AssertValidationAsync(_service.CreateAsync(new CreateClassRequest("Class 6", seats), Ct));

    [Theory, InlineData(1), InlineData(100000)]
    public async Task Seat_bounds_are_accepted(int seats)
    {
        var created = await _service.CreateAsync(new CreateClassRequest("Class 6", seats), Ct);

        Assert.Equal(seats, created.TotalSeats);
    }

    [Fact]
    public async Task Bengali_name_is_accepted()
    {
        const string name = "ষষ্ঠ শ্রেণি - প্রভাতী";

        var created = await _service.CreateAsync(new CreateClassRequest(name, 40), Ct);

        Assert.Equal(name, created.Name);
    }

    [Fact]
    public async Task Get_returns_created_class()
    {
        var created = await _service.CreateAsync(new CreateClassRequest("Class 6", 30), Ct);

        var found = await _service.GetAsync(created.Id.ToString(), Ct);

        Assert.Equal(created, found);
    }

    [Theory, InlineData("not-a-guid"), InlineData(""), InlineData("00000000-0000-0000-0000-000000000001")]
    public async Task Get_throws_not_found_for_malformed_or_unknown_id(string id)
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => _service.GetAsync(id, Ct));

        Assert.Equal(ErrorKind.NotFound, ex.Kind);
        Assert.Equal("CLASS_NOT_FOUND", ex.Code);
    }

    [Fact]
    public async Task List_returns_all_created_classes()
    {
        await _service.CreateAsync(new CreateClassRequest("Class 5", 20), Ct);
        await _service.CreateAsync(new CreateClassRequest("Class 6", 30), Ct);

        var all = await _service.ListAsync(Ct);

        Assert.Equal(["Class 5", "Class 6"], all.Select(c => c.Name).Order());
    }
}
