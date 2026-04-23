-- =============================================================================
-- HASTANE RANDEVU NO-SHOW TAHMİN SİSTEMİ
-- DDL Script: Tablo Oluşturma, Kısıtlar ve İndeksler
-- Veritabanı: SQL Server (LocalDB / SQL Server 2019+)
-- Schema: hospital
-- Hazırlayan: Hastane No-Show Projesi
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 0. VERİTABANI OLUŞTURMA (opsiyonel, zaten varsa atla)
-- -----------------------------------------------------------------------------
-- CREATE DATABASE HospitalNoShowDb_Dev;
-- GO
-- USE HospitalNoShowDb_Dev;
-- GO

-- -----------------------------------------------------------------------------
-- 1. SCHEMA OLUŞTURMA
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'hospital')
BEGIN
    EXEC('CREATE SCHEMA hospital');
END
GO

-- =============================================================================
-- NOT: ASP.NET Identity Tabloları (AspNetUsers, AspNetRoles, AspNetUserRoles,
-- AspNetUserClaims, AspNetUserLogins, AspNetUserTokens, AspNetRoleClaims)
-- EF Core Migration tarafından otomatik olarak hospital şeması altında
-- oluşturulur. Bu script yalnızca uygulama tablolarını kapsamaktadır.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 2. POLİKLİNİKLER TABLOSU
-- -----------------------------------------------------------------------------
IF OBJECT_ID('hospital.Polyclinics', 'U') IS NOT NULL DROP TABLE hospital.Polyclinics;

CREATE TABLE hospital.Polyclinics (
    Id              INT             NOT NULL IDENTITY(1,1),
    Name            NVARCHAR(150)   NOT NULL,
    Department      NVARCHAR(100)   NOT NULL,
    Floor           NVARCHAR(20)    NOT NULL,
    RoomNumber      NVARCHAR(20)    NOT NULL,
    PhoneExtension  NVARCHAR(10)    NOT NULL,
    IsActive        BIT             NOT NULL CONSTRAINT DF_Polyclinics_IsActive DEFAULT (1),

    CONSTRAINT PK_Polyclinics PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT CK_Polyclinics_Name CHECK (LEN(LTRIM(RTRIM(Name))) > 0),
    CONSTRAINT CK_Polyclinics_PhoneExtension CHECK (PhoneExtension NOT LIKE '%[^0-9]%')
);
GO

-- -----------------------------------------------------------------------------
-- 3. KULLANICILAR TABLOSU (ASP.NET Identity - ApplicationUser)
-- -----------------------------------------------------------------------------
-- Not: Bu tablo EF Core Identity migration'ı tarafından oluşturulur.
-- Burada manuel DDL referans amaçlı verilmiştir.

IF OBJECT_ID('hospital.Users', 'U') IS NOT NULL DROP TABLE hospital.Users;

CREATE TABLE hospital.Users (
    Id                   NVARCHAR(450)    NOT NULL,
    FirstName            NVARCHAR(MAX)    NOT NULL,
    LastName             NVARCHAR(MAX)    NOT NULL,
    CreatedAt            DATETIME2        NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (GETUTCDATE()),
    LastLoginAt          DATETIME2        NULL,
    IsActive             BIT              NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    -- ASP.NET Identity alanları:
    UserName             NVARCHAR(256)    NULL,
    NormalizedUserName   NVARCHAR(256)    NULL,
    Email                NVARCHAR(256)    NULL,
    NormalizedEmail      NVARCHAR(256)    NULL,
    EmailConfirmed       BIT              NOT NULL DEFAULT (0),
    PasswordHash         NVARCHAR(MAX)    NULL,
    SecurityStamp        NVARCHAR(MAX)    NULL,
    ConcurrencyStamp     NVARCHAR(MAX)    NULL,
    PhoneNumber          NVARCHAR(MAX)    NULL,
    PhoneNumberConfirmed BIT              NOT NULL DEFAULT (0),
    TwoFactorEnabled     BIT              NOT NULL DEFAULT (0),
    LockoutEnd           DATETIMEOFFSET   NULL,
    LockoutEnabled       BIT              NOT NULL DEFAULT (0),
    AccessFailedCount    INT              NOT NULL DEFAULT (0),

    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT CK_Users_Email CHECK (Email LIKE '%@%.%')
);

CREATE UNIQUE INDEX UserNameIndex
    ON hospital.Users (NormalizedUserName)
    WHERE NormalizedUserName IS NOT NULL;

CREATE INDEX EmailIndex
    ON hospital.Users (NormalizedEmail);
GO

-- -----------------------------------------------------------------------------
-- 4. HASTALAR TABLOSU
-- -----------------------------------------------------------------------------
IF OBJECT_ID('hospital.Patients', 'U') IS NOT NULL DROP TABLE hospital.Patients;

CREATE TABLE hospital.Patients (
    Id                      INT             NOT NULL IDENTITY(1,1),
    UserId                  NVARCHAR(450)   NOT NULL,
    IdentityNumber          NCHAR(11)       NOT NULL,
    DateOfBirth             DATE            NOT NULL,
    Gender                  NVARCHAR(20)    NOT NULL,
    PhoneNumber             NVARCHAR(20)    NOT NULL,
    Address                 NVARCHAR(500)   NOT NULL,
    City                    NVARCHAR(100)   NOT NULL,
    DistanceToHospitalKm    DECIMAL(10,2)   NOT NULL CONSTRAINT DF_Patients_Distance DEFAULT (0),
    InsuranceType           NVARCHAR(30)    NOT NULL CONSTRAINT DF_Patients_Insurance DEFAULT ('None'),
    InsurancePolicyNumber   NVARCHAR(50)    NULL,
    HasChronicDisease       BIT             NOT NULL CONSTRAINT DF_Patients_ChronicDisease DEFAULT (0),
    ChronicDiseaseNotes     NVARCHAR(1000)  NULL,
    TotalAppointments       INT             NOT NULL CONSTRAINT DF_Patients_TotalAppt DEFAULT (0),
    NoShowCount             INT             NOT NULL CONSTRAINT DF_Patients_NoShow DEFAULT (0),
    CreatedAt               DATETIME2       NOT NULL CONSTRAINT DF_Patients_CreatedAt DEFAULT (GETUTCDATE()),
    UpdatedAt               DATETIME2       NULL,

    CONSTRAINT PK_Patients PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_Patients_Users
        FOREIGN KEY (UserId) REFERENCES hospital.Users(Id)
        ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT UQ_Patients_UserId
        UNIQUE (UserId),
    CONSTRAINT UQ_Patients_IdentityNumber
        UNIQUE (IdentityNumber),
    CONSTRAINT CK_Patients_Gender
        CHECK (Gender IN ('Male', 'Female', 'Other', 'PreferNotToSay')),
    CONSTRAINT CK_Patients_InsuranceType
        CHECK (InsuranceType IN ('None', 'SGK', 'PrivateInsurance', 'GreenCard', 'SelfPay')),
    CONSTRAINT CK_Patients_Distance
        CHECK (DistanceToHospitalKm >= 0 AND DistanceToHospitalKm <= 500),
    CONSTRAINT CK_Patients_IdentityNumber
        CHECK (IdentityNumber NOT LIKE '%[^0-9]%' AND LEN(IdentityNumber) = 11),
    CONSTRAINT CK_Patients_NoShowCount
        CHECK (NoShowCount >= 0 AND NoShowCount <= TotalAppointments),
    CONSTRAINT CK_Patients_TotalAppt
        CHECK (TotalAppointments >= 0)
);

CREATE INDEX IX_Patients_UserId
    ON hospital.Patients (UserId);

CREATE INDEX IX_Patients_City
    ON hospital.Patients (City);
GO

-- -----------------------------------------------------------------------------
-- 5. DOKTORLAR TABLOSU
-- -----------------------------------------------------------------------------
IF OBJECT_ID('hospital.Doctors', 'U') IS NOT NULL DROP TABLE hospital.Doctors;

CREATE TABLE hospital.Doctors (
    Id                                  INT             NOT NULL IDENTITY(1,1),
    UserId                              NVARCHAR(450)   NOT NULL,
    Specialty                           NVARCHAR(100)   NOT NULL,
    Title                               NVARCHAR(50)    NOT NULL,
    DiplomaCertificateNumber            NVARCHAR(50)    NOT NULL,
    PolyclinicId                        INT             NOT NULL,
    MaxDailyPatients                    INT             NOT NULL CONSTRAINT DF_Doctors_MaxPatients DEFAULT (20),
    AverageAppointmentDurationMinutes   INT             NOT NULL CONSTRAINT DF_Doctors_AvgDuration DEFAULT (15),
    CreatedAt                           DATETIME2       NOT NULL CONSTRAINT DF_Doctors_CreatedAt DEFAULT (GETUTCDATE()),
    UpdatedAt                           DATETIME2       NULL,

    CONSTRAINT PK_Doctors PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_Doctors_Users
        FOREIGN KEY (UserId) REFERENCES hospital.Users(Id)
        ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT FK_Doctors_Polyclinics
        FOREIGN KEY (PolyclinicId) REFERENCES hospital.Polyclinics(Id)
        ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT UQ_Doctors_UserId
        UNIQUE (UserId),
    CONSTRAINT UQ_Doctors_DiplomaCertificateNumber
        UNIQUE (DiplomaCertificateNumber),
    CONSTRAINT CK_Doctors_MaxDailyPatients
        CHECK (MaxDailyPatients BETWEEN 1 AND 100),
    CONSTRAINT CK_Doctors_AvgDuration
        CHECK (AverageAppointmentDurationMinutes BETWEEN 5 AND 120),
    CONSTRAINT CK_Doctors_Title
        CHECK (Title IN ('Prof. Dr.', 'Doç. Dr.', 'Dr.', 'Uzm. Dr.', 'Op. Dr.'))
);

CREATE INDEX IX_Doctors_UserId
    ON hospital.Doctors (UserId);

CREATE INDEX IX_Doctors_PolyclinicId
    ON hospital.Doctors (PolyclinicId);

CREATE INDEX IX_Doctors_Specialty
    ON hospital.Doctors (Specialty);
GO

-- -----------------------------------------------------------------------------
-- 6. DOKTOR ÇALIŞMA SAATLERİ TABLOSU
-- -----------------------------------------------------------------------------
IF OBJECT_ID('hospital.DoctorSchedules', 'U') IS NOT NULL DROP TABLE hospital.DoctorSchedules;

CREATE TABLE hospital.DoctorSchedules (
    Id          INT             NOT NULL IDENTITY(1,1),
    DoctorId    INT             NOT NULL,
    DayOfWeek   NVARCHAR(15)    NOT NULL,
    StartTime   TIME            NOT NULL,
    EndTime     TIME            NOT NULL,
    IsAvailable BIT             NOT NULL CONSTRAINT DF_DoctorSchedules_IsAvail DEFAULT (1),

    CONSTRAINT PK_DoctorSchedules PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_DoctorSchedules_Doctors
        FOREIGN KEY (DoctorId) REFERENCES hospital.Doctors(Id)
        ON DELETE CASCADE ON UPDATE NO ACTION,
    CONSTRAINT UQ_DoctorSchedule_Doctor_Day
        UNIQUE (DoctorId, DayOfWeek),
    CONSTRAINT CK_DoctorSchedules_DayOfWeek
        CHECK (DayOfWeek IN ('Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday')),
    CONSTRAINT CK_DoctorSchedules_TimeRange
        CHECK (EndTime > StartTime)
);
GO

-- -----------------------------------------------------------------------------
-- 7. RANDEVULAR TABLOSU
-- -----------------------------------------------------------------------------
IF OBJECT_ID('hospital.Appointments', 'U') IS NOT NULL DROP TABLE hospital.Appointments;

CREATE TABLE hospital.Appointments (
    Id                  INT             NOT NULL IDENTITY(1,1),
    PatientId           INT             NOT NULL,
    DoctorId            INT             NOT NULL,
    AppointmentDate     DATETIME2       NOT NULL,
    AppointmentTime     TIME            NOT NULL,
    CreatedAt           DATETIME2       NOT NULL CONSTRAINT DF_Appointments_CreatedAt DEFAULT (GETUTCDATE()),
    UpdatedAt           DATETIME2       NULL,
    Status              NVARCHAR(30)    NOT NULL CONSTRAINT DF_Appointments_Status DEFAULT ('Scheduled'),
    CancellationReason  NVARCHAR(500)   NULL,
    Notes               NVARCHAR(1000)  NULL,
    IsFirstVisit        BIT             NOT NULL CONSTRAINT DF_Appointments_FirstVisit DEFAULT (0),
    SlotOrderInDay      INT             NOT NULL CONSTRAINT DF_Appointments_SlotOrder DEFAULT (1),
    TotalSlotsInDay     INT             NOT NULL CONSTRAINT DF_Appointments_TotalSlots DEFAULT (1),

    CONSTRAINT PK_Appointments PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_Appointments_Patients
        FOREIGN KEY (PatientId) REFERENCES hospital.Patients(Id)
        ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT FK_Appointments_Doctors
        FOREIGN KEY (DoctorId) REFERENCES hospital.Doctors(Id)
        ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT CK_Appointments_Status
        CHECK (Status IN ('Scheduled','Completed','NoShow','Cancelled','Rescheduled')),
    CONSTRAINT CK_Appointments_SlotOrder
        CHECK (SlotOrderInDay >= 1 AND SlotOrderInDay <= TotalSlotsInDay),
    CONSTRAINT CK_Appointments_TotalSlots
        CHECK (TotalSlotsInDay >= 1),
    CONSTRAINT CK_Appointments_CancelReason
        CHECK (Status <> 'Cancelled' OR CancellationReason IS NOT NULL)
);

-- Birincil performans indeksi (arama)
CREATE INDEX IX_Appointments_PatientId
    ON hospital.Appointments (PatientId);

-- Benzersiz: Aynı doktor, aynı gün/saat için yalnızca bir randevu
CREATE UNIQUE INDEX IX_Appointments_Doctor_DateTime
    ON hospital.Appointments (DoctorId, AppointmentDate, AppointmentTime);

-- Tarih bazlı sorgular için
CREATE INDEX IX_Appointments_Date
    ON hospital.Appointments (AppointmentDate);

-- Status filtresi için
CREATE INDEX IX_Appointments_Status
    ON hospital.Appointments (Status);
GO

-- -----------------------------------------------------------------------------
-- 8. TIBBİ GEÇMİŞ TABLOSU
-- -----------------------------------------------------------------------------
IF OBJECT_ID('hospital.MedicalHistories', 'U') IS NOT NULL DROP TABLE hospital.MedicalHistories;

CREATE TABLE hospital.MedicalHistories (
    Id                      INT             NOT NULL IDENTITY(1,1),
    PatientId               INT             NOT NULL,
    DiagnosisCode           NVARCHAR(10)    NOT NULL,   -- ICD-10 kodu
    DiagnosisName           NVARCHAR(200)   NOT NULL,
    DiagnosisDescription    NVARCHAR(2000)  NULL,
    DiagnosisDate           DATE            NOT NULL,
    IsActive                BIT             NOT NULL CONSTRAINT DF_MedHist_IsActive DEFAULT (1),
    CreatedAt               DATETIME2       NOT NULL CONSTRAINT DF_MedHist_CreatedAt DEFAULT (GETUTCDATE()),
    UpdatedAt               DATETIME2       NULL,

    CONSTRAINT PK_MedicalHistories PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_MedicalHistories_Patients
        FOREIGN KEY (PatientId) REFERENCES hospital.Patients(Id)
        ON DELETE CASCADE ON UPDATE NO ACTION,
    CONSTRAINT CK_MedicalHistories_Code
        CHECK (LEN(LTRIM(RTRIM(DiagnosisCode))) >= 3)
);

CREATE INDEX IX_MedicalHistory_Patient_Diagnosis
    ON hospital.MedicalHistories (PatientId, DiagnosisCode);
GO

-- -----------------------------------------------------------------------------
-- 9. İLAÇLAR TABLOSU
-- -----------------------------------------------------------------------------
IF OBJECT_ID('hospital.Medications', 'U') IS NOT NULL DROP TABLE hospital.Medications;

CREATE TABLE hospital.Medications (
    Id                  INT             NOT NULL IDENTITY(1,1),
    MedicalHistoryId    INT             NOT NULL,
    Name                NVARCHAR(200)   NOT NULL,
    Dosage              NVARCHAR(100)   NOT NULL,
    Frequency           NVARCHAR(100)   NOT NULL,
    StartDate           DATE            NOT NULL,
    EndDate             DATE            NULL,
    Notes               NVARCHAR(500)   NULL,

    CONSTRAINT PK_Medications PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_Medications_MedicalHistories
        FOREIGN KEY (MedicalHistoryId) REFERENCES hospital.MedicalHistories(Id)
        ON DELETE CASCADE ON UPDATE NO ACTION,
    CONSTRAINT CK_Medications_DateRange
        CHECK (EndDate IS NULL OR EndDate >= StartDate)
);

CREATE INDEX IX_Medications_MedicalHistoryId
    ON hospital.Medications (MedicalHistoryId);
GO

-- -----------------------------------------------------------------------------
-- 10. NO-SHOW ANALİTİK TABLOSU
-- -----------------------------------------------------------------------------
IF OBJECT_ID('hospital.NoShowAnalytics', 'U') IS NOT NULL DROP TABLE hospital.NoShowAnalytics;

CREATE TABLE hospital.NoShowAnalytics (
    Id                          INT             NOT NULL IDENTITY(1,1),
    AppointmentId               INT             NOT NULL,
    RiskScore                   DECIMAL(5,4)    NOT NULL CONSTRAINT DF_NSA_RiskScore DEFAULT (0.0),
    PreviousNoShowRateWeight    DECIMAL(5,4)    NOT NULL CONSTRAINT DF_NSA_PrevNS DEFAULT (0.0),
    AgeGroupWeight              DECIMAL(5,4)    NOT NULL CONSTRAINT DF_NSA_Age DEFAULT (0.0),
    DistanceWeight              DECIMAL(5,4)    NOT NULL CONSTRAINT DF_NSA_Dist DEFAULT (0.0),
    AppointmentTimeWeight       DECIMAL(5,4)    NOT NULL CONSTRAINT DF_NSA_Time DEFAULT (0.0),
    AppointmentDayWeight        DECIMAL(5,4)    NOT NULL CONSTRAINT DF_NSA_Day DEFAULT (0.0),
    WeatherWeight               DECIMAL(5,4)    NOT NULL CONSTRAINT DF_NSA_Weather DEFAULT (0.0),
    SmsResponseWeight           DECIMAL(5,4)    NOT NULL CONSTRAINT DF_NSA_Sms DEFAULT (0.0),
    InsuranceTypeWeight         DECIMAL(5,4)    NOT NULL CONSTRAINT DF_NSA_Ins DEFAULT (0.0),
    FirstVisitWeight            DECIMAL(5,4)    NOT NULL CONSTRAINT DF_NSA_First DEFAULT (0.0),
    WeatherCondition            NVARCHAR(30)    NOT NULL CONSTRAINT DF_NSA_Weather2 DEFAULT ('Unknown'),
    SmsResponse                 NVARCHAR(30)    NOT NULL CONSTRAINT DF_NSA_SmsResp DEFAULT ('NotSent'),
    SmsSentAt                   DATETIME2       NULL,
    SmsRespondedAt              DATETIME2       NULL,
    IsReminderSent              BIT             NOT NULL CONSTRAINT DF_NSA_Reminder DEFAULT (0),
    ReminderSentAt              DATETIME2       NULL,
    CalculatedAt                DATETIME2       NOT NULL CONSTRAINT DF_NSA_CalcAt DEFAULT (GETUTCDATE()),
    UpdatedAt                   DATETIME2       NULL,

    CONSTRAINT PK_NoShowAnalytics PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_NoShowAnalytics_Appointments
        FOREIGN KEY (AppointmentId) REFERENCES hospital.Appointments(Id)
        ON DELETE CASCADE ON UPDATE NO ACTION,
    CONSTRAINT UQ_NoShowAnalytics_AppointmentId
        UNIQUE (AppointmentId),
    CONSTRAINT CK_NSA_RiskScore
        CHECK (RiskScore >= 0.0000 AND RiskScore <= 1.0000),
    CONSTRAINT CK_NSA_WeatherCondition
        CHECK (WeatherCondition IN ('Unknown','Clear','Cloudy','Rainy','Stormy','Snowy','Foggy')),
    CONSTRAINT CK_NSA_SmsResponse
        CHECK (SmsResponse IN ('NotSent','Sent','Confirmed','Cancelled','NoResponse')),
    CONSTRAINT CK_NSA_SmsLogic
        CHECK (SmsResponse = 'NotSent' OR SmsSentAt IS NOT NULL)
);

CREATE UNIQUE INDEX IX_NoShowAnalytics_AppointmentId
    ON hospital.NoShowAnalytics (AppointmentId);

CREATE INDEX IX_NoShowAnalytics_RiskScore
    ON hospital.NoShowAnalytics (RiskScore DESC);
GO

-- -----------------------------------------------------------------------------
-- 11. AUDİT LOG TABLOSU (Trigger için)
-- -----------------------------------------------------------------------------
IF OBJECT_ID('hospital.AuditLog', 'U') IS NOT NULL DROP TABLE hospital.AuditLog;

CREATE TABLE hospital.AuditLog (
    Id              INT             NOT NULL IDENTITY(1,1),
    TableName       NVARCHAR(100)   NOT NULL,
    RecordId        INT             NOT NULL,
    ColumnName      NVARCHAR(100)   NOT NULL,
    OldValue        NVARCHAR(MAX)   NULL,
    NewValue        NVARCHAR(MAX)   NULL,
    ChangedBy       NVARCHAR(256)   NULL,
    ChangedAt       DATETIME2       NOT NULL CONSTRAINT DF_AuditLog_ChangedAt DEFAULT (GETUTCDATE()),
    ChangeType      NVARCHAR(10)    NOT NULL,

    CONSTRAINT PK_AuditLog PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT CK_AuditLog_ChangeType
        CHECK (ChangeType IN ('INSERT','UPDATE','DELETE'))
);

CREATE INDEX IX_AuditLog_TableRecord
    ON hospital.AuditLog (TableName, RecordId);

CREATE INDEX IX_AuditLog_ChangedAt
    ON hospital.AuditLog (ChangedAt DESC);
GO

-- =============================================================================
-- ÖZET: Oluşturulan Nesneler
-- =============================================================================
-- TABLOLAR:
--   hospital.Polyclinics         (PK, 2 CHECK)
--   hospital.Users               (PK, UNIQUE INDEX x2, 1 CHECK)
--   hospital.Patients            (PK, FK, 2 UNIQUE, 5 CHECK)
--   hospital.Doctors             (PK, 2 FK, 2 UNIQUE, 3 CHECK)
--   hospital.DoctorSchedules     (PK, FK CASCADE, UNIQUE, 2 CHECK)
--   hospital.Appointments        (PK, 2 FK, 4 CHECK, UNIQUE IDX, 3 INDEX)
--   hospital.MedicalHistories    (PK, FK CASCADE, 1 CHECK, 1 INDEX)
--   hospital.Medications         (PK, FK CASCADE, 1 CHECK, 1 INDEX)
--   hospital.NoShowAnalytics     (PK, FK CASCADE, UNIQUE, 4 CHECK, 2 INDEX)
--   hospital.AuditLog            (PK, 1 CHECK, 2 INDEX)
--
-- TOPLAM: 10 tablo, 30+ kısıt, 15+ indeks
-- =============================================================================
