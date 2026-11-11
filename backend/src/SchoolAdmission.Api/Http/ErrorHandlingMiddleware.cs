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
        catch (Exception ex) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client is gone, so there is nothing to write. A cancellation is expected and not logged.
            // Anything else (for example a broken Npgsql stream) is logged at Debug only.
            if (ex is not OperationCanceledException)
            {
                logger.LogDebug(ex, "Request aborted by the client for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            }
        }
        catch (Exception ex) when (!context.RequestAborted.IsCancellationRequested && !context.Response.HasStarted)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        // Drop headers that the endpoint set before it threw (for example Location or Content-Length),
        // but keep CORS headers, so the browser can still read the error.
        var corsHeaders = context.Response.Headers
            .Where(header => header.Key.StartsWith("Access-Control-", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        context.Response.Clear();
        foreach (var (name, value) in corsHeaders)
        {
            context.Response.Headers[name] = value;
        }

        if (exception is BadHttpRequestException badRequest)
        {
            var (code, message) = StatusCodeBody.Describe(badRequest.StatusCode);
            await ErrorBody.WriteAsync(context.Response, badRequest.StatusCode, code, message);
            return;
        }

        var appException = exception switch
        {
            AppException app => app,
            _ => translator.Translate(exception)
        };

        if (appException is null)
        {
            // Log the exception only. Never log the request body.
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);
            var (code, message) = StatusCodeBody.Describe(StatusCodes.Status500InternalServerError);
            await ErrorBody.WriteAsync(context.Response, StatusCodes.Status500InternalServerError, code, message);
            return;
        }

        if (appException.Kind == ErrorKind.Busy)
        {
            // Log the type only. The exception text can carry query values.
            logger.LogWarning("Database busy ({ExceptionType}) for {Method} {Path}",
                exception.GetType().Name, context.Request.Method, context.Request.Path);
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
