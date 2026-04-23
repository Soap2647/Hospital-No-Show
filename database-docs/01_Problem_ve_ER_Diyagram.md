# Hastane Randevu No-Show Tahmin Sistemi
## 1. Problem Tanımı ve ER Diyagramı

---

## 1.1 Problem Tanımı

### Genel Bakış

Hastane randevu sistemlerinde **"no-show"** (randevuya gelmeme) problemi, dünya genelinde ortalama **%20-30** oranında görülmekte ve ciddi operasyonel sorunlara yol açmaktadır. Türkiye'de ikinci ve üçüncü basamak sağlık kurumlarında bu oran bazı bölümlerde **%35'i** aşmaktadır.

**No-show'un etkileri:**
- Doktor çalışma saatlerinin boşa geçmesi (kaynak israfı)
- Gerçekten randevu arayan hastaların sistemde yer bulamaması
- Sağlık hizmetlerinde hizmet kalitesinin düşmesi
- Toplam tedavi maliyetlerinin artması

### Sistemin Amacı

Bu sistem, bir hastanın randevusuna gelip gelmeyeceğini **önceden tahmin eden** ve buna göre **proaktif önlem alınmasını sağlayan** bir veritabanı destekli karar destek uygulamasıdır.

### Kullanım Senaryoları

| Aktör | Senaryo |
|---|---|
| **Admin** | Sistemdeki tüm randevuları, risk analizlerini ve hasta istatistiklerini görüntüler; doktor kaydeder |
| **Doktor** | Günlük randevu listesini, hastaların risk skorlarını ve tıbbi geçmişlerini görür |
| **Hasta** | Kendi randevularını görür, yeni randevu alır, ilaçlarını takip eder |
| **Sistem** | Her randevu oluşturulduğunda otomatik risk skoru hesaplar; SMS hatırlatma durumunu takip eder |

### Risk Faktörleri

Sistem aşağıdaki 8 faktörü ağırlıklı model ile değerlendirerek **0.0–1.0** arasında risk skoru üretir:

| Faktör | Ağırlık | Açıklama |
|---|---|---|
| Geçmiş No-Show Oranı | **%35** | Bayesian düzeltmeli geçmiş performans |
| Hastaneye Uzaklık | **%12** | Sigmoid eğri (0–50+ km) |
| Günlük Yoğunluk | **%10** | Slotun günlük kalabalık içindeki konumu |
| Yaş Grubu | **%10** | 18-30 ve 65+ yaş grubunda risk yüksek |
| Randevu Saati | **%10** | Erken sabah ve öğle arası riskli |
| Haftanın Günü | **%8** | Pazartesi ve Cuma riskli |
| Sigorta Türü | **%8** | Sigortasız veya yeşil kart daha riskli |
| İlk Ziyaret | **%7** | İlk randevularda no-show daha yaygın |

**SMS Yanıtı Düzeltmesi** (risk skoru hesaplandıktan sonra uygulanır):
- Onaylayan hasta: `-0.25` (risk düşer)
- İptal bildiren hasta: `+0.40` (risk çok artar)
- Yanıt vermeyen: `+0.10`

---

## 1.2 Varlıklar ve Özellikleri

### Varlık Listesi

| # | Varlık | Açıklama | Tablo |
|---|---|---|---|
| 1 | **Kullanıcı** | Sisteme giriş yapan kişi (Admin/Doktor/Hasta) | `hospital.Users` |
| 2 | **Hasta** | Randevu alan, risk analizi yapılan kişi | `hospital.Patients` |
| 3 | **Doktor** | Randevu veren uzman hekim | `hospital.Doctors` |
| 4 | **Poliklinik** | Doktorların bağlı olduğu bölüm | `hospital.Polyclinics` |
| 5 | **Doktor Programı** | Doktorun günlük çalışma takvimi | `hospital.DoctorSchedules` |
| 6 | **Randevu** | Hasta-Doktor buluşmasının kaydı | `hospital.Appointments` |
| 7 | **Tıbbi Geçmiş** | Hastanın tanı kayıtları (ICD-10) | `hospital.MedicalHistories` |
| 8 | **İlaç** | Hastanın kullandığı ilaçlar | `hospital.Medications` |
| 9 | **No-Show Analitik** | Randevu başına risk skoru ve faktörleri | `hospital.NoShowAnalytics` |

---

## 1.3 ER Diyagramı (ASCII — Crow's Foot Notasyonu)

```
Gösterim açıklaması:
  ||---- = Bire-bir (One-to-One)
  |o---- = Sıfır veya bir
  |<---- = Birden-çoğa (One-to-Many)
  >o---- = Sıfır veya çok
  PK = Birincil Anahtar
  FK = Yabancı Anahtar
  UK = Benzersiz Kısıt


┌─────────────────────────────┐
│     hospital.Polyclinics    │
│─────────────────────────────│
│ PK  Id           INT        │
│     Name         NVARCHAR   │
│     Department   NVARCHAR   │
│     Floor        NVARCHAR   │
│     RoomNumber   NVARCHAR   │
│     PhoneExt     NVARCHAR   │
│     IsActive     BIT        │
└──────────────┬──────────────┘
               │ 1
               │ (Polikliniğe birden fazla
               │  doktor bağlı olabilir)
               │ N
┌──────────────▼──────────────┐          ┌─────────────────────────────┐
│      hospital.Doctors       │          │   hospital.DoctorSchedules  │
│─────────────────────────────│          │─────────────────────────────│
│ PK  Id           INT        │ 1      N │ PK  Id          INT         │
│ FK  UserId       NVARCHAR   ├──────────► FK  DoctorId    INT         │
│ FK  PolyclinicId INT        │          │     DayOfWeek   NVARCHAR    │
│ UK  DiplomaCertNo NVARCHAR  │          │     StartTime   TIME        │
│     Specialty    NVARCHAR   │          │     EndTime     TIME        │
│     Title        NVARCHAR   │          │     IsAvailable BIT         │
│     MaxDailyPat  INT        │          │ UK  (DoctorId, DayOfWeek)   │
│     AvgDuration  INT        │          └─────────────────────────────┘
└──────────────┬──────────────┘
               │ 1           ||
               │             ||
               │ N           ||
┌──────────────▼──────────────┐          ┌─────────────────────────────┐
│    hospital.Appointments    │          │  hospital.NoShowAnalytics   │
│─────────────────────────────│          │─────────────────────────────│
│ PK  Id           INT        │ 1      1 │ PK  Id            INT       │
│ FK  PatientId    INT        ├──────────► FK  AppointmentId  INT      │
│ FK  DoctorId     INT        │          │ UK  AppointmentId  (unique) │
│     AppointmentDate DATETIME│          │     RiskScore      DEC(5,4) │
│     AppointmentTime TIME    │          │     PrevNSWeight   DEC(5,4) │
│     Status       NVARCHAR   │          │     AgeGroupWeight DEC(5,4) │
│     IsFirstVisit BIT        │          │     DistanceWeight DEC(5,4) │
│     SlotOrder    INT        │          │     TimeWeight     DEC(5,4) │
│     TotalSlots   INT        │          │     DayWeight      DEC(5,4) │
│ UK  (DoctorId,Date,Time)    │          │     SmsRespWeight  DEC(5,4) │
└──────────────┬──────────────┘          │     InsuranceWeight DEC(5,4)│
               │ N                       │     FirstVisitWeight DEC(5,4)│
               │                        │     WeatherCondition NVARCHAR│
               │ 1                       │     SmsResponse    NVARCHAR  │
┌──────────────▼──────────────┐          └─────────────────────────────┘
│      hospital.Patients      │
│─────────────────────────────│
│ PK  Id           INT        │
│ FK  UserId       NVARCHAR   │ ◄────────────────────────────────────┐
│ UK  IdentityNo   NCHAR(11)  │                                      │
│ UK  UserId       (unique)   │          ┌──────────────────────────┐ │
│     DateOfBirth  DATE       │          │      hospital.Users      │ │
│     Gender       NVARCHAR   │          │──────────────────────────│ │
│     City         NVARCHAR   │  1     1 │ PK  Id       NVARCHAR(450)├─┘
│     DistanceKm   DECIMAL    ├──────────► (ASP.NET Identity User)  │
│     InsuranceType NVARCHAR  │          │     FirstName  NVARCHAR  │◄─┐
│     HasChronic   BIT        │          │     LastName   NVARCHAR  │  │
│     TotalAppts   INT        │          │     Email      NVARCHAR  │  │
│     NoShowCount  INT        │          │     IsActive   BIT       │  │
└──────────────┬──────────────┘          └──────────────────────────┘  │
               │ 1                                        ▲ 1          │
               │                                          │ 1          │
               │ N                           FK(UserId)   │            │
┌──────────────▼──────────────┐          ┌───────────────┘            │
│  hospital.MedicalHistories  │          │   hospital.Doctors         │
│─────────────────────────────│          │   (UserId → Users.Id)      │
│ PK  Id          INT         │          └────────────────────────────┘
│ FK  PatientId   INT         │
│     DiagnosisCode NVARCHAR  │          (1 Kullanıcı → 1 Hasta VEYA
│     DiagnosisName NVARCHAR  │           1 Kullanıcı → 1 Doktor,
│     DiagnosisDate DATE      │           fakat Hasta-Doktor aynı kullanıcı
│     IsActive    BIT         │           olamaz — uygulama seviyesinde)
└──────────────┬──────────────┘
               │ 1
               │ N
┌──────────────▼──────────────┐
│     hospital.Medications    │
│─────────────────────────────│
│ PK  Id              INT     │
│ FK  MedicalHistoryId INT    │
│     Name            NVARCHAR│
│     Dosage          NVARCHAR│
│     Frequency       NVARCHAR│
│     StartDate       DATE    │
│     EndDate         DATE    │
└─────────────────────────────┘
```

---

## 1.4 İlişki Kardinaliteleri

| İlişki | Tür | Kural |
|---|---|---|
| Kullanıcı → Hasta | 1:1 | Her hastanın bir kullanıcı hesabı var |
| Kullanıcı → Doktor | 1:1 | Her doktorun bir kullanıcı hesabı var |
| Poliklinik → Doktor | 1:N | Bir poliklinikte birden fazla doktor çalışabilir |
| Doktor → Çalışma Saatleri | 1:N | Bir doktorun her gün için bir programı olabilir |
| Doktor → Randevu | 1:N | Bir doktor birçok randevu alır |
| Hasta → Randevu | 1:N | Bir hastanın birçok randevusu olabilir |
| Randevu → No-Show Analitik | 1:1 | Her randevu için bir risk kaydı |
| Hasta → Tıbbi Geçmiş | 1:N | Bir hastanın birden fazla tanısı olabilir |
| Tıbbi Geçmiş → İlaç | 1:N | Bir tanıya birden fazla ilaç bağlanabilir |

---

## 1.5 İş Kuralları

1. **Çakışma Yasağı:** Aynı doktor, aynı gün ve saatte yalnızca bir randevu verebilir
   → `IX_Appointments_Doctor_DateTime` (UNIQUE index)

2. **İptal Gerekçesi:** Randevu iptal edildiğinde gerekçe girilmesi zorunludur
   → `CK_Appointments_CancelReason` (CHECK constraint)

3. **No-Show Tutarlılığı:** Gelmeyen randevu sayısı toplam randevu sayısını geçemez
   → `CK_Patients_NoShowCount` (CHECK constraint)

4. **Doktor Programı Tekliği:** Bir doktor aynı gün için birden fazla program giremez
   → `UQ_DoctorSchedule_Doctor_Day` (UNIQUE constraint)

5. **TC Kimlik Numarası:** 11 haneli, yalnızca rakamlardan oluşur
   → `CK_Patients_IdentityNumber` (CHECK constraint)

6. **Risk Skoru Aralığı:** Risk skoru 0.0000 ile 1.0000 arasında olmalıdır
   → `CK_NSA_RiskScore` (CHECK constraint)

7. **SMS Tutarlılığı:** SMS gönderilmeden yanıt alınamaz
   → `CK_NSA_SmsLogic` (CHECK constraint)
