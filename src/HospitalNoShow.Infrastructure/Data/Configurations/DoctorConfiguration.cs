using HospitalNoShow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalNoShow.Infrastructure.Data.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(d => d.Specialty)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.Title)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.DiplomaCertificateNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.MaxDailyPatients)
            .HasDefaultValue(20);

        builder.Property(d => d.AverageAppointmentDurationMinutes)
            .HasDefaultValue(15);

        // Indexes
        builder.HasIndex(d => d.UserId).IsUnique();
        builder.HasIndex(d => d.DiplomaCertificateNumber).IsUnique();

        // Relationships
        builder.HasOne(d => d.User)
            .WithOne(u => u.Doctor)
            .HasForeignKey<Doctor>(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Polyclinic)
            .WithMany(p => p.Doctors)
            .HasForeignKey(d => d.PolyclinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Appointments)
            .WithOne(a => a.Doctor)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Schedules)
            .WithOne(s => s.Doctor)
            .HasForeignKey(s => s.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
