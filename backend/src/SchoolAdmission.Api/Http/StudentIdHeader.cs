using SchoolAdmission.Business;
using SchoolAdmission.Business.Students;

namespace SchoolAdmission.Api.Http;

public static class StudentIdHeader
{
    public const string Name = "X-Student-Id";

    /// <summary>Reads the one valid student ID header. A missing, repeated, comma-joined or malformed value is a 400.</summary>
    public static StudentId Require(HttpRequest request)
    {
        var values = request.Headers[Name];
        if (values.Count != 1 || values[0] is not { } raw || raw.Contains(',') || !StudentId.TryParse(raw, out var id))
        {
            throw Errors.InvalidStudentId();
        }

        return id;
    }
}
