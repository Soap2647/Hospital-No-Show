# Hastane Randevu No-Show Tahmin Sistemi
## 2. İlişkisel Şema ve Kısıtlar

---

## 2.1 İlişkisel Şema Notasyonu

> **Kullanılan Gösterim:**
> - `PK` = Birincil Anahtar (Primary Key)
> - `FK → Tablo.Sütun` = Yabancı Anahtar (Foreign Key)
> - `UK` = Benzersiz Kısıt (Unique Key)
> - `*` = NOT NULL
> - _italik_ = NULL olabilir

---

### Tablo 1: hospital.Polyclinics

```
Polyclinics(
  PK  Id*             INT          IDENTITY,
      Name*           NVARCHAR(150),
      Department*     NVARCHAR(100),
      Floor*          NVARCHAR(20),
      RoomNumber*     NVARCHAR(20),
      PhoneExtension* NVARCHAR(10),
      IsActive*       BIT          DEFAULT 1
)
```

---

### Tablo 2: hospital.Users *(ASP.NET Identity)*

```
Users(
  PK  Id*                  NVARCHAR(450),
      FirstName*           NVARCHAR(MAX),
      LastName*            NVARCHAR(MAX),
      CreatedAt*           DATETIME2    DEFAULT GETUTCDATE(),
      _LastLoginAt         DATETIME2,
      IsActive*            BIT          DEFAULT 1,
  UK  _NormalizedUserName  NVARCHAR(256),
      _Email               NVARCHAR(256),
      _NormalizedEmail     NVARCHAR(256),
      EmailConfirmed*      BIT,
      _PasswordHash        NVARCHAR(MAX),
      _SecurityStamp       NVARCHAR(MAX),
      _ConcurrencyStamp    NVARCHAR(MAX),
      _PhoneNumber         NVARCHAR(MAX),
      PhoneNumberConfirmed* BIT,
      TwoFactorEnabled*    BIT,
      _LockoutEnd          DATETIMEOFFSET,
      LockoutEnabled*      BIT,
      AccessFailedCount*   INT
)
```

---

### Tablo 3: hospital.Patients

```
Patients(
  PK  Id*                    INT          IDENTITY,
  FK  UserId*                NVARCHAR(450) → Users.Id  [RESTRICT],
  UK  UserId,
  UK  IdentityNumber*        NCHAR(11),
      DateOfBirth*           DATE,
      Gender*                NVARCHAR(20)  CHECK IN('Male','Female','Other','PreferNotToSay'),
      PhoneNumber*           NVARCHAR(20),
      Address*               NVARCHAR(500),
      City*                  NVARCHAR(100),
      DistanceToHospitalKm*  DECIMAL(10,2) DEFAULT 0  CHECK(0–500),
      InsuranceType*         NVARCHAR(30)  CHECK IN('None','SGK','PrivateInsurance','GreenCard','SelfPay'),
      _InsurancePolicyNumber NVARCHAR(50),
      HasChronicDisease*     BIT           DEFAULT 0,
      _ChronicDiseaseNotes   NVARCHAR(1000),
      TotalAppointments*     INT           DEFAULT 0  CHECK(≥0),
      NoShowCount*           INT           DEFAULT 0  CHECK(0 ≤ NoShowCount ≤ TotalAppointments),
      CreatedAt*             DATETIME2     DEFAULT GETUTCDATE(),
      _UpdatedAt             DATETIME2
)
```

---

### Tablo 4: hospital.Doctors

```
Doctors(
  PK  Id*                              INT          IDENTITY,
  FK  UserId*                          NVARCHAR(450) → Users.Id  [RESTRICT],
  UK  UserId,
  FK  PolyclinicId*                    INT          → Polyclinics.Id  [RESTRICT],
  UK  DiplomaCertificateNumber*        NVARCHAR(50),
      Specialty*                       NVARCHAR(100),
      Title*                           NVARCHAR(50)  CHECK IN('Prof. Dr.','Doç. Dr.','Dr.','Uzm. Dr.','Op. Dr.'),
      MaxDailyPatients*                INT           DEFAULT 20  CHECK(1–100),
      AverageAppointmentDurationMinutes* INT          DEFAULT 15  CHECK(5–120),
      CreatedAt*                       DATETIME2     DEFAULT GETUTCDATE(),
      _UpdatedAt                       DATETIME2
)
```

---

### Tablo 5: hospital.DoctorSchedules

```
DoctorSchedules(
  PK  Id*          INT          IDENTITY,
  FK  DoctorId*    INT          → Doctors.Id  [CASCADE DELETE],
      DayOfWeek*   NVARCHAR(15) CHECK IN('Sunday',...,'Saturday'),
      StartTime*   TIME,
      EndTime*     TIME         CHECK(EndTime > StartTime),
      IsAvailable* BIT          DEFAULT 1,
  UK  (DoctorId, DayOfWeek)
)
```

---

### Tablo 6: hospital.Appointments

```
Appointments(
  PK  Id*                INT          IDENTITY,
  FK  PatientId*         INT          → Patients.Id  [RESTRICT],
  FK  DoctorId*          INT          → Doctors.Id   [RESTRICT],
      AppointmentDate*   DATETIME2,
      AppointmentTime*   TIME,
      CreatedAt*         DATETIME2    DEFAULT GETUTCDATE(),
      _UpdatedAt         DATETIME2,
      Status*            NVARCHAR(30) DEFAULT 'Scheduled'
                                      CHECK IN('Scheduled','Completed','NoShow','Cancelled','Rescheduled'),
      _CancellationReason NVARCHAR(500) CHECK(Status='Cancelled' → NOT NULL),
      _Notes             NVARCHAR(1000),
      IsFirstVisit*      BIT          DEFAULT 0,
      SlotOrderInDay*    INT          CHECK(1 ≤ SlotOrder ≤ TotalSlots),
      TotalSlotsInDay*   INT          CHECK(≥1),
  UK  (DoctorId, AppointmentDate, AppointmentTime)   -- Çakışma önlemi
)
```

---

### Tablo 7: hospital.MedicalHistories

```
MedicalHistories(
  PK  Id*                    INT          IDENTITY,
  FK  PatientId*             INT          → Patients.Id  [CASCADE DELETE],
      DiagnosisCode*         NVARCHAR(10) CHECK(LEN≥3),   -- ICD-10
      DiagnosisName*         NVARCHAR(200),
      _DiagnosisDescription  NVARCHAR(2000),
      DiagnosisDate*         DATE,
      IsActive*              BIT          DEFAULT 1,
      CreatedAt*             DATETIME2    DEFAULT GETUTCDATE(),
      _UpdatedAt             DATETIME2
)
```

---

### Tablo 8: hospital.Medications

```
Medications(
  PK  Id*              INT          IDENTITY,
  FK  MedicalHistoryId* INT         → MedicalHistories.Id  [CASCADE DELETE],
      Name*            NVARCHAR(200),
      Dosage*          NVARCHAR(100),
      Frequency*       NVARCHAR(100),
      StartDate*       DATE,
      _EndDate         DATE         CHECK(EndDate ≥ StartDate),
      _Notes           NVARCHAR(500)
)
```

---

### Tablo 9: hospital.NoShowAnalytics

```
NoShowAnalytics(
  PK  Id*                       INT          IDENTITY,
  FK  AppointmentId*            INT          → Appointments.Id  [CASCADE DELETE],
  UK  AppointmentId,
      RiskScore*                DECIMAL(5,4) DEFAULT 0  CHECK(0.0000–1.0000),
      PreviousNoShowRateWeight* DECIMAL(5,4) DEFAULT 0,
      AgeGroupWeight*           DECIMAL(5,4) DEFAULT 0,
      DistanceWeight*           DECIMAL(5,4) DEFAULT 0,
      AppointmentTimeWeight*    DECIMAL(5,4) DEFAULT 0,
      AppointmentDayWeight*     DECIMAL(5,4) DEFAULT 0,
      WeatherWeight*            DECIMAL(5,4) DEFAULT 0,
      SmsResponseWeight*        DECIMAL(5,4) DEFAULT 0,
      InsuranceTypeWeight*      DECIMAL(5,4) DEFAULT 0,
      FirstVisitWeight*         DECIMAL(5,4) DEFAULT 0,
      WeatherCondition*         NVARCHAR(30) DEFAULT 'Unknown'
                                             CHECK IN('Unknown','Clear','Cloudy','Rainy','Stormy','Snowy','Foggy'),
      SmsResponse*              NVARCHAR(30) DEFAULT 'NotSent'
                                             CHECK IN('NotSent','Sent','Confirmed','Cancelled','NoResponse'),
      _SmsSentAt                DATETIME2    CHECK(SmsResponse≠'NotSent' → NOT NULL),
      _SmsRespondedAt           DATETIME2,
      IsReminderSent*           BIT          DEFAULT 0,
      _ReminderSentAt           DATETIME2,
      CalculatedAt*             DATETIME2    DEFAULT GETUTCDATE(),
      _UpdatedAt                DATETIME2
)
```

---

### Tablo 10: hospital.AuditLog *(Audit Trail)*

```
AuditLog(
  PK  Id*        INT          IDENTITY,
      TableName* NVARCHAR(100),
      RecordId*  INT,
      ColumnName* NVARCHAR(100),
      _OldValue  NVARCHAR(MAX),
      _NewValue  NVARCHAR(MAX),
      _ChangedBy NVARCHAR(256),
      ChangedAt* DATETIME2    DEFAULT GETUTCDATE(),
      ChangeType* NVARCHAR(10) CHECK IN('INSERT','UPDATE','DELETE')
)
```

---

## 2.2 Tüm Kısıtların Özet Tablosu

### Birincil Anahtarlar (PRIMARY KEY)

| Tablo | PK Sütunu | Tip | Özellik |
|---|---|---|---|
| Polyclinics | Id | INT | IDENTITY(1,1) |
| Users | Id | NVARCHAR(450) | GUID formatında |
| Patients | Id | INT | IDENTITY(1,1) |
| Doctors | Id | INT | IDENTITY(1,1) |
| DoctorSchedules | Id | INT | IDENTITY(1,1) |
| Appointments | Id | INT | IDENTITY(1,1) |
| MedicalHistories | Id | INT | IDENTITY(1,1) |
| Medications | Id | INT | IDENTITY(1,1) |
| NoShowAnalytics | Id | INT | IDENTITY(1,1) |
| AuditLog | Id | INT | IDENTITY(1,1) |

---

### Yabancı Anahtarlar (FOREIGN KEY)

| Kısıt Adı | Tablo | Sütun | Referans | Silme Davranışı |
|---|---|---|---|---|
| FK_Patients_Users | Patients | UserId | Users.Id | RESTRICT |
| FK_Doctors_Users | Doctors | UserId | Users.Id | RESTRICT |
| FK_Doctors_Polyclinics | Doctors | PolyclinicId | Polyclinics.Id | RESTRICT |
| FK_DoctorSchedules_Doctors | DoctorSchedules | DoctorId | Doctors.Id | **CASCADE** |
| FK_Appointments_Patients | Appointments | PatientId | Patients.Id | RESTRICT |
| FK_Appointments_Doctors | Appointments | DoctorId | Doctors.Id | RESTRICT |
| FK_MedicalHistories_Patients | MedicalHistories | PatientId | Patients.Id | **CASCADE** |
| FK_Medications_MedicalHistories | Medications | MedicalHistoryId | MedicalHistories.Id | **CASCADE** |
| FK_NoShowAnalytics_Appointments | NoShowAnalytics | AppointmentId | Appointments.Id | **CASCADE** |

> **CASCADE kullanım gerekçesi:**
> - Doktor silindiğinde çalışma saatleri anlamsız → CASCADE
> - Hasta silindiğinde tıbbi geçmişi anlamsız → CASCADE
> - Randevu silindiğinde risk analizi anlamsız → CASCADE
> - Hasta/Doktor kaydı silindiğinde User kaydına dokunulmamalı → RESTRICT

---

### Benzersiz Kısıtlar (UNIQUE)

| Kısıt Adı | Tablo | Sütunlar | Açıklama |
|---|---|---|---|
| UQ_Patients_UserId | Patients | UserId | Her kullanıcıya en fazla bir hasta kaydı |
| UQ_Patients_IdentityNumber | Patients | IdentityNumber | TCKN tekrarlanamaz |
| UQ_Doctors_UserId | Doctors | UserId | Her kullanıcıya en fazla bir doktor kaydı |
| UQ_Doctors_DiplomaCertNo | Doctors | DiplomaCertificateNumber | Diploma no tekrarlanamaz |
| UQ_DoctorSchedule_Doctor_Day | DoctorSchedules | (DoctorId, DayOfWeek) | Aynı gün için tek program |
| IX_Appointments_Doctor_DateTime | Appointments | (DoctorId, AppointmentDate, AppointmentTime) | Çakışma önleme |
| UQ_NoShowAnalytics_AppointmentId | NoShowAnalytics | AppointmentId | Her randevu için tek analitik kayıt |
| UserNameIndex | Users | NormalizedUserName | Kullanıcı adı tekrarlanamaz |

---

### CHECK Kısıtları

| Kısıt Adı | Tablo | Kural |
|---|---|---|
| CK_Polyclinics_Name | Polyclinics | `LEN(LTRIM(RTRIM(Name))) > 0` |
| CK_Polyclinics_PhoneExtension | Polyclinics | Yalnızca rakam |
| CK_Users_Email | Users | `Email LIKE '%@%.%'` |
| CK_Patients_Gender | Patients | `IN ('Male','Female','Other','PreferNotToSay')` |
| CK_Patients_InsuranceType | Patients | `IN ('None','SGK','PrivateInsurance','GreenCard','SelfPay')` |
| CK_Patients_Distance | Patients | `0 ≤ DistanceToHospitalKm ≤ 500` |
| CK_Patients_IdentityNumber | Patients | 11 hane, yalnızca rakam |
| CK_Patients_NoShowCount | Patients | `0 ≤ NoShowCount ≤ TotalAppointments` |
| CK_Patients_TotalAppt | Patients | `TotalAppointments ≥ 0` |
| CK_Doctors_MaxDailyPatients | Doctors | `1 ≤ MaxDailyPatients ≤ 100` |
| CK_Doctors_AvgDuration | Doctors | `5 ≤ AvgDuration ≤ 120 (dakika)` |
| CK_Doctors_Title | Doctors | `IN ('Prof. Dr.','Doç. Dr.','Dr.','Uzm. Dr.','Op. Dr.')` |
| CK_DoctorSchedules_DayOfWeek | DoctorSchedules | `IN ('Sunday',...,'Saturday')` |
| CK_DoctorSchedules_TimeRange | DoctorSchedules | `EndTime > StartTime` |
| CK_Appointments_Status | Appointments | `IN ('Scheduled','Completed','NoShow','Cancelled','Rescheduled')` |
| CK_Appointments_SlotOrder | Appointments | `1 ≤ SlotOrderInDay ≤ TotalSlotsInDay` |
| CK_Appointments_CancelReason | Appointments | `Status='Cancelled' → CancellationReason IS NOT NULL` |
| CK_MedicalHistories_Code | MedicalHistories | `LEN(DiagnosisCode) ≥ 3` |
| CK_Medications_DateRange | Medications | `EndDate IS NULL OR EndDate ≥ StartDate` |
| CK_NSA_RiskScore | NoShowAnalytics | `0.0000 ≤ RiskScore ≤ 1.0000` |
| CK_NSA_WeatherCondition | NoShowAnalytics | `IN ('Unknown','Clear','Cloudy','Rainy','Stormy','Snowy','Foggy')` |
| CK_NSA_SmsResponse | NoShowAnalytics | `IN ('NotSent','Sent','Confirmed','Cancelled','NoResponse')` |
| CK_NSA_SmsLogic | NoShowAnalytics | `SmsResponse='NotSent' OR SmsSentAt IS NOT NULL` |
| CK_AuditLog_ChangeType | AuditLog | `IN ('INSERT','UPDATE','DELETE')` |

---

### DEFAULT Değerleri

| Tablo | Sütun | Default Değer |
|---|---|---|
| Polyclinics | IsActive | `1` (aktif) |
| Users | CreatedAt | `GETUTCDATE()` |
| Users | IsActive | `1` |
| Patients | DistanceToHospitalKm | `0` |
| Patients | InsuranceType | `'None'` |
| Patients | HasChronicDisease | `0` |
| Patients | TotalAppointments | `0` |
| Patients | NoShowCount | `0` |
| Patients | CreatedAt | `GETUTCDATE()` |
| Doctors | MaxDailyPatients | `20` |
| Doctors | AverageAppointmentDurationMinutes | `15` |
| Doctors | CreatedAt | `GETUTCDATE()` |
| DoctorSchedules | IsAvailable | `1` |
| Appointments | CreatedAt | `GETUTCDATE()` |
| Appointments | Status | `'Scheduled'` |
| Appointments | IsFirstVisit | `0` |
| Appointments | SlotOrderInDay | `1` |
| Appointments | TotalSlotsInDay | `1` |
| MedicalHistories | IsActive | `1` |
| MedicalHistories | CreatedAt | `GETUTCDATE()` |
| NoShowAnalytics | RiskScore | `0.0` |
| NoShowAnalytics | WeatherCondition | `'Unknown'` |
| NoShowAnalytics | SmsResponse | `'NotSent'` |
| NoShowAnalytics | IsReminderSent | `0` |
| NoShowAnalytics | CalculatedAt | `GETUTCDATE()` |
| AuditLog | ChangedAt | `GETUTCDATE()` |

---

## 2.3 İndeks Listesi

| İndeks Adı | Tablo | Sütunlar | Tür | Amaç |
|---|---|---|---|---|
| UserNameIndex | Users | NormalizedUserName | UNIQUE (filtered) | Login hızı |
| EmailIndex | Users | NormalizedEmail | NON-UNIQUE | Email arama |
| IX_Patients_UserId | Patients | UserId | NON-UNIQUE | FK join |
| IX_Patients_City | Patients | City | NON-UNIQUE | Şehir filtresi |
| IX_Doctors_UserId | Doctors | UserId | NON-UNIQUE | FK join |
| IX_Doctors_PolyclinicId | Doctors | PolyclinicId | NON-UNIQUE | FK join |
| IX_Doctors_Specialty | Doctors | Specialty | NON-UNIQUE | Uzmanlık filtresi |
| UQ_DoctorSchedule_Doctor_Day | DoctorSchedules | (DoctorId, DayOfWeek) | UNIQUE | Çakışma önleme |
| IX_Appointments_PatientId | Appointments | PatientId | NON-UNIQUE | Hasta randevuları |
| **IX_Appointments_Doctor_DateTime** | **Appointments** | **(DoctorId, AppDate, AppTime)** | **UNIQUE** | **Kritik: çakışma önleme** |
| IX_Appointments_Date | Appointments | AppointmentDate | NON-UNIQUE | Tarih bazlı sorgu |
| IX_Appointments_Status | Appointments | Status | NON-UNIQUE | Durum filtresi |
| IX_MedicalHistory_Patient_Diagnosis | MedicalHistories | (PatientId, DiagnosisCode) | NON-UNIQUE | Tanı arama |
| IX_Medications_MedicalHistoryId | Medications | MedicalHistoryId | NON-UNIQUE | FK join |
| UQ_NoShowAnalytics_AppointmentId | NoShowAnalytics | AppointmentId | UNIQUE | 1:1 sağlama |
| IX_NoShowAnalytics_RiskScore | NoShowAnalytics | RiskScore DESC | NON-UNIQUE | Risk sıralaması |
| IX_AuditLog_TableRecord | AuditLog | (TableName, RecordId) | NON-UNIQUE | Kayıt geçmişi |
| IX_AuditLog_ChangedAt | AuditLog | ChangedAt DESC | NON-UNIQUE | Zaman bazlı sorgu |
