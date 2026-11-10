using System.Text.Json;
using SchoolAdmission.Business;
using SchoolAdmission.Business.Errors;

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
