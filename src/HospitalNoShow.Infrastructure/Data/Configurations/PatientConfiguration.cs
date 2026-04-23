using HospitalNoShow.Domain.Entities;
using HospitalNoShow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalNoShow.Infrastructure.Data.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();

        builder.Property(p => p.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(p => p.IdentityNumber)
            .IsRequired()
            .HasMaxLength(11)
            .IsFixedLength();

        builder.Property(p => p.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.Address)
            .HasMaxLength(500);

        builder.Property(p => p.City)
            .HasMaxLength(100);

        builder.Property(p => p.Gender)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.InsuranceType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(p => p.InsurancePolicyNumber)
            .HasMaxLength(50);

        builder.Property(p => p.ChronicDiseaseNotes)
            .HasMaxLength(1000);

        builder.Property(p => p.DistanceToHospitalKm)
            .HasColumnType("decimal(10, 2)");

        builder.Property(p => p.NoShowCount)
            .HasDefaultValue(0);

        builder.Property(p => p.TotalAppointments)
            .HasDefaultValue(0);

        // Ignored computed properties
        builder.Ignore(p => p.Age);
        builder.Ignore(p => p.NoShowRate);

        // Indexes
        builder.HasIndex(p => p.UserId).IsUnique();
        builder.HasIndex(p => p.IdentityNumber).IsUnique();

        // Relationships
        builder.HasOne(p => p.User)
            .WithOne(u => u.Patient)
            .HasForeignKey<Patient>(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Appointments)
            .WithOne(a => a.Patient)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.MedicalHistories)
            .WithOne(m => m.Patient)
            .HasForeignKey(m => m.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
