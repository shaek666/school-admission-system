using SchoolAdmission.Business.Students;

namespace SchoolAdmission.Business.Tests;

public class StudentIdTests
{
    [Theory]
    [InlineData("20152691234567890")]
    [InlineData("00000000000000000")]
    public void Valid_17_digits_parse(string raw)
    {
        Assert.True(StudentId.TryParse(raw, out var id));
        Assert.Equal(raw, id.Value);
    }

    [Theory]
    [InlineData("2015269123456789")]
    [InlineData("201526912345678901")]
    [InlineData("2015269123456789a")]
    [InlineData(" 20152691234567890")]
    [InlineData("20152691234567890 ")]
    [InlineData("20152691234567890\n")]
    [InlineData("\n20152691234567890")]
    [InlineData("20152691234567890\0")]
    [InlineData("২০১৫২৬৯১২৩৪৫৬৭৮৯০")]
    [InlineData("")]
    [InlineData(null)]
    public void Invalid_ids_fail(string? raw)
    {
        Assert.False(StudentId.TryParse(raw, out var id));
        Assert.Equal(default, id);
    }

    [Fact]
    public void Masked_keeps_last_4()
    {
        StudentId.TryParse("20152691234567890", out var id);

        Assert.Equal("*************7890", id.Masked);
    }

    [Fact]
    public void Default_id_masks_fully_instead_of_throwing()
    {
        Assert.Equal(new string('*', 17), default(StudentId).Masked);
        Assert.Equal(new string('*', 17), default(StudentId).ToString());
    }

    [Fact]
    public void ToString_and_interpolation_print_masked_value()
    {
        StudentId.TryParse("20152691234567890", out var id);

        Assert.Equal("*************7890", id.ToString());
        Assert.DoesNotContain("2015269", $"student {id}");
    }
}
