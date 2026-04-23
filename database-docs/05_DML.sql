-- =============================================================================
-- HASTANE RANDEVU NO-SHOW TAHMİN SİSTEMİ
-- DML Script: Örnek Veri Ekleme (INSERT)
-- =============================================================================
-- ÇALIŞTIRMA SIRASI:
--   1. Önce 04_DDL.sql çalıştırılmış olmalı
--   2. Bu script sırayla: Polyclinics → Users → Patients → Doctors
--      → DoctorSchedules → Appointments → NoShowAnalytics
--      → MedicalHistories → Medications
-- =============================================================================

USE HospitalNoShowDb_Dev;
GO

-- Mevcut veriyi temizle (test ortamı için)
-- DIKKAT: Production'da bu satırları çalıştırma!
DELETE FROM hospital.NoShowAnalytics;
DELETE FROM hospital.Medications;
DELETE FROM hospital.MedicalHistories;
DELETE FROM hospital.Appointments;
DELETE FROM hospital.DoctorSchedules;
DELETE FROM hospital.Doctors;
DELETE FROM hospital.Patients;
-- Users ve Polyclinics son silinir (FK bağımlılığı)
DELETE FROM hospital.Polyclinics;
GO

-- =============================================================================
-- 1. POLİKLİNİKLER
-- =============================================================================
SET IDENTITY_INSERT hospital.Polyclinics ON;

INSERT INTO hospital.Polyclinics (Id, Name, Department, Floor, RoomNumber, PhoneExtension, IsActive)
VALUES
    (1, 'Kardiyoloji Polikliniği',       'İç Hastalıkları',       '3', '301', '3010', 1),
    (2, 'İç Hastalıkları Polikliniği',   'İç Hastalıkları',       '2', '205', '2050', 1),
    (3, 'Ortopedi ve Travmatoloji',       'Cerrahi',               '4', '412', '4120', 1),
    (4, 'Nöroloji Polikliniği',           'Sinir Sistemi',         '3', '318', '3180', 1),
    (5, 'Göz Hastalıkları Polikliniği',  'Göz-KBB',               '2', '210', '2100', 1);

SET IDENTITY_INSERT hospital.Polyclinics OFF;
GO

-- =============================================================================
-- 2. KULLANICILAR (ASP.NET Identity - Şifre hash'ler burada yoktur)
-- Not: Gerçek uygulamada şifreler ASP.NET Identity PasswordHasher ile
--      hash'lenerek saklanır. Bu örnekte hash alanları NULL bırakılmıştır.
--      Gerçek hash değerleri uygulama seeder'ı tarafından oluşturulur.
-- =============================================================================
-- Kullanıcı ID'leri sabit GUID formatında tanımlanıyor
DECLARE @AdminId   NVARCHAR(450) = 'A0000000-0000-0000-0000-000000000001';
DECLARE @Doctor1Id NVARCHAR(450) = 'D0000000-0000-0000-0000-000000000001';
DECLARE @Doctor2Id NVARCHAR(450) = 'D0000000-0000-0000-0000-000000000002';
DECLARE @Patient1Id NVARCHAR(450) = 'P0000000-0000-0000-0000-000000000001';
DECLARE @Patient2Id NVARCHAR(450) = 'P0000000-0000-0000-0000-000000000002';
DECLARE @Patient3Id NVARCHAR(450) = 'P0000000-0000-0000-0000-000000000003';
DECLARE @Patient4Id NVARCHAR(450) = 'P0000000-0000-0000-0000-000000000004';
DECLARE @Patient5Id NVARCHAR(450) = 'P0000000-0000-0000-0000-000000000005';

INSERT INTO hospital.Users
    (Id, FirstName, LastName, CreatedAt, IsActive,
     UserName, NormalizedUserName, Email, NormalizedEmail,
     EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
     TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
VALUES
    (@AdminId,    'Admin',   'Kullanıcı', GETUTCDATE(), 1,
     'admin@hospital.com', 'ADMIN@HOSPITAL.COM', 'admin@hospital.com', 'ADMIN@HOSPITAL.COM',
     1, '<<HASH:Admin@12345>>', NEWID(), NEWID(), 0, 0, 0),

    (@Doctor1Id,  'Ahmet',   'Yılmaz',   GETUTCDATE(), 1,
     'dr.ahmet.yilmaz@hospital.com', 'DR.AHMET.YILMAZ@HOSPITAL.COM',
     'dr.ahmet.yilmaz@hospital.com', 'DR.AHMET.YILMAZ@HOSPITAL.COM',
     1, '<<HASH:Doctor@12345>>', NEWID(), NEWID(), 0, 0, 0),

    (@Doctor2Id,  'Fatma',   'Kaya',     GETUTCDATE(), 1,
     'dr.fatma.kaya@hospital.com', 'DR.FATMA.KAYA@HOSPITAL.COM',
     'dr.fatma.kaya@hospital.com', 'DR.FATMA.KAYA@HOSPITAL.COM',
     1, '<<HASH:Doctor@12345>>', NEWID(), NEWID(), 0, 0, 0),

    (@Patient1Id, 'Mehmet',  'Demir',    GETUTCDATE(), 1,
     'hasta001@test.com', 'HASTA001@TEST.COM', 'hasta001@test.com', 'HASTA001@TEST.COM',
     1, '<<HASH:Hasta@12345>>', NEWID(), NEWID(), 0, 0, 0),

    (@Patient2Id, 'Ayşe',    'Çelik',    GETUTCDATE(), 1,
     'hasta002@test.com', 'HASTA002@TEST.COM', 'hasta002@test.com', 'HASTA002@TEST.COM',
     1, '<<HASH:Hasta@12345>>', NEWID(), NEWID(), 0, 0, 0),

    (@Patient3Id, 'Ali',     'Şahin',    GETUTCDATE(), 1,
     'hasta003@test.com', 'HASTA003@TEST.COM', 'hasta003@test.com', 'HASTA003@TEST.COM',
     1, '<<HASH:Hasta@12345>>', NEWID(), NEWID(), 0, 0, 0),

    (@Patient4Id, 'Zeynep',  'Arslan',   GETUTCDATE(), 1,
     'hasta004@test.com', 'HASTA004@TEST.COM', 'hasta004@test.com', 'HASTA004@TEST.COM',
     1, '<<HASH:Hasta@12345>>', NEWID(), NEWID(), 0, 0, 0),

    (@Patient5Id, 'Mustafa', 'Koç',      GETUTCDATE(), 1,
     'hasta005@test.com', 'HASTA005@TEST.COM', 'hasta005@test.com', 'HASTA005@TEST.COM',
     1, '<<HASH:Hasta@12345>>', NEWID(), NEWID(), 0, 0, 0);
GO

-- =============================================================================
-- 3. HASTALAR
-- Not: NoShowRate = NoShowCount / TotalAppointments (hesaplanmış, sütun yok)
-- =============================================================================
SET IDENTITY_INSERT hospital.Patients ON;

INSERT INTO hospital.Patients
    (Id, UserId, IdentityNumber, DateOfBirth, Gender, PhoneNumber,
     Address, City, DistanceToHospitalKm, InsuranceType, InsurancePolicyNumber,
     HasChronicDisease, ChronicDiseaseNotes, TotalAppointments, NoShowCount, CreatedAt)
VALUES
    -- Düşük riskli hasta (SGK, yakın mesafe, orta yaş)
    (1, 'P0000000-0000-0000-0000-000000000001', '12345678901', '1978-03-15',
     'Male', '05321234567',
     'Kadıköy Mah. Bağdat Cad. No:45', 'İstanbul', 4.5,
     'SGK', 'SGK-2024-001', 0, NULL, 12, 1, GETUTCDATE()),

    -- Orta riskli hasta (kronik hastalık, uzak mesafe)
    (2, 'P0000000-0000-0000-0000-000000000002', '23456789012', '1992-07-22',
     'Female', '05427654321',
     'Çankaya Mah. Atatürk Bulvarı No:88', 'Ankara', 18.3,
     'PrivateInsurance', 'ÖZEL-2024-456', 1, 'Tip 2 Diyabet', 8, 2, GETUTCDATE()),

    -- Yüksek riskli hasta (genç, çok uzak, geçmişte no-show)
    (3, 'P0000000-0000-0000-0000-000000000003', '34567890123', '1998-11-05',
     'Male', '05053332211',
     'Bornova Mah. İzmir Yolu No:12', 'İzmir', 35.7,
     'None', NULL, 0, NULL, 10, 5, GETUTCDATE()),

    -- Kritik riskli hasta (yaşlı, yüksek no-show oranı)
    (4, 'P0000000-0000-0000-0000-000000000004', '45678901234', '1955-01-18',
     'Female', '05321119988',
     'Selçuklu Mah. Konya Cad. No:3', 'Konya', 42.1,
     'GreenCard', NULL, 1, 'Hipertansiyon, KOAH', 15, 10, GETUTCDATE()),

    -- Yeni hasta (hiç randevusu olmamış, ilk ziyaret)
    (5, 'P0000000-0000-0000-0000-000000000005', '56789012345', '1985-09-30',
     'Male', '05428889977',
     'Nilüfer Mah. Osmangazi Sok. No:7', 'Bursa', 8.0,
     'SGK', 'SGK-2024-789', 0, NULL, 0, 0, GETUTCDATE());

SET IDENTITY_INSERT hospital.Patients OFF;
GO

-- =============================================================================
-- 4. DOKTORLAR
-- =============================================================================
SET IDENTITY_INSERT hospital.Doctors ON;

INSERT INTO hospital.Doctors
    (Id, UserId, Specialty, Title, DiplomaCertificateNumber, PolyclinicId,
     MaxDailyPatients, AverageAppointmentDurationMinutes, CreatedAt)
VALUES
    (1, 'D0000000-0000-0000-0000-000000000001',
     'Kardiyoloji', 'Prof. Dr.', 'KARD-2005-0042', 1, 20, 20, GETUTCDATE()),

    (2, 'D0000000-0000-0000-0000-000000000002',
     'İç Hastalıkları', 'Doç. Dr.', 'IHAS-2010-0117', 2, 25, 15, GETUTCDATE());

SET IDENTITY_INSERT hospital.Doctors OFF;
GO

-- =============================================================================
-- 5. DOKTOR ÇALIŞMA SAATLERİ
-- =============================================================================
SET IDENTITY_INSERT hospital.DoctorSchedules ON;

INSERT INTO hospital.DoctorSchedules (Id, DoctorId, DayOfWeek, StartTime, EndTime, IsAvailable)
VALUES
    -- Dr. Ahmet Yılmaz (Kardiyoloji) - Haftaiçi sabah
    (1,  1, 'Monday',    '08:00', '12:00', 1),
    (2,  1, 'Tuesday',   '08:00', '12:00', 1),
    (3,  1, 'Wednesday', '13:00', '17:00', 1),
    (4,  1, 'Thursday',  '08:00', '12:00', 1),
    (5,  1, 'Friday',    '08:00', '11:00', 1),

    -- Dr. Fatma Kaya (İç Hastalıkları) - Öğleden sonra
    (6,  2, 'Monday',    '13:00', '17:00', 1),
    (7,  2, 'Tuesday',   '13:00', '17:00', 1),
    (8,  2, 'Wednesday', '08:00', '12:00', 1),
    (9,  2, 'Thursday',  '13:00', '17:00', 1),
    (10, 2, 'Friday',    '09:00', '13:00', 1);

SET IDENTITY_INSERT hospital.DoctorSchedules OFF;
GO

-- =============================================================================
-- 6. RANDEVULAR
-- =============================================================================
SET IDENTITY_INSERT hospital.Appointments ON;

INSERT INTO hospital.Appointments
    (Id, PatientId, DoctorId, AppointmentDate, AppointmentTime,
     Status, CancellationReason, Notes, IsFirstVisit, SlotOrderInDay, TotalSlotsInDay, CreatedAt)
VALUES
    -- Geçmiş: Tamamlandı
    (1,  1, 1, '2026-01-10', '09:00', 'Completed', NULL, 'Kontrol randevusu', 0, 3, 20, GETUTCDATE()),
    (2,  2, 2, '2026-01-15', '14:00', 'Completed', NULL, NULL, 0, 7, 25, GETUTCDATE()),
    -- Geçmiş: No-Show
    (3,  3, 1, '2026-01-20', '10:00', 'NoShow',    NULL, NULL, 0, 5, 20, GETUTCDATE()),
    (4,  4, 2, '2026-01-25', '15:30', 'NoShow',    NULL, NULL, 0, 12, 25, GETUTCDATE()),
    -- Geçmiş: İptal
    (5,  3, 1, '2026-02-05', '08:30', 'Cancelled', 'Hasta seyahatte', NULL, 0, 2, 20, GETUTCDATE()),
    (6,  4, 2, '2026-02-10', '13:00', 'Completed', NULL, 'Tansiyon takibi', 0, 4, 25, GETUTCDATE()),
    -- Geçmiş: Tamamlandı
    (7,  1, 2, '2026-02-20', '14:30', 'Completed', NULL, NULL, 0, 8, 25, GETUTCDATE()),
    (8,  2, 1, '2026-02-28', '09:30', 'Completed', NULL, NULL, 0, 4, 20, GETUTCDATE()),
    -- Gelecek: Planlı
    (9,  5, 1, '2026-03-15', '09:00', 'Scheduled', NULL, 'İlk muayene', 1, 3, 20, GETUTCDATE()),
    (10, 1, 2, '2026-03-20', '14:00', 'Scheduled', NULL, NULL, 0, 6, 25, GETUTCDATE());

SET IDENTITY_INSERT hospital.Appointments OFF;
GO

-- =============================================================================
-- 7. NO-SHOW ANALİTİK VERİSİ
-- (Her randevu için bir analytics kaydı)
-- =============================================================================
SET IDENTITY_INSERT hospital.NoShowAnalytics ON;

INSERT INTO hospital.NoShowAnalytics
    (Id, AppointmentId, RiskScore, PreviousNoShowRateWeight, AgeGroupWeight, DistanceWeight,
     AppointmentTimeWeight, AppointmentDayWeight, WeatherWeight, SmsResponseWeight,
     InsuranceTypeWeight, FirstVisitWeight, WeatherCondition, SmsResponse,
     SmsSentAt, SmsRespondedAt, IsReminderSent, ReminderSentAt, CalculatedAt)
VALUES
    -- Appt 1: Düşük risk (tamamlandı)
    (1,  1, 0.1823, 0.0233, 0.0200, 0.0540, 0.0600, 0.0360, 0.0000, 0.0000, 0.0120, 0.0000,
     'Clear', 'Confirmed', DATEADD(day,-7,GETUTCDATE()), DATEADD(day,-6,GETUTCDATE()),
     1, DATEADD(day,-7,GETUTCDATE()), GETUTCDATE()),

    -- Appt 2: Orta risk
    (2,  2, 0.3542, 0.1050, 0.0250, 0.0876, 0.0600, 0.0400, 0.0000, 0.0000, 0.0120, 0.0000,
     'Cloudy', 'Sent', DATEADD(day,-10,GETUTCDATE()), NULL,
     1, DATEADD(day,-10,GETUTCDATE()), GETUTCDATE()),

    -- Appt 3: Yüksek risk (no-show oldu)
    (3,  3, 0.7134, 0.1750, 0.0500, 0.1428, 0.0600, 0.0400, 0.0000, 0.1000, 0.0240, 0.0350,
     'Rainy', 'NoResponse', DATEADD(day,-14,GETUTCDATE()), NULL,
     1, DATEADD(day,-14,GETUTCDATE()), GETUTCDATE()),

    -- Appt 4: Kritik risk (no-show oldu)
    (4,  4, 0.8901, 0.2450, 0.0550, 0.1680, 0.1000, 0.0400, 0.0000, 0.1000, 0.0400, 0.0000,
     'Stormy', 'Cancelled', DATEADD(day,-20,GETUTCDATE()), DATEADD(day,-19,GETUTCDATE()),
     1, DATEADD(day,-20,GETUTCDATE()), GETUTCDATE()),

    -- Appt 5: Orta-yüksek risk (iptal)
    (5,  5, 0.5890, 0.1750, 0.0500, 0.1428, 0.0300, 0.0360, 0.0000, 0.0000, 0.0240, 0.0350,
     'Clear', 'NotSent', NULL, NULL,
     0, NULL, GETUTCDATE()),

    -- Appt 6: Orta risk (tamamlandı)
    (6,  6, 0.4201, 0.1750, 0.0550, 0.1680, 0.0600, 0.0360, 0.0000, -0.2500, 0.0400, 0.0000,
     'Clear', 'Confirmed', DATEADD(day,-15,GETUTCDATE()), DATEADD(day,-14,GETUTCDATE()),
     1, DATEADD(day,-15,GETUTCDATE()), GETUTCDATE()),

    -- Appt 7: Düşük risk (tamamlandı)
    (7,  7, 0.2150, 0.0233, 0.0200, 0.0540, 0.0600, 0.0400, 0.0000, -0.2500, 0.0120, 0.0000,
     'Clear', 'Confirmed', DATEADD(day,-25,GETUTCDATE()), DATEADD(day,-24,GETUTCDATE()),
     1, DATEADD(day,-25,GETUTCDATE()), GETUTCDATE()),

    -- Appt 8: Düşük risk (tamamlandı)
    (8,  8, 0.2890, 0.1050, 0.0250, 0.0876, 0.0300, 0.0360, 0.0000, -0.2500, 0.0120, 0.0000,
     'Cloudy', 'Confirmed', DATEADD(day,-30,GETUTCDATE()), DATEADD(day,-29,GETUTCDATE()),
     1, DATEADD(day,-30,GETUTCDATE()), GETUTCDATE()),

    -- Appt 9: Orta risk (planlı - ilk ziyaret)
    (9,  9, 0.4100, 0.0000, 0.0200, 0.0720, 0.0600, 0.0400, 0.0000, 0.0000, 0.0120, 0.0700,
     'Unknown', 'NotSent', NULL, NULL,
     0, NULL, GETUTCDATE()),

    -- Appt 10: Düşük risk (planlı)
    (10, 10, 0.1950, 0.0233, 0.0200, 0.0540, 0.0600, 0.0400, 0.0000, 0.0000, 0.0120, 0.0000,
     'Unknown', 'NotSent', NULL, NULL,
     0, NULL, GETUTCDATE());

SET IDENTITY_INSERT hospital.NoShowAnalytics OFF;
GO

-- =============================================================================
-- 8. TIBBİ GEÇMİŞ
-- =============================================================================
SET IDENTITY_INSERT hospital.MedicalHistories ON;

INSERT INTO hospital.MedicalHistories
    (Id, PatientId, DiagnosisCode, DiagnosisName, DiagnosisDescription,
     DiagnosisDate, IsActive, CreatedAt)
VALUES
    (1, 2, 'E11',   'Tip 2 Diabetes Mellitus',
     'HbA1c %7.2, metformin ile kontrol altında',
     '2020-03-10', 1, GETUTCDATE()),

    (2, 2, 'I10',   'Esansiyel (Primer) Hipertansiyon',
     'Sistolik 145/diastolik 92, antihipertansif tedavi başlandı',
     '2021-06-15', 1, GETUTCDATE()),

    (3, 4, 'I10',   'Esansiyel (Primer) Hipertansiyon',
     'Uzun süreli hipertansiyon, çoklu ilaç kullanımı',
     '2015-02-20', 1, GETUTCDATE()),

    (4, 4, 'J44.1', 'Akut Alevlenme ile KOAH',
     'Orta şiddetli KOAH, yıllık 2-3 alevlenme',
     '2018-09-05', 1, GETUTCDATE()),

    (5, 1, 'Z82.49','Ailede Kardiyovasküler Hastalık Öyküsü',
     'Baba ve amca koroner arter hastalığı',
     '2019-11-12', 1, GETUTCDATE());

SET IDENTITY_INSERT hospital.MedicalHistories OFF;
GO

-- =============================================================================
-- 9. İLAÇLAR
-- =============================================================================
SET IDENTITY_INSERT hospital.Medications ON;

INSERT INTO hospital.Medications
    (Id, MedicalHistoryId, Name, Dosage, Frequency, StartDate, EndDate, Notes)
VALUES
    -- Diyabet ilacı
    (1, 1, 'Metformin HCl', '1000 mg', 'Günde 2 kez (sabah-akşam yemekle)', '2020-03-15', NULL,
     'Böbrek fonksiyonları 3 ayda bir takip edilecek'),

    -- Hipertansiyon ilacı (hasta 2)
    (2, 2, 'Amlodipine', '5 mg', 'Günde 1 kez (sabah)', '2021-06-20', NULL,
     'Ayak bileği ödemi açısından takip'),

    -- Hipertansiyon ilacı (hasta 4)
    (3, 3, 'Enalapril', '10 mg', 'Günde 2 kez', '2015-03-01', NULL,
     'Kuru öksürük yan etkisi izleniyor'),

    -- KOAH ilacı
    (4, 4, 'Tiotropium (Spiriva)', '18 mcg', 'Günde 1 kez inhaler', '2018-09-10', NULL,
     'Spirometri 6 ayda bir'),

    -- Kardiyovasküler koruma (hasta 1)
    (5, 5, 'Aspirin', '100 mg', 'Günde 1 kez (akşam yemekten sonra)', '2019-11-15', NULL,
     'Mide koruyucu ile birlikte alınacak');

SET IDENTITY_INSERT hospital.Medications OFF;
GO

-- =============================================================================
-- DOĞRULAMA SORGULARI
-- =============================================================================
SELECT 'Polyclinics'       AS Tablo, COUNT(*) AS Adet FROM hospital.Polyclinics UNION ALL
SELECT 'Users',              COUNT(*) FROM hospital.Users UNION ALL
SELECT 'Patients',           COUNT(*) FROM hospital.Patients UNION ALL
SELECT 'Doctors',            COUNT(*) FROM hospital.Doctors UNION ALL
SELECT 'DoctorSchedules',    COUNT(*) FROM hospital.DoctorSchedules UNION ALL
SELECT 'Appointments',       COUNT(*) FROM hospital.Appointments UNION ALL
SELECT 'NoShowAnalytics',    COUNT(*) FROM hospital.NoShowAnalytics UNION ALL
SELECT 'MedicalHistories',   COUNT(*) FROM hospital.MedicalHistories UNION ALL
SELECT 'Medications',        COUNT(*) FROM hospital.Medications;
GO
-- Beklenen çıktı:
-- Polyclinics: 5 | Users: 8 | Patients: 5 | Doctors: 2
-- DoctorSchedules: 10 | Appointments: 10 | NoShowAnalytics: 10
-- MedicalHistories: 5 | Medications: 5
