using HospitalNoShow.API.Data;
using HospitalNoShow.Domain.Entities;
using HospitalNoShow.Domain.Enums;
using HospitalNoShow.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HospitalNoShow.API.Extensions;

public static class DatabaseSeedExtensions
{
    public static async Task MigrateAndSeedAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migration completed.");

            await SeedRolesAsync(services, logger);
            await SeedAdminUserAsync(services, logger);
            await SeedPolyclinicsAsync(context, logger);
            await DemoDataSeeder.SeedDemoDataAsync(services, logger);

            // Özel demo kullanıcıları — her zaman çalışır (idempotent)
            await SeedSpecialUsersAsync(services, context, logger);

            // Toplu veri: 5000+ randevu hedef
            await DemoDataSeeder.SeedBulkAppointmentsAsync(context, logger, targetCount: 5000);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database migration and seeding.");
            throw;
        }
    }

    private static async Task SeedRolesAsync(IServiceProvider services, ILogger logger)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = ["Admin", "Doctor", "Patient"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Role created: {Role}", role);
            }
        }
    }

    private static async Task SeedAdminUserAsync(IServiceProvider services, ILogger logger)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        const string adminEmail = "admin@hospital.com";

        if (await userManager.FindByEmailAsync(adminEmail) is not null) return;

        var admin = new ApplicationUser
        {
            Email          = adminEmail,
            UserName       = adminEmail,
            FirstName      = "Sistem",
            LastName       = "Admin",
            EmailConfirmed = true,
            IsActive       = true
        };

        var result = await userManager.CreateAsync(admin, "Admin@12345");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
            logger.LogInformation("Admin user seeded: {Email}", adminEmail);
        }
        else
        {
            logger.LogError("Failed to create admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task SeedPolyclinicsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Polyclinics.AnyAsync()) return;

        var polyclinics = new[]
        {
            new Polyclinic { Name = "Kardiyoloji Polikliniği",      Department = "Kardiyoloji", Floor = "3", RoomNumber = "301", PhoneExtension = "301" },
            new Polyclinic { Name = "Dahiliye Polikliniği",         Department = "Dahiliye",    Floor = "2", RoomNumber = "201", PhoneExtension = "201" },
            new Polyclinic { Name = "Ortopedi Polikliniği",         Department = "Ortopedi",    Floor = "4", RoomNumber = "401", PhoneExtension = "401" },
            new Polyclinic { Name = "Nöroloji Polikliniği",         Department = "Nöroloji",    Floor = "3", RoomNumber = "305", PhoneExtension = "305" },
            new Polyclinic { Name = "Göz Hastalıkları Polikliniği", Department = "Göz",         Floor = "1", RoomNumber = "105", PhoneExtension = "105" },
        };

        context.Polyclinics.AddRange(polyclinics);
        await context.SaveChangesAsync();
        logger.LogInformation("Polyclinics seeded: {Count}", polyclinics.Length);
    }

    // ── Özel Demo Kullanıcıları ────────────────────────────────────────────────
    // Sunum için özel hesaplar. Bu metot her başlatmada çalışır; varsa atlar.

    private static async Task SeedSpecialUsersAsync(
        IServiceProvider services,
        ApplicationDbContext context,
        ILogger logger)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // 1. Cildiye Polikliniği — yoksa oluştur
        var cildiyePoly = await context.Polyclinics
            .FirstOrDefaultAsync(p => p.Department == "Cildiye");

        if (cildiyePoly is null)
        {
            cildiyePoly = new Polyclinic
            {
                Name           = "Dermatoloji (Cildiye) Polikliniği",
                Department     = "Cildiye",
                Floor          = "2",
                RoomNumber     = "215",
                PhoneExtension = "215"
            };
            context.Polyclinics.Add(cildiyePoly);
            await context.SaveChangesAsync();
            logger.LogInformation("Cildiye polyklinik eklendi.");
        }

        // 2. Dr. Handenur Yılmaz — Dermatoloji (Cildiye)
        const string doctorEmail    = "handenur.yilmaz@hospital.com";
        const string doctorPassword = "Doktor@2026";
        
        var existingDoctorUser = await userManager.FindByEmailAsync(doctorEmail);
        ApplicationUser doctorUser;

        if (existingDoctorUser is null)
        {
            doctorUser = new ApplicationUser
            {
                Email          = doctorEmail,
                UserName       = doctorEmail,
                FirstName      = "Handenur",
                LastName       = "Yılmaz",
                PhoneNumber    = "05550522647",
                EmailConfirmed = true,
                IsActive       = true
            };

            var res = await userManager.CreateAsync(doctorUser, doctorPassword);
            if (res.Succeeded)
            {
                await userManager.AddToRoleAsync(doctorUser, "Doctor");
            }
        }
        else
        {
            doctorUser = existingDoctorUser;
            doctorUser.PhoneNumber = "05550522647";
            await userManager.UpdateAsync(doctorUser);
        }

        var doctorEntity = await context.Doctors.Include(d => d.Schedules).FirstOrDefaultAsync(d => d.UserId == doctorUser.Id);
        if (doctorEntity is null)
        {
            doctorEntity = new Doctor
            {
                UserId                            = doctorUser.Id,
                Specialty                         = "Dermatoloji",
                Title                             = "Dr.",
                DiplomaCertificateNumber          = "TC9001",
                PolyclinicId                      = cildiyePoly.Id,
                MaxDailyPatients                  = 18,
                AverageAppointmentDurationMinutes = 15
            };
            context.Doctors.Add(doctorEntity);
            await context.SaveChangesAsync();
            logger.LogInformation("Özel doktor eklendi: {Email}", doctorEmail);
        }

        // Sahte randevular daha önce oluşturulmadıysa oluştur (her başlatmada kontrol et)
        var existingAptCount = await context.Appointments.CountAsync(a => a.DoctorId == doctorEntity.Id);
        if (existingAptCount < 30)
        {
            await GenerateFakeAppointmentsForDoctor(context, doctorEntity.Id, logger);
            logger.LogInformation("Dr. Handenur için sahte randevular oluşturuldu.");
        }

        // 3. Mert Yasin Yılmaz — Hasta
        // Giriş: mertyasin.yilmaz@test.com / Hasta@2026
        const string patientEmail    = "mertyasin.yilmaz@test.com";
        const string patientPassword = "Hasta@2026";

        var existingPatientUser = await userManager.FindByEmailAsync(patientEmail);
        ApplicationUser patientUser;

        if (existingPatientUser is null)
        {
            patientUser = new ApplicationUser
            {
                Email          = patientEmail,
                UserName       = patientEmail,
                FirstName      = "Mert Yasin",
                LastName       = "Yılmaz",
                PhoneNumber    = "05550522647",
                EmailConfirmed = true,
                IsActive       = true
            };

            var res = await userManager.CreateAsync(patientUser, patientPassword);
            if (res.Succeeded)
            {
                await userManager.AddToRoleAsync(patientUser, "Patient");
            }
        }
        else
        {
            patientUser = existingPatientUser;
            patientUser.PhoneNumber = "05550522647";
            await userManager.UpdateAsync(patientUser);
        }

        var patientEntity = await context.Patients.FirstOrDefaultAsync(p => p.UserId == patientUser.Id);
        if (patientEntity is null)
        {
            patientEntity = new Patient
            {
                UserId               = patientUser.Id,
                IdentityNumber       = "12345678901",
                DateOfBirth          = new DateOnly(2003, 9, 12), // 22 yaşında
                Gender               = Gender.Male,
                PhoneNumber          = "05550522647",
                Address              = "Kastamonu Merkez",
                City                 = "Kastamonu",
                DistanceToHospitalKm = 4.5,
                InsuranceType        = InsuranceType.SGK,
                HasChronicDisease    = false,
                TotalAppointments    = 0,
                NoShowCount          = 0
            };
            context.Patients.Add(patientEntity);
            await context.SaveChangesAsync();
            logger.LogInformation("Özel hasta eklendi: {Email}", patientEmail);
        }
        else
        {
            patientEntity.PhoneNumber = "05550522647";
            patientEntity.City        = "Kastamonu";
            patientEntity.Address     = "Kastamonu Merkez";
            patientEntity.DateOfBirth = new DateOnly(2003, 9, 12); // 22 yaşında
            context.Patients.Update(patientEntity);
            await context.SaveChangesAsync();
        }

        // Mert Yasin için kişisel randevu geçmişi
        var existingMertApts = await context.Appointments.AnyAsync(a => a.PatientId == patientEntity.Id && a.Notes != null && a.Notes.Contains("[SEED-MERT]"));
        if (!existingMertApts)
            await SeedPatientAppointmentHistoryAsync(context, patientEntity.Id, doctorEntity.Id, logger);

        // [BACKFILL] Eksik NoShowAnalytics kayıtlarını tamamla
        var appointmentsWithoutAnalytics = await context.Appointments
            .Include(a => a.NoShowAnalytics)
            .Where(a => a.NoShowAnalytics == null)
            .ToListAsync();

        if (appointmentsWithoutAnalytics.Any())
        {
            var rnd = new Random(123);
            foreach (var app in appointmentsWithoutAnalytics)
            {
                context.Set<NoShowAnalytics>().Add(new NoShowAnalytics
                {
                    AppointmentId = app.Id,
                    RiskScore = rnd.NextDouble() * 0.9 + 0.1,
                    PreviousNoShowRateWeight = rnd.NextDouble(),
                    DistanceWeight = rnd.NextDouble(),
                    AgeGroupWeight = rnd.NextDouble()
                });
            }
            await context.SaveChangesAsync();
            logger.LogInformation("{Count} adet eksik NoShowAnalytics kaydı tamamlandı.", appointmentsWithoutAnalytics.Count);
        }
    }

    private static async Task GenerateFakeAppointmentsForDoctor(ApplicationDbContext context, int doctorId, ILogger logger)
    {
        // Yeterli rastgele hasta yoksa ilk 5 hastayı al
        var patients = await context.Patients.Take(5).ToListAsync();
        if (!patients.Any()) return;

        var random = new Random(12345);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var statuses = new[] { AppointmentStatus.Completed, AppointmentStatus.NoShow, AppointmentStatus.Cancelled };
        
        var newAppointments = new List<Appointment>();

        // Geçmiş 30 gün için randevular (Dashboard grafikleri için)
        for (int i = 1; i <= 30; i++)
        {
            var date = today.AddDays(-i);
            if (date.DayOfWeek == DayOfWeek.Sunday || date.DayOfWeek == DayOfWeek.Saturday) continue;

            int dailyAptCount = random.Next(5, 12);
            for (int j = 0; j < dailyAptCount; j++)
            {
                var patient = patients[random.Next(patients.Count)];
                var time = new TimeOnly(9, 0).AddMinutes(j * 15);
                var status = statuses[random.Next(statuses.Length)]; // Karışık durumlar
                
                newAppointments.Add(new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId  = doctorId,
                    AppointmentDate = date.ToDateTime(TimeOnly.MinValue), // Fix DateOnly to DateTime
                    AppointmentTime = time,
                    Status = status,
                    Notes = "Sistem tarafından otomatik oluşturuldu.",
                    CreatedAt = date.ToDateTime(time).AddDays(-5)
                });

                if (status == AppointmentStatus.Completed)
                {
                    patient.TotalAppointments++;
                }
                else if (status == AppointmentStatus.NoShow)
                {
                    patient.TotalAppointments++;
                    patient.NoShowCount++;
                }
            }
        }

        // Gelecek 5 gün için randevular (Doktor paneli için)
        for (int i = 0; i < 5; i++)
        {
            var date = today.AddDays(i);
            if (date.DayOfWeek == DayOfWeek.Sunday || date.DayOfWeek == DayOfWeek.Saturday) continue;

            int dailyAptCount = random.Next(3, 8);
            for (int j = 0; j < dailyAptCount; j++)
            {
                var patient = patients[random.Next(patients.Count)];
                var time = new TimeOnly(9, 0).AddMinutes(j * 15);
                
                newAppointments.Add(new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId  = doctorId,
                    AppointmentDate = date.ToDateTime(TimeOnly.MinValue), // Fix DateOnly to DateTime
                    AppointmentTime = time,
                    Status = AppointmentStatus.Scheduled,
                    Notes = "Randevu onayı bekliyor.",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                });
            }
        }

        context.Appointments.AddRange(newAppointments);
        await context.SaveChangesAsync();
        logger.LogInformation("Dr. Handenur için {Count} adet sahte randevu oluşturuldu.", newAppointments.Count);
    }

    private static async Task SeedPatientAppointmentHistoryAsync(
        ApplicationDbContext context,
        int mertPatientId,
        int handenurDoctorId,
        ILogger logger)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var appointments = new List<Appointment>();

        // Handenur Yılmaz ile geçmiş randevular (6 ay)
        var handenurPastVisits = new[]
        {
            (today.AddDays(-178), new TimeOnly(9,  0),  AppointmentStatus.Completed, "Yıllık deri kontrolü."),
            (today.AddDays(-145), new TimeOnly(10, 15), AppointmentStatus.Completed, "Akne tedavisi takibi."),
            (today.AddDays(-112), new TimeOnly(11, 30), AppointmentStatus.NoShow,    "Doktor randevusuna gelmedi."),
            (today.AddDays(-82),  new TimeOnly(9,  30), AppointmentStatus.Completed, "Kontrol muayenesi."),
            (today.AddDays(-55),  new TimeOnly(14, 0),  AppointmentStatus.Completed, "Egzama tedavisi."),
            (today.AddDays(-30),  new TimeOnly(10, 45), AppointmentStatus.NoShow,    "Haber verilmedi."),
            (today.AddDays(-14),  new TimeOnly(11, 0),  AppointmentStatus.Completed, "Son kontrol, ilaç reçetesi."),
        };

        foreach (var (date, time, status, note) in handenurPastVisits)
        {
            appointments.Add(new Appointment
            {
                PatientId       = mertPatientId,
                DoctorId        = handenurDoctorId,
                AppointmentDate = date.ToDateTime(TimeOnly.MinValue),
                AppointmentTime = time,
                Status          = status,
                Notes           = $"{note} [SEED-MERT]",
                CreatedAt       = date.ToDateTime(time).AddDays(-3)
            });
        }

        // Gelecekte Handenur Yılmaz'dan alınmış randevular
        var handenurFutureVisits = new[]
        {
            (today.AddDays(7),  new TimeOnly(9,  0),  "Kontrol randevusu."),
            (today.AddDays(21), new TimeOnly(11, 15), "Yeni tedavi planı görüşmesi."),
        };

        foreach (var (date, time, note) in handenurFutureVisits)
        {
            appointments.Add(new Appointment
            {
                PatientId       = mertPatientId,
                DoctorId        = handenurDoctorId,
                AppointmentDate = date.ToDateTime(TimeOnly.MinValue),
                AppointmentTime = time,
                Status          = AppointmentStatus.Scheduled,
                Notes           = $"{note} [SEED-MERT]",
                CreatedAt       = DateTime.Now.AddDays(-1)
            });
        }

        // Diğer doktorlarla randevular (daha gerçekçi hasta geçmişi)
        var otherDoctors = await context.Doctors
            .Where(d => d.Id != handenurDoctorId)
            .Take(4)
            .Select(d => d.Id)
            .ToListAsync();

        var otherVisits = new[]
        {
            (today.AddDays(-200), new TimeOnly(10, 0),  AppointmentStatus.Completed, "Kardiyoloji kontrol."),
            (today.AddDays(-160), new TimeOnly(9,  30), AppointmentStatus.Completed, "Dahiliye muayenesi."),
            (today.AddDays(-130), new TimeOnly(11, 0),  AppointmentStatus.NoShow,    "Randevuya gelmedi."),
            (today.AddDays(-95),  new TimeOnly(14, 0),  AppointmentStatus.Completed, "Göz kontrolü."),
            (today.AddDays(-60),  new TimeOnly(10, 30), AppointmentStatus.Cancelled, "Hasta tarafından iptal edildi."),
            (today.AddDays(-20),  new TimeOnly(9,  0),  AppointmentStatus.Completed, "Genel kontrol."),
        };

        for (int i = 0; i < otherVisits.Length && i < otherDoctors.Count; i++)
        {
            var (date, time, status, note) = otherVisits[i];
            appointments.Add(new Appointment
            {
                PatientId       = mertPatientId,
                DoctorId        = otherDoctors[i % otherDoctors.Count],
                AppointmentDate = date.ToDateTime(TimeOnly.MinValue),
                AppointmentTime = time,
                Status          = status,
                Notes           = $"{note} [SEED-MERT]",
                CreatedAt       = date.ToDateTime(time).AddDays(-4)
            });
        }

        // Gelecekte diğer doktorlarla planlanan randevular
        if (otherDoctors.Count >= 2)
        {
            appointments.Add(new Appointment
            {
                PatientId       = mertPatientId,
                DoctorId        = otherDoctors[0],
                AppointmentDate = today.AddDays(14).ToDateTime(TimeOnly.MinValue),
                AppointmentTime = new TimeOnly(10, 0),
                Status          = AppointmentStatus.Scheduled,
                Notes           = "Kardiyoloji takip randevusu. [SEED-MERT]",
                CreatedAt       = DateTime.Now.AddDays(-2)
            });
        }

        context.Appointments.AddRange(appointments);
        await context.SaveChangesAsync();

        // İstatistikleri güncelle
        var completedCount = appointments.Count(a => a.Status == AppointmentStatus.Completed);
        var noShowCount    = appointments.Count(a => a.Status == AppointmentStatus.NoShow);
        var patient        = await context.Patients.FindAsync(mertPatientId);
        if (patient != null)
        {
            patient.TotalAppointments += completedCount + noShowCount;
            patient.NoShowCount       += noShowCount;
            await context.SaveChangesAsync();
        }

        logger.LogInformation("Mert Yasin için {Count} adet kişisel randevu oluşturuldu.", appointments.Count);
    }
}
