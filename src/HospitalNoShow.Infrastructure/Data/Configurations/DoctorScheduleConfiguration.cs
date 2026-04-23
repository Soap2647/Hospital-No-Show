using HospitalNoShow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalNoShow.Infrastructure.Data.Configurations;

public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.ToTable("DoctorSchedules");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.DayOfWeek)
            .HasConversion<string>()
            .HasMaxLength(15);

        // Her doktor-gün kombinasyonu için tek kayıt
        builder.HasIndex(s => new { s.DoctorId, s.DayOfWeek })
            .IsUnique()
            .HasDatabaseName("IX_DoctorSchedule_Doctor_Day");
    }
}
