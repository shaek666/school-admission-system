using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SchoolAdmission.Api.Tests.Infrastructure;

namespace SchoolAdmission.Api.Tests;

[Collection(PostgresCollection.Name)]
public class ClassContractTests(PostgresFixture fixture)
{
    private static readonly string[] CountFields =
        ["id", "name", "totalSeats", "heldSeats", "confirmedSeats", "availableSeats"];

    [Fact]
    public async Task Post_class_returns_201_with_contract_fields()
    {
        using var client = fixture.Factory.CreateClient();
        var name = "Class 6 - Morning Shift " + Guid.NewGuid();

        var response = await client.PostAsJsonAsync("/classes", new { name, totalSeats = 120 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal(JsonValueKind.String, root.GetProperty("id").ValueKind);
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

        foreach (var field in CountFields)
            Assert.True(item.TryGetProperty(field, out _), $"missing {field}");
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
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(listItem.GetRawText(), doc.RootElement.GetRawText());
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Get_unknown_or_malformed_class_is_404_CLASS_NOT_FOUND(string id)
    {
        using var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync($"/classes/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("CLASS_NOT_FOUND", doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Same_name_twice_returns_201_both_times()
    {
        using var client = fixture.Factory.CreateClient();
        var name = "Duplicate " + Guid.NewGuid();

        var first = await client.PostAsJsonAsync("/classes", new { name, totalSeats = 3 });
        var second = await client.PostAsJsonAsync("/classes", new { name, totalSeats = 3 });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    private static async Task<JsonElement> FindInListAsync(HttpClient client, string id)
    {
        var response = await client.GetAsync("/classes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        return doc.RootElement.EnumerateArray().Single(e => e.GetProperty("id").GetString() == id).Clone();
    }
}
