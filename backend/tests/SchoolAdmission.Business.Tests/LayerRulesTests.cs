namespace SchoolAdmission.Business.Tests;

public class LayerRulesTests
{
    [Fact]
    public void Business_references_no_lower_layer()
    {
        var names = typeof(SchoolAdmission.Business.Errors.AppException).Assembly
            .GetReferencedAssemblies().Select(a => a.Name!).ToList();

        Assert.DoesNotContain("SchoolAdmission.DataAccess", names);
        Assert.DoesNotContain(names, n => n.StartsWith("Npgsql") || n.StartsWith("Microsoft.EntityFrameworkCore"));
    }
}
