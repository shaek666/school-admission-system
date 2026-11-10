using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SchoolAdmission.Api;
using SchoolAdmission.Api.Http;
using SchoolAdmission.Business;
using SchoolAdmission.DataAccess;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString(DataAccessOptions.ConnectionStringName);
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        $"Connection string '{DataAccessOptions.ConnectionStringName}' is not configured. " +
        "Set ConnectionStrings__Default.");
}

builder.Services.AddHoldOptions(builder.Configuration);
builder.Services.AddOptions<DataAccessOptions>()
    .Bind(builder.Configuration.GetSection(DataAccessOptions.SectionName))
    .Validate(options => options.LockTimeoutSeconds > 0, "DataAccess:LockTimeoutSeconds must be a positive integer.")
    .ValidateOnStart();
builder.Services.AddDbContext<AdmissionDbContext>(options => options
    .UseNpgsql(connectionString, npgsql => npgsql.CommandTimeout(DataAccessOptions.CommandTimeoutSeconds))
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
builder.Services.AddSingleton<DatabaseMigrator>();
builder.Services.AddSingleton<IExceptionTranslator, PostgresExceptionTranslator>();
// Binding failures throw BadHttpRequestException in every environment (the default is Development only),
// so the error middleware answers them with INVALID_JSON instead of an empty 400.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("Date")));

var app = builder.Build();

// Validate the options now so a bad setting fails before any database work.
app.Services.GetRequiredService<IStartupValidator>().Validate();

await app.Services.GetRequiredService<DatabaseMigrator>().MigrateAsync(app.Lifetime.ApplicationStopping);

// Order matters. Errors outermost, then empty-body 4xx and 5xx, then CORS before routing so preflights
// to any path (even unknown ones) are answered, then routing and endpoints.
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseStatusCodePages(StatusCodeBody.WriteAsync);
app.UseCors();
app.UseRouting();

app.Run();

public partial class Program;
