using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using SchoolAdmission.Api.Http;
using SchoolAdmission.Business;

namespace SchoolAdmission.Api.Tests;

public class StudentIdHeaderTests
{
    private const string Valid = "20152691234567890";

    private static HttpRequest RequestWith(StringValues? header)
    {
        var request = new DefaultHttpContext().Request;
        if (header is { } values)
        {
            request.Headers["X-Student-Id"] = values;
        }

        return request;
    }

    private static void AssertInvalid(HttpRequest request)
    {
        var ex = Assert.Throws<AppException>(() => StudentIdHeader.Require(request));
        Assert.Equal(ErrorKind.Validation, ex.Kind);
        Assert.Equal("INVALID_STUDENT_ID", ex.Code);
    }

    [Fact]
    public void Valid_header_is_returned() =>
        Assert.Equal(Valid, StudentIdHeader.Require(RequestWith(Valid)).Value);

    [Fact]
    public void Missing_header_is_400_INVALID_STUDENT_ID() => AssertInvalid(RequestWith(null));

    [Fact]
    public void Repeated_header_is_400_INVALID_STUDENT_ID() =>
        AssertInvalid(RequestWith(new StringValues([Valid, Valid])));

    [Fact]
    public void Comma_joined_header_is_400_INVALID_STUDENT_ID() =>
        AssertInvalid(RequestWith($"{Valid},{Valid}"));

    [Theory]
    [InlineData("")]
    [InlineData("2015269123456789")]
    [InlineData("20152691234567890\n")]
    [InlineData(" 20152691234567890")]
    public void Malformed_header_is_400_INVALID_STUDENT_ID(string value) => AssertInvalid(RequestWith(value));
}
