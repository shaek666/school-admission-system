using System.Xml.Linq;

namespace SchoolAdmission.Business.Tests;

public class LayerRulesTests
{
    [Fact]
    public void Business_references_no_lower_layer()
    {
        var names = typeof(SchoolAdmission.Business.AppException).Assembly
            .GetReferencedAssemblies().Select(a => a.Name!).ToList();

        Assert.DoesNotContain("SchoolAdmission.DataAccess", names);
        Assert.DoesNotContain(names, n => n.StartsWith("Npgsql") || n.StartsWith("Microsoft.EntityFrameworkCore"));
    }

    [Fact]
    public void Business_project_references_no_project() =>
        Assert.Empty(ProjectReferences("SchoolAdmission.Business"));

    [Fact]
    public void DataAccess_project_references_only_Business() =>
        Assert.Equal(["SchoolAdmission.Business"], ProjectReferences("SchoolAdmission.DataAccess"));

    [Fact]
    public void Api_project_references_Business_and_DataAccess() =>
        Assert.Equal(
            ["SchoolAdmission.Business", "SchoolAdmission.DataAccess"],
            ProjectReferences("SchoolAdmission.Api"));

    private static string[] ProjectReferences(string project)
    {
        // The solution folder is `backend/` on the host and `/src` in the test container.
        var backend = new DirectoryInfo(AppContext.BaseDirectory);
        while (backend is not null && !File.Exists(Path.Combine(backend.FullName, "SchoolAdmission.sln")))
            backend = backend.Parent;
        Assert.NotNull(backend);

        var csproj = Path.Combine(backend.FullName, "src", project, project + ".csproj");
        return XDocument.Load(csproj)
            .Descendants("ProjectReference")
            .Select(e => Path.GetFileNameWithoutExtension(((string)e.Attribute("Include")!).Replace('\\', '/')))
            .Order()
            .ToArray();
    }
}
