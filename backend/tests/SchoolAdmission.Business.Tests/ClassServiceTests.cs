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

    public static TheoryData<string?, int?> RejectedInputs => new()
    {
        { null, 10 },
        { "", 10 },
        { "   ", 10 },
        { "\t\n", 10 },
        { new string('a', 201), 10 },
        { " " + new string('a', 201) + " ", 10 },
        { "Class 6", null },
        { "Class 6", 0 },
        { "Class 6", -1 },
        { "Class 6", 100001 },
    };

    public static TheoryData<string, int> AcceptedInputs => new()
    {
        { new string('a', 200), 10 },
        { " " + new string('a', 200) + " ", 10 },
        { "Class 6", 1 },
        { "Class 6", 100000 },
        { "ষষ্ঠ শ্রেণি - প্রভাতী", 40 },
    };

    [Theory]
    [MemberData(nameof(RejectedInputs))]
    public Task Invalid_input_is_rejected(string? name, int? seats) =>
        AssertValidationAsync(_service.CreateAsync(new CreateClassRequest(name, seats), Ct));

    [Theory]
    [MemberData(nameof(AcceptedInputs))]
    public async Task Valid_input_is_accepted(string name, int seats)
    {
        var created = await _service.CreateAsync(new CreateClassRequest(name, seats), Ct);

        Assert.Equal(name.Trim(), created.Name);
        Assert.Equal(seats, created.TotalSeats);
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
