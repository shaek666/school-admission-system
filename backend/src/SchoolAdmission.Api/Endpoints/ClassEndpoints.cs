using SchoolAdmission.Api.Http;
using SchoolAdmission.Business.Classes;

namespace SchoolAdmission.Api.Endpoints;

public static class ClassEndpoints
{
    public static IEndpointRouteBuilder MapClassEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/classes", async (CreateClassRequest request, ClassService service, CancellationToken ct) =>
            Results.Json(ClassResponse.From(await service.CreateAsync(request, ct)), statusCode: StatusCodes.Status201Created));

        app.MapGet("/classes", async (ClassService service, CancellationToken ct) =>
            (await service.ListAsync(ct)).Select(ClassResponse.From).ToList());

        app.MapGet("/classes/{id}", async (string id, ClassService service, CancellationToken ct) =>
            ClassResponse.From(await service.GetAsync(id, ct)));

        return app;
    }
}
