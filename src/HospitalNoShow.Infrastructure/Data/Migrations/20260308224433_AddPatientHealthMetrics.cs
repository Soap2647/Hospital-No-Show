using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalNoShow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientHealthMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BloodType",
                schema: "hospital",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HeightCm",
                schema: "hospital",
                table: "Patients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WeightKg",
                schema: "hospital",
                table: "Patients",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BloodType",
                schema: "hospital",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                schema: "hospital",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                schema: "hospital",
                table: "Patients");
        }
    }
}
