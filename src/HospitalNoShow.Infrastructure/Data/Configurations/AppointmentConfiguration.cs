using HospitalNoShow.Domain.Entities;
using HospitalNoShow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalNoShow.Infrastructure.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(AppointmentStatus.Scheduled);

        builder.Property(a => a.CancellationReason)
            .HasMaxLength(500);

        builder.Property(a => a.Notes)
            .HasMaxLength(1000);

        builder.Property(a => a.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(a => new { a.DoctorId, a.AppointmentDate, a.AppointmentTime })
            .IsUnique()
            .HasDatabaseName("IX_Appointments_Doctor_DateTime");

        builder.HasIndex(a => a.PatientId)
            .HasDatabaseName("IX_Appointments_PatientId");

        builder.HasIndex(a => a.Status)
            .HasDatabaseName("IX_Appointments_Status");

        builder.HasIndex(a => a.AppointmentDate)
            .HasDatabaseName("IX_Appointments_Date");

        // Relationships
        builder.HasOne(a => a.NoShowAnalytics)
            .WithOne(n => n.Appointment)
            .HasForeignKey<NoShowAnalytics>(n => n.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
