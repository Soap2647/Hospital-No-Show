using HospitalNoShow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalNoShow.Infrastructure.Data.Configurations;

public class NoShowAnalyticsConfiguration : IEntityTypeConfiguration<NoShowAnalytics>
{
    public void Configure(EntityTypeBuilder<NoShowAnalytics> builder)
    {
        builder.ToTable("NoShowAnalytics");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.RiskScore)
            .HasColumnType("decimal(5, 4)")
            .HasDefaultValue(0.0);

        builder.Property(n => n.PreviousNoShowRateWeight)
            .HasColumnType("decimal(5, 4)");

        builder.Property(n => n.AgeGroupWeight)
            .HasColumnType("decimal(5, 4)");

        builder.Property(n => n.DistanceWeight)
            .HasColumnType("decimal(5, 4)");

        builder.Property(n => n.AppointmentTimeWeight)
            .HasColumnType("decimal(5, 4)");

        builder.Property(n => n.AppointmentDayWeight)
            .HasColumnType("decimal(5, 4)");

        builder.Property(n => n.WeatherWeight)
            .HasColumnType("decimal(5, 4)");

        builder.Property(n => n.SmsResponseWeight)
            .HasColumnType("decimal(5, 4)");

        builder.Property(n => n.InsuranceTypeWeight)
            .HasColumnType("decimal(5, 4)");

        builder.Property(n => n.FirstVisitWeight)
            .HasColumnType("decimal(5, 4)");

        builder.Property(n => n.WeatherCondition)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(n => n.SmsResponse)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(n => n.CalculatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Ignored computed properties
        builder.Ignore(n => n.RiskLevel);

        // Indexes
        builder.HasIndex(n => n.AppointmentId).IsUnique();
        builder.HasIndex(n => n.RiskScore).HasDatabaseName("IX_NoShowAnalytics_RiskScore");
    }
}
