using System.Text.Json;
using SchoolAdmission.Business;

namespace SchoolAdmission.Api.Http;

public sealed class ErrorHandlingMiddleware(
    RequestDelegate next,
    IExceptionTranslator translator,
    ILogger<ErrorHandlingMiddleware> logger)
{
    private const string RetryAfterSeconds = "2";

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (!context.RequestAborted.IsCancellationRequested && !context.Response.HasStarted)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        if (exception is BadHttpRequestException badRequest && !IsBadBody(badRequest))
        {
            var (code, message) = StatusCodeBody.Describe(badRequest.StatusCode);
            await ErrorBody.WriteAsync(context.Response, badRequest.StatusCode, code, message);
            return;
        }

        var appException = exception switch
        {
            AppException app => app,
            JsonException or BadHttpRequestException => Errors.InvalidJson(),
            _ => translator.Translate(exception)
        };

        if (appException is null)
        {
            // Log the exception only. Never log the request body.
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await ErrorBody.WriteAsync(context.Response, StatusCodes.Status500InternalServerError,
                "INTERNAL_ERROR", "Something went wrong on our side. Please try again.");
            return;
        }

        if (appException.Kind == ErrorKind.Busy)
        {
            context.Response.Headers.RetryAfter = RetryAfterSeconds;
        }

        await ErrorBody.WriteAsync(context.Response, StatusFor(appException.Kind), appException.Code, appException.Message);
    }

    /// <summary>
    /// True when a BadHttpRequestException is a 400 about the request body: it wraps a JsonException, or its
    /// framework message says the body could not be read or was not provided. Any other status or 400 is not.
    /// </summary>
    private static bool IsBadBody(BadHttpRequestException exception)
    {
        if (exception.InnerException is JsonException)
        {
            return true;
        }

        return exception.StatusCode == StatusCodes.Status400BadRequest
            && (exception.Message.Contains("body", StringComparison.OrdinalIgnoreCase)
                || exception.Message.Contains("request content", StringComparison.OrdinalIgnoreCase));
    }

    private static int StatusFor(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => StatusCodes.Status400BadRequest,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.Busy => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };
}
