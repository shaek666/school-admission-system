using Microsoft.AspNetCore.Diagnostics;

namespace SchoolAdmission.Api.Http;

/// <summary>Writes the error body for a 4xx or 5xx response that has no body.</summary>
public static class StatusCodeBody
{
    public static Task WriteAsync(StatusCodeContext context)
    {
        var (code, message) = Describe(context.HttpContext.Response.StatusCode);
        return ErrorBody.WriteAsync(context.HttpContext.Response, context.HttpContext.Response.StatusCode, code, message);
    }

    private static (string Code, string Message) Describe(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => ("BAD_REQUEST", "The request could not be understood."),
        StatusCodes.Status404NotFound => ("NOT_FOUND", "We could not find what you asked for."),
        StatusCodes.Status405MethodNotAllowed => ("METHOD_NOT_ALLOWED", "This method is not allowed here."),
        StatusCodes.Status415UnsupportedMediaType => ("UNSUPPORTED_MEDIA_TYPE", "Send the body as application/json."),
        >= 500 => ("INTERNAL_ERROR", "Something went wrong on our side. Please try again."),
        _ => ("REQUEST_ERROR", "The request could not be completed.")
    };
}
