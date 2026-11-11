namespace SchoolAdmission.Api.Tests.Concurrency;

/// <summary>Runs alone, so these tests never share the database pool with the parallel suite.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConcurrencyCollection
{
    public const string Name = "Concurrency";
}
