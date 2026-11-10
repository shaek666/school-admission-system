namespace SchoolAdmission.Api.Http;

public static class ErrorBody
{
    public static Task WriteAsync(HttpResponse response, int statusCode, string code, string message)
    {
        response.StatusCode = statusCode;
        return response.WriteAsJsonAsync(
            new { error = new { code, message } }, options: null, "application/json", response.HttpContext.RequestAborted);
    }
}
