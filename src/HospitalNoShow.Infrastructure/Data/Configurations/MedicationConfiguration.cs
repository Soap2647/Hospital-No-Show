using HospitalNoShow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalNoShow.Infrastructure.Data.Configurations;

public class MedicationConfiguration : IEntityTypeConfiguration<Medication>
{
    public void Configure(EntityTypeBuilder<Medication> builder)
    {
        builder.ToTable("Medications");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Dosage)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Frequency)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Notes)
            .HasMaxLength(500);

        builder.Ignore(m => m.IsOngoing);
    }
}
