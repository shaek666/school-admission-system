using System.Net;
using SchoolAdmission.Api.Tests.Infrastructure;

namespace SchoolAdmission.Api.Tests;

[Collection(PostgresCollection.Name)]
public class ConfirmCancelContractTests(PostgresFixture fixture)
{
    private static Task<HttpResponseMessage> ConfirmAsync(HttpClient client, string holdId, string? studentId) =>
        TestData.SendAsync(client, HttpMethod.Post, $"/holds/{holdId}/confirm", studentId);

    private static Task<HttpResponseMessage> DeleteAsync(HttpClient client, string holdId, string? studentId) =>
        TestData.SendAsync(client, HttpMethod.Delete, $"/holds/{holdId}", studentId);

    private static async Task<int> SeatAsync(HttpClient client, string classId, string property)
    {
        var response = await client.GetAsync($"/classes/{classId}");
        return (await HttpJson.ReadJsonAsync(response)).GetProperty(property).GetInt32();
    }

    [Fact]
    public async Task Confirm_returns_200_with_holdId_and_CONFIRMED()
    {
        using var client = fixture.Factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 3);
        var student = TestData.NewStudentId();
        var holdId = await TestData.CreateHoldAsync(client, classId, student);

        var response = await ConfirmAsync(client, holdId, student);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = await HttpJson.ReadJsonAsync(response);
        Assert.Equal(holdId, root.GetProperty("holdId").GetString());
        Assert.Equal("CONFIRMED", root.GetProperty("status").GetString());
        Assert.Equal(2, root.EnumerateObject().Count());
    }

    [Fact]
    public async Task Confirm_twice_returns_200_both_times_and_confirmedSeats_is_1()
    {
        using var client = fixture.Factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 3);
        var student = TestData.NewStudentId();
        var holdId = await TestData.CreateHoldAsync(client, classId, student);

        var first = await ConfirmAsync(client, holdId, student);
        var second = await ConfirmAsync(client, holdId, student);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal("CONFIRMED", (await HttpJson.ReadJsonAsync(second)).GetProperty("status").GetString());
        Assert.Equal(1, await SeatAsync(client, classId, "confirmedSeats"));
    }

    [Fact]
    public async Task Confirm_without_header_is_400_even_for_an_unknown_hold()
    {
        using var client = fixture.Factory.CreateClient();

        var response = await ConfirmAsync(client, Guid.NewGuid().ToString(), null);
        var deleted = await DeleteAsync(client, Guid.NewGuid().ToString(), null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, deleted.StatusCode);
    }

    [Fact]
    public async Task Confirm_other_students_hold_is_403_NOT_HOLD_OWNER()
    {
        using var client = fixture.Factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 3);
        var holdId = await TestData.CreateHoldAsync(client, classId, TestData.NewStudentId());

        var response = await ConfirmAsync(client, holdId, TestData.NewStudentId());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("NOT_HOLD_OWNER", await HttpJson.ErrorCodeAsync(response));
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("not-a-guid")]
    public async Task Confirm_and_delete_unknown_or_malformed_hold_is_404(string holdId)
    {
        using var client = fixture.Factory.CreateClient();
        var student = TestData.NewStudentId();

        var confirm = await ConfirmAsync(client, holdId, student);
        var delete = await DeleteAsync(client, holdId, student);

        Assert.Equal(HttpStatusCode.NotFound, confirm.StatusCode);
        Assert.Equal("HOLD_NOT_FOUND", await HttpJson.ErrorCodeAsync(confirm));
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Equal("HOLD_NOT_FOUND", await HttpJson.ErrorCodeAsync(delete));
    }

    [Fact]
    public async Task Delete_returns_204_and_the_seat_returns()
    {
        using var client = fixture.Factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 3);
        var student = TestData.NewStudentId();
        var holdId = await TestData.CreateHoldAsync(client, classId, student);
        Assert.Equal(2, await SeatAsync(client, classId, "availableSeats"));

        var response = await DeleteAsync(client, holdId, student);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        Assert.Equal(3, await SeatAsync(client, classId, "availableSeats"));
    }

    [Fact]
    public async Task Delete_twice_returns_204()
    {
        using var client = fixture.Factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 3);
        var student = TestData.NewStudentId();
        var holdId = await TestData.CreateHoldAsync(client, classId, student);

        var first = await DeleteAsync(client, holdId, student);
        var second = await DeleteAsync(client, holdId, student);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }

    [Fact]
    public async Task Delete_other_students_hold_is_403()
    {
        using var client = fixture.Factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 3);
        var holdId = await TestData.CreateHoldAsync(client, classId, TestData.NewStudentId());

        var response = await DeleteAsync(client, holdId, TestData.NewStudentId());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("NOT_HOLD_OWNER", await HttpJson.ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Delete_confirmed_is_409_HOLD_CONFIRMED()
    {
        using var client = fixture.Factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 3);
        var student = TestData.NewStudentId();
        var holdId = await TestData.CreateHoldAsync(client, classId, student);
        await ConfirmAsync(client, holdId, student);

        var response = await DeleteAsync(client, holdId, student);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("HOLD_CONFIRMED", await HttpJson.ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Confirm_cancelled_is_409_HOLD_CANCELLED()
    {
        using var client = fixture.Factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 3);
        var student = TestData.NewStudentId();
        var holdId = await TestData.CreateHoldAsync(client, classId, student);
        await DeleteAsync(client, holdId, student);

        var response = await ConfirmAsync(client, holdId, student);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("HOLD_CANCELLED", await HttpJson.ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Cancelled_student_can_apply_again()
    {
        using var client = fixture.Factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 3);
        var student = TestData.NewStudentId();
        var holdId = await TestData.CreateHoldAsync(client, classId, student);
        await DeleteAsync(client, holdId, student);

        var again = await TestData.CreateHoldAsync(client, classId, student);

        Assert.NotEqual(holdId, again);
    }
}
