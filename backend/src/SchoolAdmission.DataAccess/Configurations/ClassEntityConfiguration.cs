using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.DataAccess.Entities;

namespace SchoolAdmission.DataAccess.Configurations;

internal sealed class ClassEntityConfiguration : IEntityTypeConfiguration<ClassEntity>
{
    public void Configure(EntityTypeBuilder<ClassEntity> builder)
    {
        builder.ToTable("classes", table =>
        {
            table.HasCheckConstraint(DbNames.TotalSeatsCheck, "total_seats BETWEEN 1 AND 100000");
            table.HasCheckConstraint(DbNames.ClassNameCheck, "char_length(name) BETWEEN 1 AND 200");
        });
        builder.HasKey(c => c.Id).HasName("pk_classes");
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(c => c.Name).HasColumnName("name").HasColumnType("text").IsRequired();
        builder.Property(c => c.TotalSeats).HasColumnName("total_seats");
    }
}
