using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.DataAccess.Entities;

namespace SchoolAdmission.DataAccess.Configurations;

internal sealed class HoldEntityConfiguration : IEntityTypeConfiguration<HoldEntity>
{
    public void Configure(EntityTypeBuilder<HoldEntity> builder)
    {
        builder.ToTable("holds", table =>
            table.HasCheckConstraint("ck_holds_student_id_format", "student_id ~ '^[0-9]{17}$'"));

        builder.HasKey(h => h.Id).HasName("pk_holds");
        builder.Property(h => h.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(h => h.ClassId).HasColumnName("class_id");
        builder.Property(h => h.StudentId).HasColumnName("student_id").HasColumnType("text").IsRequired();
        builder.Property(h => h.Status).HasColumnName("status").HasColumnType("text").IsRequired();
        builder.Property(h => h.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz");
        builder.Property(h => h.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        builder.HasOne<ClassEntity>()
            .WithMany()
            .HasForeignKey(h => h.ClassId)
            .HasConstraintName("fk_holds_classes_class_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => h.ClassId).HasDatabaseName("ix_holds_class_id");

        builder.HasIndex(h => new { h.StudentId, h.ClassId })
            .IsUnique()
            .HasDatabaseName(DbNames.ActiveHoldIndex)
            .HasFilter("status IN ('HELD','CONFIRMED')");
    }
}
