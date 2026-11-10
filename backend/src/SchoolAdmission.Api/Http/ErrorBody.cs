using System.Text.Json;

namespace SchoolAdmission.Api.Http;

public static class ErrorBody
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static Task WriteAsync(HttpResponse response, int statusCode, string code, string message)
    {
        response.StatusCode = statusCode;
        return response.WriteAsJsonAsync(
            new { error = new { code, message } }, JsonOptions, "application/json", response.HttpContext.RequestAborted);
    }
}
