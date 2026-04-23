-- =============================================================================
-- HASTANE RANDEVU NO-SHOW TAHMİN SİSTEMİ
-- SQL Sorguları: Temel (10 adet) + İleri (10 adet)
-- =============================================================================

USE HospitalNoShowDb_Dev;
GO

-- =============================================================================
-- BÖLÜM A: TEMEL SORGULAR (10 adet)
-- =============================================================================

-- ----------------------------------------------------------------------------
-- T1: Tüm aktif hastaları ad, şehir ve sigorta türüyle listele
-- ----------------------------------------------------------------------------
SELECT
    p.Id,
    u.FirstName + ' ' + u.LastName   AS AdSoyad,
    u.Email,
    p.City                           AS Sehir,
    p.InsuranceType                  AS SigortaTuru,
    DATEDIFF(YEAR, p.DateOfBirth, GETDATE())
                                     AS Yas,
    p.DistanceToHospitalKm           AS HastaneyeUzaklikKm,
    p.TotalAppointments              AS ToplamRandevu,
    p.NoShowCount                    AS GelmediSayisi
FROM hospital.Patients p
INNER JOIN hospital.Users u ON u.Id = p.UserId
WHERE u.IsActive = 1
ORDER BY u.LastName, u.FirstName;
GO

-- ----------------------------------------------------------------------------
-- T2: Belirli bir doktorun tüm randevularını getir (DoctorId = 1)
-- ----------------------------------------------------------------------------
SELECT
    a.Id                             AS RandevuId,
    a.AppointmentDate                AS Tarih,
    a.AppointmentTime                AS Saat,
    a.Status                         AS Durum,
    up.FirstName + ' ' + up.LastName AS HastaAdSoyad,
    a.IsFirstVisit                   AS IlkZiyaret,
    a.Notes                          AS Notlar
FROM hospital.Appointments a
INNER JOIN hospital.Patients p  ON p.Id = a.PatientId
INNER JOIN hospital.Users up    ON up.Id = p.UserId
WHERE a.DoctorId = 1
ORDER BY a.AppointmentDate DESC, a.AppointmentTime;
GO

-- ----------------------------------------------------------------------------
-- T3: Toplam no-show sayısı 3'ten fazla olan hastalar
-- ----------------------------------------------------------------------------
SELECT
    p.Id,
    u.FirstName + ' ' + u.LastName   AS AdSoyad,
    p.TotalAppointments              AS ToplamRandevu,
    p.NoShowCount                    AS GelmediSayisi,
    CAST(p.NoShowCount * 100.0 / NULLIF(p.TotalAppointments, 0) AS DECIMAL(5,1))
                                     AS GelmemeOrani_Pct
FROM hospital.Patients p
INNER JOIN hospital.Users u ON u.Id = p.UserId
WHERE p.NoShowCount > 3
ORDER BY p.NoShowCount DESC;
GO

-- ----------------------------------------------------------------------------
-- T4: Polikliniğe göre toplam randevu sayısı
-- ----------------------------------------------------------------------------
SELECT
    pol.Name                         AS PoliklinikAdi,
    pol.Department                   AS Bolum,
    COUNT(a.Id)                      AS ToplamRandevu,
    SUM(CASE WHEN a.Status = 'Completed' THEN 1 ELSE 0 END)  AS Tamamlanan,
    SUM(CASE WHEN a.Status = 'NoShow'    THEN 1 ELSE 0 END)  AS GelmeyenSayisi,
    SUM(CASE WHEN a.Status = 'Cancelled' THEN 1 ELSE 0 END)  AS IptalSayisi
FROM hospital.Polyclinics pol
INNER JOIN hospital.Doctors d  ON d.PolyclinicId = pol.Id
INNER JOIN hospital.Appointments a ON a.DoctorId = d.Id
GROUP BY pol.Id, pol.Name, pol.Department
ORDER BY ToplamRandevu DESC;
GO

-- ----------------------------------------------------------------------------
-- T5: Son 30 gündeki iptal edilen randevular
-- ----------------------------------------------------------------------------
SELECT
    a.Id                             AS RandevuId,
    a.AppointmentDate                AS RandevuTarihi,
    up.FirstName + ' ' + up.LastName AS HastaAdSoyad,
    ud.FirstName + ' ' + ud.LastName AS DoktorAdSoyad,
    pol.Name                         AS Poliklinik,
    a.CancellationReason             AS IptalNedeni,
    a.CreatedAt                      AS RandevuOlusturulmaTarihi,
    DATEDIFF(DAY, a.CreatedAt, a.AppointmentDate) AS OnceSonra_Gun
FROM hospital.Appointments a
INNER JOIN hospital.Patients p  ON p.Id = a.PatientId
INNER JOIN hospital.Users up    ON up.Id = p.UserId
INNER JOIN hospital.Doctors d   ON d.Id = a.DoctorId
INNER JOIN hospital.Users ud    ON ud.Id = d.UserId
INNER JOIN hospital.Polyclinics pol ON pol.Id = d.PolyclinicId
WHERE a.Status = 'Cancelled'
  AND a.UpdatedAt >= DATEADD(DAY, -30, GETUTCDATE())
ORDER BY a.UpdatedAt DESC;
GO

-- ----------------------------------------------------------------------------
-- T6: Yüksek riskli hastaları listele (No-Show oranı > %40, min 5 randevu)
-- ----------------------------------------------------------------------------
SELECT
    p.Id                             AS HastaId,
    u.FirstName + ' ' + u.LastName   AS AdSoyad,
    p.City                           AS Sehir,
    p.InsuranceType                  AS Sigorta,
    p.TotalAppointments              AS ToplamRandevu,
    p.NoShowCount                    AS GelmediSayisi,
    CAST(p.NoShowCount * 100.0 / p.TotalAppointments AS DECIMAL(5,1))
                                     AS GelmemeOrani_Pct,
    p.DistanceToHospitalKm           AS UzaklikKm,
    p.HasChronicDisease              AS KronikHastalik
FROM hospital.Patients p
INNER JOIN hospital.Users u ON u.Id = p.UserId
WHERE p.TotalAppointments >= 5
  AND (CAST(p.NoShowCount AS FLOAT) / p.TotalAppointments) > 0.40
ORDER BY (CAST(p.NoShowCount AS FLOAT) / p.TotalAppointments) DESC;
GO

-- ----------------------------------------------------------------------------
-- T7: Her doktorun adı, polikliniği ve toplam hasta (randevu) sayısı
-- ----------------------------------------------------------------------------
SELECT
    d.Id                             AS DoktorId,
    ud.FirstName + ' ' + ud.LastName AS DoktorAdSoyad,
    d.Title                          AS Unvan,
    d.Specialty                      AS Uzmanlik,
    pol.Name                         AS Poliklinik,
    COUNT(DISTINCT a.PatientId)      AS ToplamHastaSayisi,
    COUNT(a.Id)                      AS ToplamRandevuSayisi,
    d.MaxDailyPatients               AS MaxGunlukHasta
FROM hospital.Doctors d
INNER JOIN hospital.Users ud         ON ud.Id = d.UserId
INNER JOIN hospital.Polyclinics pol  ON pol.Id = d.PolyclinicId
LEFT  JOIN hospital.Appointments a   ON a.DoctorId = d.Id
GROUP BY d.Id, ud.FirstName, ud.LastName, d.Title, d.Specialty, pol.Name, d.MaxDailyPatients
ORDER BY ToplamRandevuSayisi DESC;
GO

-- ----------------------------------------------------------------------------
-- T8: Durumu 'Scheduled' olan gelecek randevular (bekleyen liste)
-- ----------------------------------------------------------------------------
SELECT
    a.Id                             AS RandevuId,
    a.AppointmentDate                AS Tarih,
    a.AppointmentTime                AS Saat,
    up.FirstName + ' ' + up.LastName AS HastaAdSoyad,
    ud.FirstName + ' ' + ud.LastName AS DoktorAdSoyad,
    d.Specialty                      AS Uzmanlik,
    pol.Name                         AS Poliklinik,
    a.IsFirstVisit                   AS IlkZiyaret,
    nsa.RiskScore                    AS RiskSkoru,
    CASE
        WHEN nsa.RiskScore >= 0.80 THEN 'Kritik'
        WHEN nsa.RiskScore >= 0.60 THEN 'Yüksek'
        WHEN nsa.RiskScore >= 0.30 THEN 'Orta'
        ELSE 'Düşük'
    END                              AS RiskSeviyesi
FROM hospital.Appointments a
INNER JOIN hospital.Patients p  ON p.Id = a.PatientId
INNER JOIN hospital.Users up    ON up.Id = p.UserId
INNER JOIN hospital.Doctors d   ON d.Id = a.DoctorId
INNER JOIN hospital.Users ud    ON ud.Id = d.UserId
INNER JOIN hospital.Polyclinics pol ON pol.Id = d.PolyclinicId
LEFT  JOIN hospital.NoShowAnalytics nsa ON nsa.AppointmentId = a.Id
WHERE a.Status = 'Scheduled'
  AND a.AppointmentDate >= CAST(GETUTCDATE() AS DATE)
ORDER BY a.AppointmentDate, a.AppointmentTime;
GO

-- ----------------------------------------------------------------------------
-- T9: Bugünkü randevu listesi (belirli bir doktor için)
-- ----------------------------------------------------------------------------
DECLARE @BugunkiTarih DATE = CAST(GETUTCDATE() AS DATE);
DECLARE @DoktorId INT = 1;

SELECT
    a.SlotOrderInDay                 AS SlotSirasi,
    a.AppointmentTime                AS Saat,
    up.FirstName + ' ' + up.LastName AS HastaAdSoyad,
    DATEDIFF(YEAR, p.DateOfBirth, GETDATE()) AS HastaYasi,
    a.Status                         AS Durum,
    a.IsFirstVisit                   AS IlkZiyaret,
    nsa.RiskScore                    AS RiskSkoru,
    nsa.SmsResponse                  AS SmsYanit
FROM hospital.Appointments a
INNER JOIN hospital.Patients p  ON p.Id = a.PatientId
INNER JOIN hospital.Users up    ON up.Id = p.UserId
LEFT  JOIN hospital.NoShowAnalytics nsa ON nsa.AppointmentId = a.Id
WHERE a.DoctorId = @DoktorId
  AND CAST(a.AppointmentDate AS DATE) = @BugunkiTarih
ORDER BY a.SlotOrderInDay;
GO

-- ----------------------------------------------------------------------------
-- T10: Sigorta türüne göre no-show dağılımı
-- ----------------------------------------------------------------------------
SELECT
    p.InsuranceType                  AS SigortaTuru,
    COUNT(a.Id)                      AS ToplamRandevu,
    SUM(CASE WHEN a.Status = 'NoShow' THEN 1 ELSE 0 END) AS GelmediSayisi,
    CAST(
        SUM(CASE WHEN a.Status = 'NoShow' THEN 1.0 ELSE 0.0 END)
        / NULLIF(COUNT(a.Id), 0) * 100
    AS DECIMAL(5,2))                 AS GelmemeOrani_Pct
FROM hospital.Patients p
INNER JOIN hospital.Appointments a ON a.PatientId = p.Id
GROUP BY p.InsuranceType
ORDER BY GelmemeOrani_Pct DESC;
GO


-- =============================================================================
-- BÖLÜM B: İLERİ SORGULAR (10 adet)
-- =============================================================================

-- ----------------------------------------------------------------------------
-- I1: CTE ile aylık no-show trend analizi (son 6 ay)
-- ----------------------------------------------------------------------------
WITH AylikTrend AS (
    SELECT
        YEAR(a.AppointmentDate)  AS Yil,
        MONTH(a.AppointmentDate) AS Ay,
        COUNT(*)                 AS ToplamRandevu,
        SUM(CASE WHEN a.Status = 'NoShow' THEN 1 ELSE 0 END)    AS GelmediSayisi,
        SUM(CASE WHEN a.Status = 'Completed' THEN 1 ELSE 0 END) AS TamamlananSayisi,
        SUM(CASE WHEN a.Status = 'Cancelled' THEN 1 ELSE 0 END) AS IptalSayisi
    FROM hospital.Appointments a
    WHERE a.AppointmentDate >= DATEADD(MONTH, -6, GETUTCDATE())
    GROUP BY YEAR(a.AppointmentDate), MONTH(a.AppointmentDate)
),
TrendHesap AS (
    SELECT
        Yil, Ay,
        ToplamRandevu,
        GelmediSayisi,
        CAST(GelmediSayisi * 100.0 / NULLIF(ToplamRandevu, 0) AS DECIMAL(5,2)) AS GelmemeOrani_Pct,
        LAG(GelmediSayisi) OVER (ORDER BY Yil, Ay)      AS OncekiAyGelmedi,
        GelmediSayisi - LAG(GelmediSayisi) OVER (ORDER BY Yil, Ay) AS DegisimSayisi
    FROM AylikTrend
)
SELECT
    Yil,
    Ay,
    ToplamRandevu,
    GelmediSayisi,
    GelmemeOrani_Pct,
    ISNULL(DegisimSayisi, 0) AS AylikDegisim,
    CASE
        WHEN DegisimSayisi > 0  THEN 'Artis'
        WHEN DegisimSayisi < 0  THEN 'Azalis'
        WHEN DegisimSayisi = 0  THEN 'Ayni'
        ELSE 'Ilk Ay'
    END                      AS Trend
FROM TrendHesap
ORDER BY Yil, Ay;
GO

-- ----------------------------------------------------------------------------
-- I2: Window function — Her poliklinikteki doktorların ortalama risk skorları
--     ve poliklinik sıralaması
-- ----------------------------------------------------------------------------
SELECT
    pol.Name                         AS Poliklinik,
    ud.FirstName + ' ' + ud.LastName AS DoktorAdSoyad,
    d.Specialty                      AS Uzmanlik,
    COUNT(a.Id)                      AS RandevuSayisi,
    AVG(nsa.RiskScore)               AS OrtalamaRiskSkoru,
    RANK() OVER (
        PARTITION BY pol.Id
        ORDER BY AVG(nsa.RiskScore) DESC
    )                                AS PoliklinikIcinSiralama,
    DENSE_RANK() OVER (
        ORDER BY AVG(nsa.RiskScore) DESC
    )                                AS GenelSiralama,
    AVG(AVG(nsa.RiskScore)) OVER (PARTITION BY pol.Id) AS PoliklinikOrtRisk
FROM hospital.Doctors d
INNER JOIN hospital.Users ud         ON ud.Id = d.UserId
INNER JOIN hospital.Polyclinics pol  ON pol.Id = d.PolyclinicId
INNER JOIN hospital.Appointments a   ON a.DoctorId = d.Id
INNER JOIN hospital.NoShowAnalytics nsa ON nsa.AppointmentId = a.Id
GROUP BY pol.Id, pol.Name, d.Id, d.Specialty, ud.FirstName, ud.LastName
ORDER BY pol.Name, OrtalamaRiskSkoru DESC;
GO

-- ----------------------------------------------------------------------------
-- I3: Subquery — Genel ortalama no-show oranının üzerinde olan hastalar
-- ----------------------------------------------------------------------------
SELECT
    p.Id,
    u.FirstName + ' ' + u.LastName   AS AdSoyad,
    p.TotalAppointments,
    p.NoShowCount,
    CAST(p.NoShowCount * 100.0 / NULLIF(p.TotalAppointments, 0) AS DECIMAL(5,2)) AS BireyselOran_Pct,
    CAST((
        SELECT AVG(CAST(pi.NoShowCount AS FLOAT) / NULLIF(pi.TotalAppointments, 0)) * 100
        FROM hospital.Patients pi
        WHERE pi.TotalAppointments > 0
    ) AS DECIMAL(5,2))               AS GenelOrtalama_Pct
FROM hospital.Patients p
INNER JOIN hospital.Users u ON u.Id = p.UserId
WHERE p.TotalAppointments > 0
  AND (CAST(p.NoShowCount AS FLOAT) / p.TotalAppointments) > (
        SELECT AVG(CAST(pi.NoShowCount AS FLOAT) / NULLIF(pi.TotalAppointments, 0))
        FROM hospital.Patients pi
        WHERE pi.TotalAppointments > 0
  )
ORDER BY (CAST(p.NoShowCount AS FLOAT) / p.TotalAppointments) DESC;
GO

-- ----------------------------------------------------------------------------
-- I4: CASE WHEN — Risk skoruna göre öncelik kategorisi ve aksiyon önerisi
-- ----------------------------------------------------------------------------
SELECT
    a.Id                             AS RandevuId,
    a.AppointmentDate                AS Tarih,
    a.AppointmentTime                AS Saat,
    up.FirstName + ' ' + up.LastName AS Hasta,
    ud.FirstName + ' ' + ud.LastName AS Doktor,
    nsa.RiskScore                    AS RiskSkoru,
    CASE
        WHEN nsa.RiskScore >= 0.80 THEN 'Kritik'
        WHEN nsa.RiskScore >= 0.60 THEN 'Yüksek'
        WHEN nsa.RiskScore >= 0.30 THEN 'Orta'
        ELSE 'Düşük'
    END                              AS RiskSeviyesi,
    CASE
        WHEN nsa.RiskScore >= 0.80 THEN 1
        WHEN nsa.RiskScore >= 0.60 THEN 2
        WHEN nsa.RiskScore >= 0.30 THEN 3
        ELSE 4
    END                              AS OncelikSirasi,
    CASE
        WHEN nsa.RiskScore >= 0.80 THEN 'Telefon ile hatırlatma + alternatif slot hazırla'
        WHEN nsa.RiskScore >= 0.60 THEN 'SMS + 2 gün önce hatırlatma'
        WHEN nsa.RiskScore >= 0.30 THEN 'Standart SMS hatırlatma'
        ELSE 'İşlem gerekmiyor'
    END                              AS Onerilen_Aksiyon,
    CASE
        WHEN nsa.SmsResponse = 'Confirmed' THEN 'Onaylandi'
        WHEN nsa.SmsResponse = 'Cancelled' THEN 'Iptal Etti'
        WHEN nsa.SmsResponse = 'NoResponse' THEN 'Yanit Yok'
        WHEN nsa.SmsResponse = 'Sent' THEN 'Gonderildi'
        ELSE 'SMS Gonderilmedi'
    END                              AS SMS_Durumu
FROM hospital.Appointments a
INNER JOIN hospital.Patients p  ON p.Id = a.PatientId
INNER JOIN hospital.Users up    ON up.Id = p.UserId
INNER JOIN hospital.Doctors d   ON d.Id = a.DoctorId
INNER JOIN hospital.Users ud    ON ud.Id = d.UserId
INNER JOIN hospital.NoShowAnalytics nsa ON nsa.AppointmentId = a.Id
WHERE a.Status = 'Scheduled'
  AND a.AppointmentDate BETWEEN GETUTCDATE() AND DATEADD(DAY, 7, GETUTCDATE())
ORDER BY OncelikSirasi, a.AppointmentDate;
GO

-- ----------------------------------------------------------------------------
-- I5: Multi-level JOIN + Aggregation — Hasta profil özeti
--     (Hasta → Randevu → Risk → Doktor → Poliklinik → Tıbbi Geçmiş)
-- ----------------------------------------------------------------------------
SELECT
    p.Id                             AS HastaId,
    u.FirstName + ' ' + u.LastName   AS AdSoyad,
    DATEDIFF(YEAR, p.DateOfBirth, GETDATE()) AS Yas,
    p.City                           AS Sehir,
    p.InsuranceType                  AS Sigorta,
    p.TotalAppointments              AS ToplamRandevu,
    p.NoShowCount                    AS GelmediSayisi,
    COUNT(DISTINCT mh.Id)            AS TandiSayisi,
    COUNT(DISTINCT med.Id)           AS IlacSayisi,
    COUNT(DISTINCT d.Id)             AS ZiyaretEttigiDoktorSayisi,
    COUNT(DISTINCT pol.Id)           AS ZiyaretEttigiPoliklinikSayisi,
    AVG(nsa.RiskScore)               AS OrtRiskSkoru,
    MAX(nsa.RiskScore)               AS MaksRiskSkoru,
    MIN(a.AppointmentDate)           AS IlkRandevuTarihi,
    MAX(a.AppointmentDate)           AS SonRandevuTarihi
FROM hospital.Patients p
INNER JOIN hospital.Users u ON u.Id = p.UserId
LEFT JOIN hospital.Appointments a      ON a.PatientId = p.Id
LEFT JOIN hospital.NoShowAnalytics nsa ON nsa.AppointmentId = a.Id
LEFT JOIN hospital.Doctors d           ON d.Id = a.DoctorId
LEFT JOIN hospital.Polyclinics pol     ON pol.Id = d.PolyclinicId
LEFT JOIN hospital.MedicalHistories mh ON mh.PatientId = p.Id
LEFT JOIN hospital.Medications med     ON med.MedicalHistoryId = mh.Id
GROUP BY p.Id, u.FirstName, u.LastName, p.DateOfBirth, p.City,
         p.InsuranceType, p.TotalAppointments, p.NoShowCount
ORDER BY OrtRiskSkoru DESC;
GO

-- ----------------------------------------------------------------------------
-- I6: STRING_AGG — Hastanın tüm aktif tanılarını tek satırda listele
-- ----------------------------------------------------------------------------
SELECT
    p.Id                             AS HastaId,
    u.FirstName + ' ' + u.LastName   AS AdSoyad,
    COUNT(mh.Id)                     AS TaniSayisi,
    STRING_AGG(mh.DiagnosisCode + ': ' + mh.DiagnosisName, ' | ')
        WITHIN GROUP (ORDER BY mh.DiagnosisDate)
                                     AS TumTaniler,
    STRING_AGG(med.Name, ', ')
        WITHIN GROUP (ORDER BY med.StartDate)
                                     AS KullanilanIlaclar
FROM hospital.Patients p
INNER JOIN hospital.Users u         ON u.Id = p.UserId
LEFT  JOIN hospital.MedicalHistories mh ON mh.PatientId = p.Id AND mh.IsActive = 1
LEFT  JOIN hospital.Medications med ON med.MedicalHistoryId = mh.Id
GROUP BY p.Id, u.FirstName, u.LastName
ORDER BY p.Id;
GO

-- ----------------------------------------------------------------------------
-- I7: EXISTS — Hiç randevusu bulunmayan hastalar
-- ----------------------------------------------------------------------------
SELECT
    p.Id,
    u.FirstName + ' ' + u.LastName   AS AdSoyad,
    u.Email,
    p.City                           AS Sehir,
    p.CreatedAt                      AS KayitTarihi,
    DATEDIFF(DAY, p.CreatedAt, GETUTCDATE()) AS KayitliGunSayisi
FROM hospital.Patients p
INNER JOIN hospital.Users u ON u.Id = p.UserId
WHERE NOT EXISTS (
    SELECT 1
    FROM hospital.Appointments a
    WHERE a.PatientId = p.Id
)
ORDER BY p.CreatedAt;
GO

-- ----------------------------------------------------------------------------
-- I8: ROW_NUMBER() — Her doktorun en yüksek riskli top-3 randevusu
-- ----------------------------------------------------------------------------
WITH SiralanmisRandevular AS (
    SELECT
        d.Id                             AS DoktorId,
        ud.FirstName + ' ' + ud.LastName AS DoktorAdSoyad,
        d.Specialty                      AS Uzmanlik,
        a.Id                             AS RandevuId,
        a.AppointmentDate                AS Tarih,
        up.FirstName + ' ' + up.LastName AS HastaAdSoyad,
        nsa.RiskScore                    AS RiskSkoru,
        ROW_NUMBER() OVER (
            PARTITION BY d.Id
            ORDER BY nsa.RiskScore DESC
        )                                AS SiraNo
    FROM hospital.Doctors d
    INNER JOIN hospital.Users ud         ON ud.Id = d.UserId
    INNER JOIN hospital.Appointments a   ON a.DoctorId = d.Id
    INNER JOIN hospital.Patients p       ON p.Id = a.PatientId
    INNER JOIN hospital.Users up         ON up.Id = p.UserId
    INNER JOIN hospital.NoShowAnalytics nsa ON nsa.AppointmentId = a.Id
    WHERE a.Status = 'Scheduled'
)
SELECT
    DoktorId,
    DoktorAdSoyad,
    Uzmanlik,
    SiraNo          AS DoktorIcinSira,
    RandevuId,
    Tarih,
    HastaAdSoyad,
    RiskSkoru,
    CASE
        WHEN RiskSkoru >= 0.80 THEN 'Kritik'
        WHEN RiskSkoru >= 0.60 THEN 'Yüksek'
        WHEN RiskSkoru >= 0.30 THEN 'Orta'
        ELSE 'Düşük'
    END AS RiskSeviyesi
FROM SiralanmisRandevular
WHERE SiraNo <= 3
ORDER BY DoktorId, SiraNo;
GO

-- ----------------------------------------------------------------------------
-- I9: DATEDIFF + Aggregation — Randevu oluşturma ile iptal arasındaki
--     ortalama süre (kaç gün önceden iptal edildi?)
-- ----------------------------------------------------------------------------
SELECT
    pol.Name                         AS Poliklinik,
    COUNT(a.Id)                      AS IptalSayisi,
    AVG(DATEDIFF(DAY, a.CreatedAt, a.UpdatedAt))       AS OrtIptalSuresi_Gun,
    MIN(DATEDIFF(DAY, a.CreatedAt, a.UpdatedAt))       AS MinIptalSuresi_Gun,
    MAX(DATEDIFF(DAY, a.CreatedAt, a.UpdatedAt))       AS MaksIptalSuresi_Gun,
    AVG(DATEDIFF(DAY, CAST(a.UpdatedAt AS DATE), CAST(a.AppointmentDate AS DATE)))
                                     AS IptaldenRandevuyaKalanGun
FROM hospital.Appointments a
INNER JOIN hospital.Doctors d        ON d.Id = a.DoctorId
INNER JOIN hospital.Polyclinics pol  ON pol.Id = d.PolyclinicId
WHERE a.Status = 'Cancelled'
  AND a.UpdatedAt IS NOT NULL
GROUP BY pol.Id, pol.Name
ORDER BY OrtIptalSuresi_Gun DESC;
GO

-- ----------------------------------------------------------------------------
-- I10: Hava durumu ve SMS yanıtının no-show oranına etkisi
--      (Çoklu GROUP BY + aggregation)
-- ----------------------------------------------------------------------------
SELECT
    nsa.WeatherCondition             AS HavaDurumu,
    nsa.SmsResponse                  AS SmsYaniti,
    COUNT(*)                         AS RandevuSayisi,
    SUM(CASE WHEN a.Status = 'NoShow' THEN 1 ELSE 0 END)    AS GelmediSayisi,
    CAST(
        SUM(CASE WHEN a.Status = 'NoShow' THEN 1.0 ELSE 0.0 END)
        / NULLIF(COUNT(*), 0) * 100
    AS DECIMAL(5,2))                 AS GelmemeOrani_Pct,
    AVG(nsa.RiskScore)               AS OrtRiskSkoru
FROM hospital.NoShowAnalytics nsa
INNER JOIN hospital.Appointments a ON a.Id = nsa.AppointmentId
GROUP BY nsa.WeatherCondition, nsa.SmsResponse
HAVING COUNT(*) >= 5  -- En az 5 randevu olan kombinasyonlar
ORDER BY GelmemeOrani_Pct DESC;
GO
