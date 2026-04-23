using HospitalNoShow.Domain.Entities;
using HospitalNoShow.Domain.Enums;
using HospitalNoShow.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HospitalNoShow.API.Data;

/// <summary>
/// Demo veri tohumlama servisi.
/// 10 doktor, 150 Türk hasta ve 600+ randevu oluşturur.
/// İdempotent: Zaten veri varsa çalışmaz.
/// </summary>
public static class DemoDataSeeder
{
    private static readonly Random Rng = new(42); // Reproducible seed

    // ── Türkçe isim havuzu ────────────────────────────────────────────────────

    private static readonly string[] MaleFirstNames =
    [
        "Ahmet", "Mehmet", "Mustafa", "İbrahim", "Ali", "Hüseyin", "Hasan",
        "İsmail", "Ömer", "Yusuf", "Murat", "Serkan", "Emre", "Burak",
        "Fatih", "Onur", "Berkay", "Arda", "Kaan", "Baran", "Tarık",
        "Selim", "Berk", "Efe", "Alper", "Okan", "Sinan", "Cem",
        "Tolga", "Uğur", "Volkan", "Caner", "Levent", "Mert", "Oğuz"
    ];

    private static readonly string[] FemaleFirstNames =
    [
        "Fatma", "Ayşe", "Emine", "Hatice", "Zeynep", "Elif", "Meryem",
        "Şükran", "Esra", "Selin", "Nilüfer", "Büşra", "Gamze", "Duygu",
        "Özlem", "Gülcan", "Şeyma", "Merve", "Seda", "Arzu", "Gülay",
        "Leyla", "Ceylan", "Yıldız", "İpek", "Cansu", "Tuğçe", "Ebru",
        "Naz", "Pınar", "Sibel", "Nuray", "Sevda", "Aslı", "Deniz"
    ];

    private static readonly string[] Surnames =
    [
        "Yılmaz", "Kaya", "Demir", "Şahin", "Çelik", "Yıldız", "Yıldırım",
        "Öztürk", "Aydın", "Özdemir", "Arslan", "Doğan", "Kılıç", "Aslan",
        "Çetin", "Korkmaz", "Özkan", "Şimşek", "Aktaş", "Güneş", "Çakır",
        "Kaplan", "Yalçın", "Bulut", "Acar", "Erdem", "Polat", "Coşkun",
        "Kurt", "Taş", "Koç", "Dinç", "Ekinci", "Bal", "Toprak",
        "Özçelik", "Uzun", "Ay", "Bozkurt", "Altın"
    ];

    private static readonly string[] Cities =
    [
        "İstanbul", "Ankara", "İzmir", "Bursa", "Antalya",
        "Adana", "Konya", "Gaziantep", "Mersin", "Kayseri"
    ];

    // ── Doktor tanımları ──────────────────────────────────────────────────────

    private record DoctorDef(
        string FirstName, string LastName,
        string Title, string Specialty,
        string Department, int MaxDaily, int Duration);

    private static readonly DoctorDef[] DoctorDefinitions =
    [
        new("Ahmet",    "Kaya",     "Prof. Dr.", "Kardiyoloji",       "Kardiyoloji",      20, 20),
        new("Mehmet",   "Yılmaz",   "Doç. Dr.", "Kardiyoloji",       "Kardiyoloji",      18, 20),
        new("Ayşe",     "Demir",    "Prof. Dr.", "Dahiliye",          "Dahiliye",         25, 15),
        new("Fatma",    "Çelik",    "Dr.",       "Dahiliye",          "Dahiliye",         22, 15),
        new("Mustafa",  "Şahin",    "Doç. Dr.", "Ortopedi",          "Ortopedi",         18, 25),
        new("Ali",      "Arslan",   "Dr.",       "Ortopedi",          "Ortopedi",         16, 25),
        new("Zeynep",   "Aydın",    "Prof. Dr.", "Nöroloji",          "Nöroloji",         15, 30),
        new("İbrahim",  "Öztürk",   "Doç. Dr.", "Nöroloji",          "Nöroloji",         14, 30),
        new("Elif",     "Doğan",    "Dr.",       "Göz Hastalıkları",  "Göz",              20, 15),
        new("Hüseyin",  "Korkmaz",  "Doç. Dr.", "Göz Hastalıkları",  "Göz",              20, 15),
    ];

    // ── Slot saatleri ─────────────────────────────────────────────────────────

    private static readonly TimeOnly[] MorningSlots =
    [
        new(08, 30), new(09, 00), new(09, 30), new(10, 00),
        new(10, 30), new(11, 00), new(11, 30), new(12, 00)
    ];

    private static readonly TimeOnly[] AfternoonSlots =
    [
        new(13, 00), new(13, 30), new(14, 00), new(14, 30),
        new(15, 00), new(15, 30), new(16, 00), new(16, 30)
    ];

    // ── Ana giriş noktası ─────────────────────────────────────────────────────

    public static async Task SeedDemoDataAsync(
        IServiceProvider services,
        ILogger logger)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Randevular zaten varsa tamamen atla
        if (await context.Appointments.AnyAsync())
        {
            logger.LogInformation("Demo data already exists, skipping.");
            return;
        }

        logger.LogInformation("Seeding demo data...");

        var polyclinics = await context.Polyclinics.ToListAsync();
        if (polyclinics.Count == 0)
        {
            logger.LogWarning("Polyclinics not found, skipping demo seed.");
            return;
        }

        var doctors = await SeedDoctorsAsync(userManager, context, polyclinics, logger);
        var patients = await SeedPatientsAsync(userManager, context, logger);
        await SeedAppointmentsAsync(context, doctors, patients, logger);

        logger.LogInformation("Demo data seeding completed.");
    }

    // ── Doktorlar ─────────────────────────────────────────────────────────────

    private static async Task<List<Doctor>> SeedDoctorsAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        List<Polyclinic> polyclinics,
        ILogger logger)
    {
        // Doktorlar zaten varsa DB'den yükle
        if (await context.Doctors.AnyAsync())
        {
            var existing = await context.Doctors.ToListAsync();
            logger.LogInformation("Doctors already seeded: {Count}, loading from DB.", existing.Count);
            return existing;
        }

        var doctors = new List<Doctor>();
        int certCounter = 1000;

        // Her department için polyclinic eşle
        var polyMap = polyclinics.ToDictionary(
            p => p.Department,
            p => p,
            StringComparer.OrdinalIgnoreCase);

        foreach (var def in DoctorDefinitions)
        {
            var email = $"dr.{Normalize(def.FirstName)}.{Normalize(def.LastName)}@hospital.com";

            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser is not null) continue;

            var user = new ApplicationUser
            {
                Email = email,
                UserName = email,
                FirstName = def.FirstName,
                LastName = def.LastName,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, "Doctor@12345");
            if (!result.Succeeded)
            {
                logger.LogWarning("Failed to create doctor {Email}: {Errors}",
                    email, string.Join(", ", result.Errors.Select(e => e.Description)));
                continue;
            }

            await userManager.AddToRoleAsync(user, "Doctor");

            if (!polyMap.TryGetValue(def.Department, out var poly))
            {
                poly = polyclinics.First();
                logger.LogWarning("Polyclinic '{Dept}' not found, using fallback.", def.Department);
            }

            var doctor = new Doctor
            {
                UserId = user.Id,
                Specialty = def.Specialty,
                Title = def.Title,
                DiplomaCertificateNumber = $"TC{++certCounter}",
                PolyclinicId = poly.Id,
                MaxDailyPatients = def.MaxDaily,
                AverageAppointmentDurationMinutes = def.Duration
            };

            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();
            doctors.Add(doctor);
            logger.LogInformation("Doctor seeded: {Title} {Name}", def.Title, $"{def.FirstName} {def.LastName}");
        }

        return doctors;
    }

    // ── Hastalar ──────────────────────────────────────────────────────────────

    private static async Task<List<Patient>> SeedPatientsAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        ILogger logger)
    {
        // Hastalar zaten varsa DB'den yükle (kısmi seed durumu)
        if (await context.Patients.AnyAsync())
        {
            var existing = await context.Patients.ToListAsync();
            logger.LogInformation("Patients already seeded: {Count}, loading from DB.", existing.Count);
            return existing;
        }

        var patients = new List<Patient>();

        // 150 hasta: farklı profiller
        var profiles = BuildPatientProfiles();

        foreach (var (profile, idx) in profiles.Select((p, i) => (p, i)))
        {
            var paddedIdx = (idx + 1).ToString("D3");
            var email = $"hasta{paddedIdx}@test.com";

            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser is not null) continue;

            var user = new ApplicationUser
            {
                Email = email,
                UserName = email,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, "Hasta@12345");
            if (!result.Succeeded) continue;

            await userManager.AddToRoleAsync(user, "Patient");

            // TC kimlik no: benzersiz 11 haneli
            var tcNo = GenerateTcNo(idx);

            var patient = new Patient
            {
                UserId = user.Id,
                IdentityNumber = tcNo,
                DateOfBirth = profile.DateOfBirth,
                Gender = profile.Gender,
                PhoneNumber = GeneratePhone(),
                Address = $"{profile.City} Mahallesi, No:{Rng.Next(1, 100)}",
                City = profile.City,
                DistanceToHospitalKm = profile.Distance,
                InsuranceType = profile.Insurance,
                HasChronicDisease = profile.HasChronicDisease,
                ChronicDiseaseNotes = profile.HasChronicDisease ? "Diyabet, Hipertansiyon" : null,
                TotalAppointments = 0,
                NoShowCount = 0
            };

            context.Patients.Add(patient);
            await context.SaveChangesAsync();
            patients.Add(patient);
        }

        logger.LogInformation("Patients seeded: {Count}", patients.Count);
        return patients;
    }

    // ── Hasta profili oluşturucu ──────────────────────────────────────────────

    private record PatientProfile(
        string FirstName, string LastName,
        DateOnly DateOfBirth, Gender Gender,
        string City, double Distance,
        InsuranceType Insurance, bool HasChronicDisease,
        double TargetNoShowRate);

    private static List<PatientProfile> BuildPatientProfiles()
    {
        var profiles = new List<PatientProfile>();

        // Risk grupları:
        // Düşük risk (0-10%): 50 hasta
        // Orta risk (15-35%): 60 hasta
        // Yüksek risk (40-70%): 30 hasta
        // Çok yüksek (70%+): 10 hasta

        var groups = new[]
        {
            (count: 50, minRate: 0.00, maxRate: 0.10, minAge: 18, maxAge: 65, minDist: 1.0,  maxDist: 15.0),
            (count: 60, minRate: 0.15, maxRate: 0.35, minAge: 25, maxAge: 75, minDist: 5.0,  maxDist: 30.0),
            (count: 30, minRate: 0.40, maxRate: 0.65, minAge: 30, maxAge: 80, minDist: 10.0, maxDist: 45.0),
            (count: 10, minRate: 0.70, maxRate: 0.90, minAge: 20, maxAge: 70, minDist: 20.0, maxDist: 50.0),
        };

        var insurances = (InsuranceType[])Enum.GetValues(typeof(InsuranceType));
        int nameIdx = 0;

        foreach (var g in groups)
        {
            for (int i = 0; i < g.count; i++)
            {
                var gender = (nameIdx % 3 == 0) ? Gender.Female : Gender.Male;
                var firstName = gender == Gender.Female
                    ? FemaleFirstNames[nameIdx % FemaleFirstNames.Length]
                    : MaleFirstNames[nameIdx % MaleFirstNames.Length];
                var lastName = Surnames[(nameIdx * 7) % Surnames.Length];
                var city = Cities[nameIdx % Cities.Length];

                var age = Rng.Next(g.minAge, g.maxAge);
                var birthYear = DateTime.Today.Year - age;
                var dob = new DateOnly(birthYear, Rng.Next(1, 13), Rng.Next(1, 28));

                var distance = Math.Round(g.minDist + Rng.NextDouble() * (g.maxDist - g.minDist), 1);
                var noShowRate = Math.Round(g.minRate + Rng.NextDouble() * (g.maxRate - g.minRate), 2);
                var insurance = insurances[nameIdx % insurances.Length];
                var hasChronic = noShowRate > 0.4 ? Rng.Next(3) == 0 : false;

                profiles.Add(new PatientProfile(
                    firstName, lastName, dob, gender,
                    city, distance, insurance, hasChronic, noShowRate));

                nameIdx++;
            }
        }

        return profiles;
    }

    // ── Randevular ────────────────────────────────────────────────────────────

    private static async Task SeedAppointmentsAsync(
        ApplicationDbContext context,
        List<Doctor> doctors,
        List<Patient> patients,
        ILogger logger)
    {
        if (!doctors.Any() || !patients.Any()) return;

        var appointments = new List<Appointment>();
        var analytics = new List<NoShowAnalytics>();

        // Unique index: (DoctorId, AppointmentDate, AppointmentTime) — çakışmaları önle
        var usedSlots = new HashSet<(int DoctorId, DateTime Date, TimeOnly Time)>();

        var today = DateTime.Today;
        var pastStart = today.AddMonths(-6);
        var futureEnd = today.AddMonths(3);
        var allSlots = MorningSlots.Concat(AfternoonSlots).ToArray();

        // Her hasta için geçmiş ve gelecek randevular oluştur
        int patientIdx = 0;
        foreach (var patient in patients)
        {
            // Profil bazlı no-show oranını belirle
            var targetRate = patientIdx < 50 ? 0.05
                           : patientIdx < 110 ? 0.25
                           : patientIdx < 140 ? 0.52
                           : 0.78;

            // Geçmiş: 3-8 randevu
            int pastCount = Rng.Next(3, 9);
            int actualNoShows = 0;
            bool isFirstVisit = true;

            for (int i = 0; i < pastCount; i++)
            {
                var daysBack = Rng.Next(5, 180);
                var aptDate = today.AddDays(-daysBack);
                if (aptDate < pastStart) aptDate = pastStart.AddDays(Rng.Next(0, 10));

                // Hafta sonu atla
                while (aptDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    aptDate = aptDate.AddDays(1);

                // Boş slot bul (çakışma varsa farklı doktor/saat dene)
                Doctor? doctor = null;
                TimeOnly time = default;
                bool found = false;
                for (int attempt = 0; attempt < 20 && !found; attempt++)
                {
                    var d = doctors[Rng.Next(doctors.Count)];
                    var t = allSlots[Rng.Next(allSlots.Length)];
                    if (usedSlots.Add((d.Id, aptDate, t)))
                    {
                        doctor = d; time = t; found = true;
                    }
                }
                if (!found) continue; // Tüm slotlar dolu, bu randevuyu atla

                // No-show kararı
                bool willNoShow = Rng.NextDouble() < targetRate;
                if (willNoShow) actualNoShows++;

                var status = willNoShow
                    ? AppointmentStatus.NoShow
                    : (Rng.Next(10) == 0 ? AppointmentStatus.Cancelled : AppointmentStatus.Completed);

                var apt = new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId = doctor!.Id,
                    AppointmentDate = aptDate,
                    AppointmentTime = time,
                    Status = status,
                    IsFirstVisit = isFirstVisit,
                    SlotOrderInDay = Rng.Next(1, 9),
                    TotalSlotsInDay = Rng.Next(12, 20),
                    Notes = i == 0 ? "İlk başvuru" : null
                };
                appointments.Add(apt);
                isFirstVisit = false;
            }

            // Gelecek: 0-2 planlanmış randevu
            int futureCount = Rng.Next(0, 3);
            for (int i = 0; i < futureCount; i++)
            {
                var daysAhead = Rng.Next(1, 90);
                var aptDate = today.AddDays(daysAhead);
                if (aptDate > futureEnd) break;

                while (aptDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    aptDate = aptDate.AddDays(1);

                Doctor? doctor = null;
                TimeOnly time = default;
                bool found = false;
                for (int attempt = 0; attempt < 20 && !found; attempt++)
                {
                    var d = doctors[Rng.Next(doctors.Count)];
                    var t = allSlots[Rng.Next(allSlots.Length)];
                    if (usedSlots.Add((d.Id, aptDate, t)))
                    {
                        doctor = d; time = t; found = true;
                    }
                }
                if (!found) continue;

                appointments.Add(new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId = doctor!.Id,
                    AppointmentDate = aptDate,
                    AppointmentTime = time,
                    Status = AppointmentStatus.Scheduled,
                    IsFirstVisit = false,
                    SlotOrderInDay = Rng.Next(1, 9),
                    TotalSlotsInDay = Rng.Next(12, 20)
                });
            }

            // Hasta istatistiklerini güncelle
            int completedOrNoShow = pastCount;
            patient.TotalAppointments = completedOrNoShow;
            patient.NoShowCount = actualNoShows;
            patient.UpdatedAt = DateTime.UtcNow;

            patientIdx++;
        }

        context.Appointments.AddRange(appointments);
        await context.SaveChangesAsync();

        // NoShowAnalytics: tüm randevular için oluştur
        var savedAppointments = await context.Appointments
            .Include(a => a.Patient)
            .ToListAsync();

        foreach (var apt in savedAppointments)
        {
            var p = apt.Patient;
            double noShowRate = p.TotalAppointments > 0
                ? (double)p.NoShowCount / p.TotalAppointments : 0;

            double riskScore = CalculateRisk(p, apt, noShowRate);

            var analytic = new NoShowAnalytics
            {
                AppointmentId = apt.Id,
                RiskScore = Math.Clamp(riskScore, 0.0, 1.0),
                PreviousNoShowRateWeight = Math.Round(noShowRate * 0.35, 4),
                AgeGroupWeight = Math.Round(AgeGroupFactor(p.Age) * 0.10, 4),
                DistanceWeight = Math.Round(DistanceFactor(p.DistanceToHospitalKm) * 0.12, 4),
                AppointmentTimeWeight = Math.Round(TimeFactor(apt.AppointmentTime) * 0.10, 4),
                AppointmentDayWeight = Math.Round(DayFactor(apt.AppointmentDate.DayOfWeek) * 0.08, 4),
                InsuranceTypeWeight = Math.Round(InsuranceFactor(p.InsuranceType) * 0.08, 4),
                FirstVisitWeight = Math.Round((apt.IsFirstVisit ? 0.7 : 0.3) * 0.07, 4),
                WeatherCondition = RandomWeather(),
                SmsResponse = RandomSms(),
                IsReminderSent = Rng.Next(2) == 0,
                CalculatedAt = apt.CreatedAt
            };

            analytics.Add(analytic);
        }

        context.NoShowAnalytics.AddRange(analytics);
        await context.SaveChangesAsync();

        // Hasta TotalAppointments/NoShowCount güncellemelerini kaydet
        await context.SaveChangesAsync();

        logger.LogInformation("Appointments seeded: {Total} total, {Analytics} analytics records",
            appointments.Count, analytics.Count);
    }

    // ── Risk hesaplama yardımcıları ───────────────────────────────────────────

    private static double CalculateRisk(Patient p, Appointment apt, double noShowRate)
    {
        double score = noShowRate * 0.35
            + AgeGroupFactor(p.Age) * 0.10
            + DistanceFactor(p.DistanceToHospitalKm) * 0.12
            + TimeFactor(apt.AppointmentTime) * 0.10
            + DayFactor(apt.AppointmentDate.DayOfWeek) * 0.08
            + InsuranceFactor(p.InsuranceType) * 0.08
            + (apt.IsFirstVisit ? 0.7 : 0.3) * 0.07
            + SlotBusyness(apt.SlotOrderInDay, apt.TotalSlotsInDay) * 0.10;
        return Math.Round(score, 4);
    }

    private static double AgeGroupFactor(int age) => age switch
    {
        < 25 => 0.55,
        < 40 => 0.30,
        < 60 => 0.40,
        < 75 => 0.60,
        _ => 0.70
    };

    private static double DistanceFactor(double km) =>
        1.0 / (1.0 + Math.Exp(-0.1 * (km - 15)));

    private static double TimeFactor(TimeOnly t) => t.Hour switch
    {
        < 9 => 0.65,
        < 12 => 0.25,
        < 14 => 0.45,
        < 17 => 0.30,
        _ => 0.70
    };

    private static double DayFactor(DayOfWeek d) => d switch
    {
        DayOfWeek.Monday => 0.35,
        DayOfWeek.Friday => 0.60,
        DayOfWeek.Wednesday => 0.25,
        _ => 0.40
    };

    private static double InsuranceFactor(InsuranceType t) => t switch
    {
        InsuranceType.None => 0.70,
        InsuranceType.GreenCard => 0.65,
        InsuranceType.SelfPay => 0.55,
        InsuranceType.SGK => 0.35,
        InsuranceType.PrivateInsurance => 0.25,
        _ => 0.50
    };

    private static double SlotBusyness(int order, int total) =>
        total > 0 ? (double)order / total : 0.5;

    // ── Yardımcı metotlar ─────────────────────────────────────────────────────

    private static string Normalize(string s) =>
        s.ToLowerInvariant()
         .Replace('ı', 'i').Replace('İ', 'i')
         .Replace('ğ', 'g').Replace('Ğ', 'g')
         .Replace('ş', 's').Replace('Ş', 's')
         .Replace('ç', 'c').Replace('Ç', 'c')
         .Replace('ö', 'o').Replace('Ö', 'o')
         .Replace('ü', 'u').Replace('Ü', 'u');

    private static string GenerateTcNo(int idx)
    {
        // Benzersiz, geçerli formatda 11 haneli TC
        var num = 10000000000L + idx * 97 + Rng.Next(10);
        return num.ToString();
    }

    private static string GeneratePhone()
    {
        int[] prefixes = [532, 542, 505, 551, 536, 545, 555];
        return $"0{prefixes[Rng.Next(prefixes.Length)]}{Rng.Next(1000000, 9999999)}";
    }

    private static WeatherCondition RandomWeather()
    {
        var values = (WeatherCondition[])Enum.GetValues(typeof(WeatherCondition));
        return values[Rng.Next(values.Length)];
    }

    private static SmsResponseType RandomSms()
    {
        // %60 gönderilmedi, %20 onaylandı, %10 reddedildi, %10 görmezden gelindi
        int r = Rng.Next(10);
        return r switch
        {
            < 6 => SmsResponseType.NotSent,
            < 8 => SmsResponseType.Confirmed,
            < 9 => SmsResponseType.Cancelled,
            _ => SmsResponseType.NoResponse
        };
    }

    // ── Toplu Ek Randevu Seeder ───────────────────────────────────────────────
    // Bu metot mevcut doktor ve hastaları kullanarak randevu sayısını 5000+ yapar.
    // İdempotent: Randevu zaten 5000 üzerindeyse çalışmaz.

    public static async Task SeedBulkAppointmentsAsync(
        ApplicationDbContext context,
        ILogger logger,
        int targetCount = 5000)
    {
        var existingCount = await context.Appointments.CountAsync();
        if (existingCount >= targetCount)
        {
            logger.LogInformation("Bulk seed skipped: {Count} appointments already exist (target={Target}).",
                existingCount, targetCount);
            return;
        }

        var doctors  = await context.Doctors.ToListAsync();
        var patients = await context.Patients.Include(p => p.User).ToListAsync();

        if (!doctors.Any() || !patients.Any())
        {
            logger.LogWarning("Bulk seed skipped: no doctors or patients found.");
            return;
        }

        var rnd      = new Random(99);  // sabit seed → tekrarlanabilir
        var today    = DateTime.Today;
        var allSlots = MorningSlots.Concat(AfternoonSlots).ToArray();
        var statuses = new[] { AppointmentStatus.Completed, AppointmentStatus.NoShow,
                               AppointmentStatus.Cancelled, AppointmentStatus.Scheduled };

        // Zaten var olan (doctorId, date, time) üçlülerini yükle
        var usedSlots = await context.Appointments
            .Select(a => new { a.DoctorId, a.AppointmentDate, a.AppointmentTime })
            .ToListAsync();
        var usedSet = usedSlots
            .Select(x => (x.DoctorId, x.AppointmentDate, x.AppointmentTime))
            .ToHashSet();

        logger.LogInformation("Bulk seeding appointments: current={Cur}, target={Target}",
            existingCount, targetCount);

        int needed    = targetCount - existingCount;
        int batchSize = 500;
        int generated = 0;

        while (generated < needed)
        {
            var apts      = new List<Appointment>(batchSize);
            var analytics = new List<NoShowAnalytics>(batchSize);

            for (int i = 0; i < batchSize && generated < needed; i++)
            {
                var patient = patients[rnd.Next(patients.Count)];
                var doctor  = doctors[rnd.Next(doctors.Count)];

                // Rastgele tarih: geçmiş 2 yıl + gelecek 6 ay
                var daysOffset = rnd.Next(-730, 180);
                var aptDate    = today.AddDays(daysOffset);

                // Hafta sonu geçmesin
                while (aptDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    aptDate = aptDate.AddDays(1);

                var slot = allSlots[rnd.Next(allSlots.Length)];

                if (!usedSet.Add((doctor.Id, aptDate, slot))) continue;

                // Geçmiş randevular tamamlansın, gelecek planlanmış olsun
                AppointmentStatus status;
                if (aptDate < today)
                {
                    int r = rnd.Next(10);
                    status = r < 6 ? AppointmentStatus.Completed
                           : r < 9 ? AppointmentStatus.NoShow
                           : AppointmentStatus.Cancelled;
                }
                else
                {
                    status = AppointmentStatus.Scheduled;
                }

                var apt = new Appointment
                {
                    PatientId       = patient.Id,
                    DoctorId        = doctor.Id,
                    AppointmentDate = aptDate,
                    AppointmentTime = slot,
                    Status          = status,
                    IsFirstVisit    = false,
                    SlotOrderInDay  = rnd.Next(1, 10),
                    TotalSlotsInDay = rnd.Next(12, 22),
                    CreatedAt       = aptDate.AddDays(-rnd.Next(1, 14))
                };
                apts.Add(apt);

                // No-show oranını tahmin et (basit)
                double noShowRate = patient.TotalAppointments > 0
                    ? (double)patient.NoShowCount / patient.TotalAppointments
                    : 0.15;

                double risk = CalculateRisk(patient, apt, noShowRate);

                analytics.Add(new NoShowAnalytics
                {
                    RiskScore                  = Math.Clamp(risk, 0.0, 1.0),
                    PreviousNoShowRateWeight   = Math.Round(noShowRate * 0.35, 4),
                    AgeGroupWeight             = Math.Round(AgeGroupFactor(patient.Age) * 0.10, 4),
                    DistanceWeight             = Math.Round(DistanceFactor(patient.DistanceToHospitalKm) * 0.12, 4),
                    AppointmentTimeWeight      = Math.Round(TimeFactor(slot) * 0.10, 4),
                    AppointmentDayWeight       = Math.Round(DayFactor(aptDate.DayOfWeek) * 0.08, 4),
                    InsuranceTypeWeight        = Math.Round(InsuranceFactor(patient.InsuranceType) * 0.08, 4),
                    FirstVisitWeight           = Math.Round(0.3 * 0.07, 4),
                    WeatherCondition           = RandomWeather(),
                    SmsResponse                = RandomSms(),
                    IsReminderSent             = rnd.Next(2) == 0,
                    CalculatedAt               = apt.CreatedAt
                });

                generated++;
            }

            // Önce randevular kaydet (PK lazım)
            context.Appointments.AddRange(apts);
            await context.SaveChangesAsync();

            // Sonra analytics kaydet (FK: AppointmentId)
            for (int j = 0; j < apts.Count; j++)
                analytics[j].AppointmentId = apts[j].Id;

            context.NoShowAnalytics.AddRange(analytics);
            await context.SaveChangesAsync();

            logger.LogInformation("Bulk seed progress: {Generated}/{Needed} new records saved.",
                generated, needed);
        }

        logger.LogInformation("Bulk seed completed. Total appointments: {Total}",
            await context.Appointments.CountAsync());
    }
}
