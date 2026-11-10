namespace SchoolAdmission.Api.Tests.Infrastructure;

/// <summary>Starts one factory for the whole collection, so the migration runs once.</summary>
public sealed class PostgresFixture : IDisposable
{
    public AppFactory Factory { get; } = new();

    public PostgresFixture()
    {
        // Creating the client builds the host, which runs the migrator.
        Factory.CreateClient().Dispose();
    }

    public void Dispose() => Factory.Dispose();
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
