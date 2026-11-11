using System.Text.Json;
using Microsoft.Net.Http.Headers;
using SchoolAdmission.Business;

namespace SchoolAdmission.Api.Http;

/// <summary>Reads a JSON request body and turns every body problem into a client error.</summary>
public static class JsonBody
{
    public static async Task<T> ReadAsync<T>(HttpRequest request, CancellationToken ct)
    {
        if (!request.HasJsonContentType())
        {
            // HasJsonContentType is also false for a JSON type with an unknown charset. That is a bad body, not a bad type.
            if (IsJsonMediaType(request.ContentType))
            {
                throw Errors.InvalidJson();
            }

            throw new BadHttpRequestException(
                "Content-Type must be application/json.", StatusCodes.Status415UnsupportedMediaType);
        }

        T? body;
        try
        {
            body = await request.ReadFromJsonAsync<T>(ct);
        }
        catch (Exception ex) when (ex is JsonException
            or InvalidOperationException // unknown charset
            or BadHttpRequestException { StatusCode: StatusCodes.Status400BadRequest })
        {
            throw Errors.InvalidJson();
        }

        return body ?? throw Errors.InvalidJson();
    }

    private static bool IsJsonMediaType(string? contentType) =>
        MediaTypeHeaderValue.TryParse(contentType, out var mediaType)
        && (mediaType.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            || mediaType.Suffix.Equals("json", StringComparison.OrdinalIgnoreCase));
}
