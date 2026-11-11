namespace SchoolAdmission.Business.Tests;

public class IdsTests
{
    private static readonly Guid Sample = Guid.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3301");

    [Fact]
    public void Dashed_guid_parses()
    {
        Assert.True(Ids.TryParse(Sample.ToString("D"), out var id));
        Assert.Equal(Sample, id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("3f2504e04f8911d39a0c0305e82c3301")]
    [InlineData("{3f2504e0-4f89-11d3-9a0c-0305e82c3301}")]
    [InlineData(" 3f2504e0-4f89-11d3-9a0c-0305e82c3301 ")]
    [InlineData("3f2504e0-4f89-11d3-9a0c-0305e82c3301\0")]
    public void Other_forms_do_not_parse(string? raw)
    {
        Assert.False(Ids.TryParse(raw, out var id));
        Assert.Equal(Guid.Empty, id);
    }
}
