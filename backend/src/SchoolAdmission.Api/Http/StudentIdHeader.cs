using SchoolAdmission.Business;
using SchoolAdmission.Business.Students;

namespace SchoolAdmission.Api.Http;

public static class StudentIdHeader
{
    public const string Name = "X-Student-Id";

    /// <summary>Reads the one valid student ID header. A missing, repeated or malformed value is a 400. A comma-joined value fails the 17-digit rule.</summary>
    public static StudentId Require(HttpRequest request)
    {
        var values = request.Headers[Name];
        if (values.Count != 1 || values[0] is not { } raw || !StudentId.TryParse(raw, out var id))
        {
            throw Errors.InvalidStudentId();
        }

        return id;
    }
}
