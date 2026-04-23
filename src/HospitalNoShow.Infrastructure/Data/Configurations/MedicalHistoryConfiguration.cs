using HospitalNoShow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalNoShow.Infrastructure.Data.Configurations;

public class MedicalHistoryConfiguration : IEntityTypeConfiguration<MedicalHistory>
{
    public void Configure(EntityTypeBuilder<MedicalHistory> builder)
    {
        builder.ToTable("MedicalHistories");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.DiagnosisCode)
            .IsRequired()
            .HasMaxLength(10); // ICD-10 kodu maks 7 karakter

        builder.Property(m => m.DiagnosisName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.DiagnosisDescription)
            .HasMaxLength(2000);

        builder.HasMany(m => m.Medications)
            .WithOne(med => med.MedicalHistory)
            .HasForeignKey(med => med.MedicalHistoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.PatientId, m.DiagnosisCode })
            .HasDatabaseName("IX_MedicalHistory_Patient_Diagnosis");
    }
}
