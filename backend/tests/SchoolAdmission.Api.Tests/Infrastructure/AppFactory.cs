using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SchoolAdmission.Api.Tests.Infrastructure;

public sealed class AppFactory : WebApplicationFactory<Program>
{
    private const string DefaultConnectionString =
        "Host=localhost;Database=admission_test;Username=postgres;Password=postgres;Maximum Pool Size=40;Timeout=60";

    private readonly Dictionary<string, string> _settings = new()
    {
        ["ConnectionStrings:Default"] = ConnectionString,
        ["EXPIRY_SWEEP_SECONDS"] = "3600",
    };

    private bool _hostCreated;

    public static string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? DefaultConnectionString;

    public AppFactory WithSetting(string key, string value)
    {
        if (_hostCreated)
        {
            throw new InvalidOperationException(
                $"Cannot apply setting '{key}' because the test host is already created. Call WithSetting first.");
        }

        _settings[key] = value;
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _hostCreated = true;
        builder.UseEnvironment("Production");
        foreach (var (key, value) in _settings)
        {
            builder.UseSetting(key, value);
        }
    }
}
