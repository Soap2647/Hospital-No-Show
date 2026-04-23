# Hastane Randevu No-Show Tahmin Sistemi
## 3. Normalizasyon Analizi (1NF → 2NF → 3NF)

---

## 3.1 Normalizasyon Nedir?

**Normalizasyon**, bir ilişkisel veritabanındaki veri tekrarını (redundancy) ve güncelleme anomalilerini ortadan kaldırmak için tablolar halinde yapılandırma sürecidir.

| Normal Form | Kural |
|---|---|
| **1NF** | Her sütun atomik (bölünemez) değer içermeli; her satır benzersiz olmalı |
| **2NF** | 1NF + Kısmi bağımlılık yok (her non-key sütun, PK'nın tamamına bağımlı) |
| **3NF** | 2NF + Geçişli bağımlılık yok (non-key sütunlar yalnızca PK'ya bağımlı) |
| **BCNF** | 3NF + Her belirleyici bir aday anahtardır |

---

## 3.2 Normalize Edilmemiş Başlangıç Tablosu (Örnek)

Sistemi tasarlamadan önce, tüm verilerin tek bir tabloda tutulduğunu hayal edelim:

```
HASTANE_RANDEVU_HAM(
  RandevuNo, RandevuTarihi, RandevuSaati,
  HastaTC, HastaAdi, HastaSoyadi, HastaEposta, HastaTelefon,
  HastaSehir, HastaUzaklik, HastaSigorta,
  HastaTani1Kodu, HastaTani1Adi, HastaTani2Kodu, HastaTani2Adi,  ← Çoklu değer!
  HastaIlac1, HastaIlac1Doz, HastaIlac2, HastaIlac2Doz,         ← Çoklu değer!
  DoktorNo, DoktorAdi, DoktorUnvan, DoktorUzmanlik,
  PoliklinikNo, PoliklinikAdi, PoliklinikKat,
  RiskSkoru, SmsDurumu, HavaDurumu
)
```

**Bu yapıdaki sorunlar:**
- Tanı sütunları (Tani1, Tani2...) tekrarlanıyor → sınırlı ve genişlemiyor
- Her randevuda doktor bilgileri tekrarlanıyor
- Poliklinik bilgileri her randevuda tekrarlanıyor
- Hasta bilgileri her randevuda tekrarlanıyor
- Güncelleme anomalisi: Doktorun unvanı değişince tüm randevular güncellenmeli

---

## 3.3 Birinci Normal Form (1NF)

### Kural: Atomik değerler, tekrar eden grup yok, PK tanımlı

**1NF İhlalleri (ham tabloda):**

| İhlal | Açıklama | Çözüm |
|---|---|---|
| `HastaTani1, HastaTani2` | Tekrar eden sütun grubu | `MedicalHistories` ayrı tablo |
| `HastaIlac1, HastaIlac2` | Tekrar eden sütun grubu | `Medications` ayrı tablo |
| `HastaAdi + HastaSoyadi` | Birleşik isim tek sütunda → atomik değil | `FirstName` + `LastName` ayrı |
| Satır benzersizliği | Aynı hasta + doktor + tarih/saat için birden fazla kayıt | UNIQUE index ile sağlandı |

**1NF Sonrası:** Her sütun tek değer taşıyor, PK tanımlı, tekrar eden gruplar kaldırıldı.

```
1NF doğrulama:
✓ Appointments: Her sütun tek değer (Date, Time ayrı; Status tek değer)
✓ Patients: FirstName / LastName / Gender / City atomik
✓ MedicalHistories: Her satır tek bir tanı (Tani1/Tani2 ayrılmış)
✓ Medications: Her satır tek bir ilaç
✓ PK her tabloda tanımlı (IDENTITY sütunlar)
```

---

## 3.4 İkinci Normal Form (2NF)

### Kural: 1NF + Tüm non-key sütunlar PK'nın **tamamına** bağımlı

> 2NF yalnızca **bileşik PK** olan tablolarda sorun oluşturabilir.
> Bu veritabanındaki tablolar **tekil INT PK (IDENTITY)** kullandığı için
> kısmi bağımlılık sorunu doğası gereği ortadan kalkmıştır.
> Ancak tasarım sürecinde göz önünde bulundurulan bileşik anahtar adayları
> üzerinden analiz yapılmıştır.

### Bileşik Aday Anahtar İncelemesi

#### DoctorSchedules Tablosu

Tasarım alternatifi: `(DoctorId, DayOfWeek)` bileşik PK olabilirdi.

```
Aday bileşik PK: {DoctorId, DayOfWeek}

Bağımlılıklar:
  {DoctorId, DayOfWeek} → StartTime, EndTime, IsAvailable  ✓ (tam bağımlı)
  DoctorId              → Doctors.Specialty                 ✗ (kısmi bağımlı olurdu!)
```

**Karar:** Tekil `Id (IDENTITY)` PK kullanıldı. `DoctorId` + `DayOfWeek` ise UNIQUE kısıt olarak tanımlandı. Böylece `Specialty` gibi doktor bilgileri `Doctors` tablosunda kaldı → 2NF sağlandı.

#### Appointments Tablosu

```
Aday bileşik PK: {DoctorId, AppointmentDate, AppointmentTime}

Bağımlılıklar:
  {DoctorId, AppointmentDate, AppointmentTime} → PatientId, Status, ...  ✓
  DoctorId → Doctors.Specialty, Doctors.Title                             ✗ (kısmi!)
  PatientId → Patients.City, Patients.InsuranceType                       ✗ (kısmi!)
```

**Karar:** Tekil `Id (IDENTITY)` PK kullanıldı. Bileşik kombinasyon UNIQUE index oldu. Doktor ve hasta bilgileri kendi tablolarında → 2NF sağlandı.

### 2NF Doğrulama (Tüm Tablolar)

```
✓ Polyclinics:       PK=Id (tekil) → tüm sütunlar Id'ye bağımlı
✓ Users:             PK=Id (tekil) → tüm sütunlar Id'ye bağımlı
✓ Patients:          PK=Id (tekil) → tüm sütunlar Id'ye bağımlı
✓ Doctors:           PK=Id (tekil) → tüm sütunlar Id'ye bağımlı
✓ DoctorSchedules:   PK=Id (tekil) → tüm sütunlar Id'ye bağımlı
✓ Appointments:      PK=Id (tekil) → tüm sütunlar Id'ye bağımlı
✓ MedicalHistories:  PK=Id (tekil) → tüm sütunlar Id'ye bağımlı
✓ Medications:       PK=Id (tekil) → tüm sütunlar Id'ye bağımlı
✓ NoShowAnalytics:   PK=Id (tekil) → tüm sütunlar Id'ye bağımlı
```

**Tüm tablolar 2NF'yi sağlamaktadır.**

---

## 3.5 Üçüncü Normal Form (3NF)

### Kural: 2NF + Non-key sütunlar arasında **geçişli bağımlılık yok**

> Geçişli bağımlılık: `PK → A → B` (B, A üzerinden PK'ya bağımlı)

### İncelenen Bağımlılıklar

#### 3.5.1 Patients Tablosu

```
Fonksiyonel Bağımlılıklar:
  Id → UserId, IdentityNumber, DateOfBirth, Gender, PhoneNumber,
       Address, City, DistanceToHospitalKm, InsuranceType,
       InsurancePolicyNumber, HasChronicDisease, ChronicDiseaseNotes,
       TotalAppointments, NoShowCount, CreatedAt

Potansiyel geçişli bağımlılık analizi:
  Id → City → (PostaKodu?)          → PostaKodu sütunu YOK, sorun yok ✓
  Id → InsuranceType → (ŞirketAdı?) → Sigorta şirketi bilgisi tutulmuyor ✓
  Id → UserId → (User.Email, ...)    → User ayrı tabloda → geçişlilik yok ✓

Computed sütunlar (DB'de sütun YOK):
  NoShowRate = NoShowCount / TotalAppointments  → Hesaplanan, saklanmıyor ✓
  Age = DATEDIFF(YEAR, DateOfBirth, ...)         → Hesaplanan, saklanmıyor ✓
```

**Sonuç: Patients tablosu 3NF'yi sağlar.**

#### 3.5.2 Doctors Tablosu

```
Fonksiyonel Bağımlılıklar:
  Id → UserId, Specialty, Title, DiplomaCertNo, PolyclinicId,
       MaxDailyPatients, AvgDuration, CreatedAt

Potansiyel geçişli bağımlılık analizi:
  Id → PolyclinicId → (PolyclinicName, Department, Floor, RoomNo)
       → Poliklinik bilgileri Doctors tablosunda DEĞİL, Polyclinics'te ✓

  Id → Specialty → ?
       → Specialty ile başka bir sütun arasında bağımlılık yok ✓
```

**Tasarım kararı:** `PolyclinicName`, `Department`, `Floor` sütunları **Doctors tablosuna eklenmedi** — bunlar Polyclinics tablosunda. Bu sayede geçişli bağımlılık önlendi.

**Sonuç: Doctors tablosu 3NF'yi sağlar.**

#### 3.5.3 Appointments Tablosu

```
Fonksiyonel Bağımlılıklar:
  Id → PatientId, DoctorId, AppointmentDate, AppointmentTime,
       Status, CancellationReason, Notes, IsFirstVisit,
       SlotOrderInDay, TotalSlotsInDay, CreatedAt

Potansiyel geçişli bağımlılık analizi:
  Id → DoctorId → (DoktorAdı, Specialty, PolyclinicId)
       → Doktor bilgileri Appointments'ta yok, Doctors'ta ✓

  Id → PatientId → (HastaAd, City, InsuranceType)
       → Hasta bilgileri Appointments'ta yok, Patients'ta ✓

  Id → Status → ?
       → Status ile CancellationReason ilişkisi:
         Status = 'Cancelled' → CancellationReason required
         Bu bir BAĞIMLILIK DEĞİL, business rule → CHECK constraint ile ifade edildi ✓

  SlotOrderInDay → TotalSlotsInDay bağımlılığı?
       → Her ikisi de DoctorId + AppointmentDate tarafından belirleniyor
       → SlotOrder, TotalSlots'u belirlemez; ikisi de bağımsız olarak aynı
          randevu gününe atanıyor ✓
```

**Sonuç: Appointments tablosu 3NF'yi sağlar.**

#### 3.5.4 NoShowAnalytics Tablosu

```
Fonksiyonel Bağımlılıklar:
  Id → AppointmentId, RiskScore, (8 ağırlık sütunu),
       WeatherCondition, SmsResponse, SmsSentAt, SmsRespondedAt,
       IsReminderSent, ReminderSentAt, CalculatedAt

Potansiyel geçişli bağımlılık analizi:
  Id → RiskScore → RiskLevel ('Low'/'Medium'/'High'/'Critical')
       → RiskLevel hesaplanan (computed) bir değer, DB'de SAKLANMIYOR ✓
       → fn_GetRiskLevel() fonksiyonu ile sorgu anında hesaplanıyor ✓

  Id → SmsResponse → SmsRespondedAt
       → SmsResponse='NotSent' iken SmsRespondedAt NULL olmalı
         Bu bir BAĞIMLILIK DEĞİL, CHECK constraint ile ifade edildi ✓

  Id → WeatherWeight → (8 ağırlık birbirine bağımlı mı?)
       → Her ağırlık bağımsız olarak aynı PK'ya bağımlı, birbiriyle ilişkili değil ✓
```

**Önemli Tasarım Kararı:** `RiskLevel` (Low/Medium/High/Critical) sütunu veritabanında saklanmıyor. Saklanırsa `RiskScore → RiskLevel` geçişli bağımlılığı oluşurdu. Bunun yerine scalar function `fn_GetRiskLevel(RiskScore)` ile sorgu anında hesaplanıyor.

**Sonuç: NoShowAnalytics tablosu 3NF'yi sağlar.**

#### 3.5.5 MedicalHistories Tablosu

```
Fonksiyonel Bağımlılıklar:
  Id → PatientId, DiagnosisCode, DiagnosisName,
       DiagnosisDescription, DiagnosisDate, IsActive, CreatedAt

Potansiyel geçişli bağımlılık analizi:
  Id → DiagnosisCode → DiagnosisName?
       → ICD-10 standardında kod→isim eşleşmesi vardır.
         Bu bağımlılık olabilir! Örn: 'E11' → 'Tip 2 Diabetes Mellitus'

  Neden 3NF ihlali sayılmadı?
       → Türkiye sağlık sisteminde ICD-10 kodları her kurumda farklı
          Türkçeleştirilebiliyor; aynı kod için farklı kurumlar farklı isim kullanabiliyor.
       → DiagnosisName ve DiagnosisDescription kuruma/doktora özgü notlar içerebilir.
       → Tam bir ICD-10 referans tablosu oluşturmak projenin kapsamı dışında.
       → Uygulamada doktor kendi tanı açıklamasını giriyor → sütun hasta kaydına özgü.
```

**Tasarım Notu:** Eğer tam normalizasyon istenirse, bir `ICD10_Codes(Code PK, OfficialName)` referans tablosu oluşturulup `DiagnosisName` oraya taşınabilir. Bu tercih edilen BCNF adımı olur. Mevcut tasarımda pratik gerekçelerle DiagnosisName hastaya özgü sütun olarak bırakıldı.

**Sonuç: MedicalHistories tablosu 3NF'yi sağlar (BCNF adayı not edildi).**

---

## 3.6 Normalizasyon Özet Tablosu

| Tablo | 1NF | 2NF | 3NF | Notlar |
|---|---|---|---|---|
| Polyclinics | ✅ | ✅ | ✅ | Basit, bağımlılık yok |
| Users | ✅ | ✅ | ✅ | ASP.NET Identity standardı |
| Patients | ✅ | ✅ | ✅ | Computed: Age, NoShowRate saklanmıyor |
| Doctors | ✅ | ✅ | ✅ | Poliklinik bilgileri ayrı tabloda |
| DoctorSchedules | ✅ | ✅ | ✅ | Tekil PK, UNIQUE kısıt ayrı |
| Appointments | ✅ | ✅ | ✅ | Hasta/doktor bilgileri kendi tablolarında |
| MedicalHistories | ✅ | ✅ | ✅* | *ICD-10 ref. tablosu ile BCNF'ye taşınabilir |
| Medications | ✅ | ✅ | ✅ | Basit bağımlılık zinciri |
| NoShowAnalytics | ✅ | ✅ | ✅ | RiskLevel hesaplanan, DB'de saklanmıyor |
| AuditLog | ✅ | ✅ | ✅ | Append-only, bağımlılık yok |

---

## 3.7 Normalizasyonun Projeye Katkıları

| Katkı | Açıklama |
|---|---|
| **Veri tutarlılığı** | Doktor adı değişince yalnızca `Users` tablosu güncellenir |
| **Yer tasarrufu** | Doktor/hasta bilgileri her randevuda tekrarlanmıyor |
| **Güncelleme anomalisi yok** | Sigorta tipi değişince yalnızca `Patients` güncellemeliyiz |
| **Ekleme anomalisi yok** | Randevu olmasa da hasta veya doktor kaydı var olabilir |
| **Silme anomalisi yok** | Son randevu silinse de hasta profili korunuyor (`RESTRICT`) |
| **Referans bütünlüğü** | FK kısıtları veri tutarsızlığını önlüyor |

---

## 3.8 Denormalizasyon Kararları (Bilinçli İstisnalar)

Performans veya pratik gerekçelerle bazı sütunlar kasıtlı olarak denormalize bırakıldı:

| Sütun | Tablo | Durum | Gerekçe |
|---|---|---|---|
| `TotalAppointments` | Patients | Denormalize (türetilen veri) | `COUNT()` sorgusu yerine hızlı okuma; trigger ile güncel tutuluyor |
| `NoShowCount` | Patients | Denormalize (türetilen veri) | Risk skoru hesabında sık kullanılan alan; trigger ile güncel tutuluyor |
| `SlotOrderInDay` | Appointments | Denormalize | Günlük yoğunluk faktörü için; sorgulanabilir |
| `TotalSlotsInDay` | Appointments | Denormalize | Günlük yoğunluk faktörü için |

> **Not:** Bu denormalizasyonlar bilinçli tasarım kararlarıdır. `tr_Appointments_UpdatePatientStats` trigger'ı, `TotalAppointments` ve `NoShowCount` sütunlarını her `INSERT/UPDATE` sonrası otomatik güncelleyerek veri tutarlılığını sağlar.
