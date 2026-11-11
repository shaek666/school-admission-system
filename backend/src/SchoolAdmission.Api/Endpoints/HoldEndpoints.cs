using SchoolAdmission.Api.Http;
using SchoolAdmission.Business.Holds;

namespace SchoolAdmission.Api.Endpoints;

public static class HoldEndpoints
{
    public static IEndpointRouteBuilder MapHoldEndpoints(this IEndpointRouteBuilder app)
    {
        // The header is checked before the class is looked up, so a bad header is 400 even for an unknown class.
        app.MapPost("/classes/{classId}/holds", async (string classId, HttpRequest http, HoldService service, CancellationToken ct) =>
        {
            var student = StudentIdHeader.Require(http);
            var hold = await service.CreateAsync(classId, student, ct);
            return Results.Json(HoldCreatedResponse.From(hold), statusCode: StatusCodes.Status201Created);
        });

        app.MapGet("/holds/{holdId}", async (string holdId, HoldService service, CancellationToken ct) =>
            HoldResponse.From(await service.GetAsync(holdId, ct)));

        app.MapGet("/me/holds", async (HttpRequest http, HoldService service, CancellationToken ct) =>
        {
            var student = StudentIdHeader.Require(http);
            return (await service.ListMineAsync(student, ct)).Select(MyHoldResponse.From).ToList();
        });

        // Header first (400), then the service runs resource, owner and state checks.
        app.MapPost("/holds/{holdId}/confirm", async (string holdId, HttpRequest http, HoldService service, CancellationToken ct) =>
        {
            var student = StudentIdHeader.Require(http);
            return ConfirmResponse.From(await service.ConfirmAsync(holdId, student, ct));
        });

        app.MapDelete("/holds/{holdId}", async (string holdId, HttpRequest http, HoldService service, CancellationToken ct) =>
        {
            var student = StudentIdHeader.Require(http);
            await service.CancelAsync(holdId, student, ct);
            return Results.NoContent();
        });

        return app;
    }
}
