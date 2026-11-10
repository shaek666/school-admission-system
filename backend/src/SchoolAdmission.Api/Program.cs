using Microsoft.EntityFrameworkCore;
using SchoolAdmission.DataAccess;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString(DataAccessOptions.ConnectionStringName);
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        $"Connection string '{DataAccessOptions.ConnectionStringName}' is not configured. " +
        "Set ConnectionStrings__Default.");
}

builder.Services.AddDbContext<AdmissionDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton<DatabaseMigrator>();

var app = builder.Build();

await app.Services.GetRequiredService<DatabaseMigrator>().MigrateAsync(app.Lifetime.ApplicationStopping);

app.Run();

public partial class Program;
