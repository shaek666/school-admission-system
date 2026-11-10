using Microsoft.EntityFrameworkCore;
using SchoolAdmission.DataAccess.Entities;

namespace SchoolAdmission.DataAccess;

public sealed class AdmissionDbContext(DbContextOptions<AdmissionDbContext> options) : DbContext(options)
{
    public DbSet<ClassEntity> Classes => Set<ClassEntity>();

    public DbSet<HoldEntity> Holds => Set<HoldEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdmissionDbContext).Assembly);
}
