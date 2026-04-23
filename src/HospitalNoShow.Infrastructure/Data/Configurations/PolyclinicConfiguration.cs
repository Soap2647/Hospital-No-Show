using HospitalNoShow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalNoShow.Infrastructure.Data.Configurations;

public class PolyclinicConfiguration : IEntityTypeConfiguration<Polyclinic>
{
    public void Configure(EntityTypeBuilder<Polyclinic> builder)
    {
        builder.ToTable("Polyclinics");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Department)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Floor)
            .HasMaxLength(20);

        builder.Property(p => p.RoomNumber)
            .HasMaxLength(20);

        builder.Property(p => p.PhoneExtension)
            .HasMaxLength(10);
    }
}
