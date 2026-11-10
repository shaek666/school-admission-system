using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SchoolAdmission.Api;
using SchoolAdmission.Business.Holds;
using SchoolAdmission.DataAccess;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString(DataAccessOptions.ConnectionStringName);
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        $"Connection string '{DataAccessOptions.ConnectionStringName}' is not configured. " +
        "Set ConnectionStrings__Default.");
}

builder.Services.AddOptions<HoldOptions>().ValidateOnStart();
builder.Services.AddSingleton<IConfigureOptions<HoldOptions>, HoldOptionsSetup>();
builder.Services.AddSingleton<IValidateOptions<HoldOptions>, HoldOptionsSetup>();
builder.Services.Configure<DataAccessOptions>(builder.Configuration.GetSection(DataAccessOptions.SectionName));
builder.Services.AddDbContext<AdmissionDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton<DatabaseMigrator>();

var app = builder.Build();

// Read the options now so a bad setting fails before any database work.
_ = app.Services.GetRequiredService<IOptions<HoldOptions>>().Value;

await app.Services.GetRequiredService<DatabaseMigrator>().MigrateAsync(app.Lifetime.ApplicationStopping);

app.Run();

public partial class Program;
