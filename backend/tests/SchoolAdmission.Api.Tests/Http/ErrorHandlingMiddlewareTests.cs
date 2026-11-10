using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolAdmission.Api.Http;
using SchoolAdmission.Business;
using SchoolAdmission.Business.Errors;

namespace SchoolAdmission.Api.Tests.Http;

public class ErrorHandlingMiddlewareTests
{
    private const string SecretText = "secret-connection-detail";

    private sealed class FakeTranslator(AppException? result) : IExceptionTranslator
    {
        public AppException? Translate(Exception exception) => result;
    }

    private sealed class StartedResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = new MemoryStream();
        public bool HasStarted => true;
        public void OnStarting(Func<object, Task> callback, object state) { }
        public void OnCompleted(Func<object, Task> callback, object state) { }
    }

    private static DefaultHttpContext NewContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<DefaultHttpContext> RunAsync(Exception thrown, AppException? translated = null)
    {
        var context = NewContext();
        var middleware = new ErrorHandlingMiddleware(
            _ => throw thrown, new FakeTranslator(translated), NullLogger<ErrorHandlingMiddleware>.Instance);
        await middleware.InvokeAsync(context);
        return context;
    }

    private static (string Code, string Message) ReadError(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var doc = JsonDocument.Parse(context.Response.Body);
        var error = doc.RootElement.GetProperty("error");
        return (error.GetProperty("code").GetString()!, error.GetProperty("message").GetString()!);
    }

    public static TheoryData<AppException, int> Kinds => new()
    {
        { Errors.Validation("bad"), 400 },
        { Errors.ClassNotFound(), 404 },
        { Errors.NotHoldOwner(), 403 },
        { Errors.NoSeatsAvailable(), 409 },
        { Errors.ServiceBusy(), 503 },
    };

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task App_exception_maps_to_status_and_body(AppException exception, int status)
    {
        var context = await RunAsync(exception);

        Assert.Equal(status, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);
        var (code, message) = ReadError(context);
        Assert.Equal(exception.Code, code);
        Assert.Equal(exception.Message, message);
    }

    [Fact]
    public async Task Busy_sets_retry_after_2()
    {
        var context = await RunAsync(Errors.ServiceBusy());

        Assert.Equal("2", context.Response.Headers.RetryAfter.ToString());
    }

    [Fact]
    public async Task Non_busy_error_has_no_retry_after()
    {
        var context = await RunAsync(Errors.HoldNotFound());

        Assert.False(context.Response.Headers.ContainsKey("Retry-After"));
    }

    [Theory]
    [InlineData(typeof(JsonException))]
    [InlineData(typeof(BadHttpRequestException))]
    public async Task Bad_json_gives_400_invalid_json(Type type)
    {
        var thrown = (Exception)Activator.CreateInstance(type, SecretText)!;

        var context = await RunAsync(thrown);

        Assert.Equal(400, context.Response.StatusCode);
        var (code, message) = ReadError(context);
        Assert.Equal("INVALID_JSON", code);
        Assert.DoesNotContain(SecretText, message);
    }

    [Fact]
    public async Task Translated_exception_uses_mapped_status()
    {
        var context = await RunAsync(new InvalidOperationException("db"), Errors.AlreadyApplied());

        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal("ALREADY_APPLIED", ReadError(context).Code);
    }

    [Fact]
    public async Task Unmapped_exception_gives_500_without_exception_text()
    {
        var context = await RunAsync(new InvalidOperationException(SecretText));

        Assert.Equal(500, context.Response.StatusCode);
        var (code, message) = ReadError(context);
        Assert.Equal("INTERNAL_ERROR", code);
        Assert.DoesNotContain(SecretText, message);
        context.Response.Body.Position = 0;
        Assert.DoesNotContain(SecretText, await new StreamReader(context.Response.Body).ReadToEndAsync());
    }

    [Fact]
    public async Task Started_response_is_untouched_and_exception_propagates()
    {
        // Install the feature before any Response access, because response features are cached on first use.
        var feature = new StartedResponseFeature();
        var context = new DefaultHttpContext();
        var body = new MemoryStream();
        context.Features.Set<IHttpResponseFeature>(feature);
        context.Features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(body));
        var thrown = Errors.ServiceBusy();
        var middleware = new ErrorHandlingMiddleware(
            _ => throw thrown, new FakeTranslator(null), NullLogger<ErrorHandlingMiddleware>.Instance);

        var caught = await Assert.ThrowsAsync<AppException>(() => middleware.InvokeAsync(context));

        Assert.Same(thrown, caught);
        Assert.Same(body, context.Response.Body);
        Assert.Equal(0, body.Length);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey("Retry-After"));
    }

    [Fact]
    public async Task Preset_cors_header_survives_error_write()
    {
        var context = NewContext();
        context.Response.Headers.AccessControlAllowOrigin = "*";
        var middleware = new ErrorHandlingMiddleware(
            _ => throw Errors.HoldNotFound(), new FakeTranslator(null), NullLogger<ErrorHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal("*", context.Response.Headers.AccessControlAllowOrigin.ToString());
        Assert.Equal(404, context.Response.StatusCode);
    }
}
