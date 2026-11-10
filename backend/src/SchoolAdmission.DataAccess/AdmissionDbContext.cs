using Microsoft.EntityFrameworkCore;
using SchoolAdmission.DataAccess.Entities;

namespace SchoolAdmission.DataAccess;

public sealed class AdmissionDbContext : DbContext
{
    public AdmissionDbContext(DbContextOptions<AdmissionDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public DbSet<ClassEntity> Classes => Set<ClassEntity>();

    public DbSet<HoldEntity> Holds => Set<HoldEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdmissionDbContext).Assembly);
}
