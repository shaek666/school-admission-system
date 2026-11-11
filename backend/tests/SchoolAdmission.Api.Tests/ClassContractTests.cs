using System.Net;
using System.Text.Json;
using SchoolAdmission.Api.Tests.Infrastructure;

namespace SchoolAdmission.Api.Tests;

[Collection(PostgresCollection.Name)]
public class ClassContractTests(PostgresFixture fixture)
{
    private static readonly string[] ContractProperties =
        ["availableSeats", "confirmedSeats", "heldSeats", "id", "name", "totalSeats"];

    private static void AssertContractShape(JsonElement element)
    {
        Assert.Equal(JsonValueKind.Object, element.ValueKind);
        Assert.Equal(ContractProperties, element.EnumerateObject().Select(p => p.Name).Order().ToArray());
    }

    [Fact]
    public async Task Post_class_returns_201_with_contract_fields()
    {
        using var client = fixture.Factory.CreateClient();
        var name = "Class 6 - Morning Shift " + Guid.NewGuid();

        var response = await TestData.PostClassAsync(client, 120, name);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var root = await HttpJson.ReadJsonAsync(response);
        AssertContractShape(root);
        Assert.True(Guid.TryParse(root.GetProperty("id").GetString(), out _));
        Assert.Equal(name, root.GetProperty("name").GetString());
        Assert.Equal(120, root.GetProperty("totalSeats").GetInt32());
    }

    [Fact]
    public async Task Get_classes_returns_array_with_all_count_fields()
    {
        using var client = fixture.Factory.CreateClient();
        var id = await TestData.CreateClassAsync(client, 120);

        var item = await FindInListAsync(client, id);

        AssertContractShape(item);
        Assert.Equal(0, item.GetProperty("heldSeats").GetInt32());
        Assert.Equal(0, item.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(120, item.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task Get_class_by_id_matches_list_item_shape()
    {
        using var client = fixture.Factory.CreateClient();
        var id = await TestData.CreateClassAsync(client, 5);

        var listItem = await FindInListAsync(client, id);
        var response = await client.GetAsync($"/classes/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(listItem.GetRawText(), (await HttpJson.ReadJsonAsync(response)).GetRawText());
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Get_unknown_or_malformed_class_is_404_CLASS_NOT_FOUND(string id)
    {
        using var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync($"/classes/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("CLASS_NOT_FOUND", await HttpJson.ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Same_name_twice_returns_201_both_times()
    {
        using var client = fixture.Factory.CreateClient();
        var name = "Duplicate " + Guid.NewGuid();

        var first = await TestData.PostClassAsync(client, 3, name);
        var second = await TestData.PostClassAsync(client, 3, name);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var firstId = (await HttpJson.ReadJsonAsync(first)).GetProperty("id").GetString();
        var secondId = (await HttpJson.ReadJsonAsync(second)).GetProperty("id").GetString();
        Assert.NotEqual(firstId, secondId);
    }

    private static async Task<JsonElement> FindInListAsync(HttpClient client, string id)
    {
        var response = await client.GetAsync("/classes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await HttpJson.ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, list.ValueKind);
        return list.EnumerateArray().Single(e => e.GetProperty("id").GetString() == id);
    }
}
