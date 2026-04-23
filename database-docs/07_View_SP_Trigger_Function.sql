-- =============================================================================
-- HASTANE RANDEVU NO-SHOW TAHMİN SİSTEMİ
-- Veritabanı Nesneleri: View (3) + Stored Procedure (3) + Trigger (2) + Function (2)
-- =============================================================================

USE HospitalNoShowDb_Dev;
GO

-- =============================================================================
-- BÖLÜM 1: VIEW (GÖRÜNÜMLER)
-- =============================================================================

-- ----------------------------------------------------------------------------
-- VIEW 1: vw_AppointmentDetails
-- Amaç: Randevu + Hasta + Doktor + Poliklinik + Risk bilgilerini birleştiren
--        hazır görünüm (en sık kullanılan birleşim)
-- ----------------------------------------------------------------------------
IF OBJECT_ID('hospital.vw_AppointmentDetails', 'V') IS NOT NULL
    DROP VIEW hospital.vw_AppointmentDetails;
GO

CREATE VIEW hospital.vw_AppointmentDetails
AS
SELECT
    a.Id                                    AS RandevuId,
    a.AppointmentDate                       AS Tarih,
    a.AppointmentTime                       AS Saat,
    a.Status                                AS Durum,
    a.IsFirstVisit                          AS IlkZiyaret,
    a.SlotOrderInDay                        AS GunlukSlotSirasi,
    a.TotalSlotsInDay                       AS GunlukToplamSlot,
    a.Notes                                 AS Notlar,
    a.CancellationReason                    AS IptalNedeni,
    a.CreatedAt                             AS OlusturulmaTarihi,

    -- Hasta bilgileri
    p.Id                                    AS HastaId,
    up.FirstName + ' ' + up.LastName        AS HastaAdSoyad,
    up.Email                                AS HastaEmail,
    p.IdentityNumber                        AS TCKN,
    DATEDIFF(YEAR, p.DateOfBirth, GETDATE()) AS HastaYasi,
    p.Gender                                AS Cinsiyet,
    p.City                                  AS Sehir,
    p.InsuranceType                         AS SigortaTuru,
    p.DistanceToHospitalKm                  AS UzaklikKm,
    p.TotalAppointments                     AS ToplamRandevu,
    p.NoShowCount                           AS GelmediSayisi,
    CAST(p.NoShowCount * 100.0 / NULLIF(p.TotalAppointments, 0)
        AS DECIMAL(5,2))                    AS GelmemeOrani_Pct,

    -- Doktor bilgileri
    d.Id                                    AS DoktorId,
    ud.FirstName + ' ' + ud.LastName        AS DoktorAdSoyad,
    d.Title                                 AS Unvan,
    d.Specialty                             AS Uzmanlik,

    -- Poliklinik bilgileri
    pol.Id                                  AS PoliklinikId,
    pol.Name                                AS PoliklinikAdi,
    pol.Department                          AS Bolum,
    pol.Floor                               AS Kat,
    pol.RoomNumber                          AS OdaNo,

    -- Risk analizi
    nsa.RiskScore                           AS RiskSkoru,
    CASE
        WHEN nsa.RiskScore >= 0.80 THEN 'Kritik'
        WHEN nsa.RiskScore >= 0.60 THEN 'Yüksek'
        WHEN nsa.RiskScore >= 0.30 THEN 'Orta'
        WHEN nsa.RiskScore IS NOT NULL THEN 'Düşük'
        ELSE 'Hesaplanmadı'
    END                                     AS RiskSeviyesi,
    nsa.WeatherCondition                    AS HavaDurumu,
    nsa.SmsResponse                         AS SmsYaniti,
    nsa.IsReminderSent                      AS HatirlatmaGonderildi,
    nsa.CalculatedAt                        AS RiskHesaplamaTarihi

FROM hospital.Appointments a
INNER JOIN hospital.Patients p       ON p.Id = a.PatientId
INNER JOIN hospital.Users up         ON up.Id = p.UserId
INNER JOIN hospital.Doctors d        ON d.Id = a.DoctorId
INNER JOIN hospital.Users ud         ON ud.Id = d.UserId
INNER JOIN hospital.Polyclinics pol  ON pol.Id = d.PolyclinicId
LEFT  JOIN hospital.NoShowAnalytics nsa ON nsa.AppointmentId = a.Id;
GO

-- Kullanım örneği:
-- SELECT * FROM hospital.vw_AppointmentDetails WHERE Durum = 'Scheduled';
-- SELECT * FROM hospital.vw_AppointmentDetails WHERE HastaId = 3;

-- ----------------------------------------------------------------------------
-- VIEW 2: vw_PatientRiskSummary
-- Amaç: Her hasta için toplam risk metrikleri ve istatistik özeti
-- ----------------------------------------------------------------------------
IF OBJECT_ID('hospital.vw_PatientRiskSummary', 'V') IS NOT NULL
    DROP VIEW hospital.vw_PatientRiskSummary;
GO

CREATE VIEW hospital.vw_PatientRiskSummary
AS
SELECT
    p.Id                                    AS HastaId,
    u.FirstName + ' ' + u.LastName         AS AdSoyad,
    u.Email,
    DATEDIFF(YEAR, p.DateOfBirth, GETDATE()) AS Yas,
    p.City                                  AS Sehir,
    p.InsuranceType                         AS SigortaTuru,
    p.DistanceToHospitalKm                  AS UzaklikKm,
    p.HasChronicDisease                     AS KronikHastalik,
    p.TotalAppointments                     AS ToplamRandevu,
    p.NoShowCount                           AS GelmediSayisi,
    CAST(p.NoShowCount * 100.0 / NULLIF(p.TotalAppointments, 0)
        AS DECIMAL(5,2))                    AS GelmemeOrani_Pct,
    -- Risk istatistikleri (randevu bazlı)
    COUNT(a.Id)                             AS RandevuSayisi,
    AVG(nsa.RiskScore)                      AS OrtRiskSkoru,
    MAX(nsa.RiskScore)                      AS MaksRiskSkoru,
    MIN(nsa.RiskScore)                      AS MinRiskSkoru,
    -- Son risk seviyesi (en son randevuya göre)
    CASE
        WHEN MAX(nsa.RiskScore) >= 0.80 THEN 'Kritik'
        WHEN MAX(nsa.RiskScore) >= 0.60 THEN 'Yüksek'
        WHEN MAX(nsa.RiskScore) >= 0.30 THEN 'Orta'
        WHEN MAX(nsa.RiskScore) IS NOT NULL THEN 'Düşük'
        ELSE 'Veri Yok'
    END                                     AS EnYuksekRiskSeviyesi,
    -- Zamansal bilgiler
    MIN(a.AppointmentDate)                  AS IlkRandevuTarihi,
    MAX(a.AppointmentDate)                  AS SonRandevuTarihi,
    SUM(CASE WHEN a.Status = 'Completed' THEN 1 ELSE 0 END)  AS TamamlananSayisi,
    SUM(CASE WHEN a.Status = 'NoShow'    THEN 1 ELSE 0 END)  AS GelmediSayisiDogrulama,
    SUM(CASE WHEN a.Status = 'Cancelled' THEN 1 ELSE 0 END)  AS IptalSayisi
FROM hospital.Patients p
INNER JOIN hospital.Users u          ON u.Id = p.UserId
LEFT  JOIN hospital.Appointments a   ON a.PatientId = p.Id
LEFT  JOIN hospital.NoShowAnalytics nsa ON nsa.AppointmentId = a.Id
GROUP BY p.Id, u.FirstName, u.LastName, u.Email, p.DateOfBirth,
         p.City, p.InsuranceType, p.DistanceToHospitalKm, p.HasChronicDisease,
         p.TotalAppointments, p.NoShowCount;
GO

-- ----------------------------------------------------------------------------
-- VIEW 3: vw_DailyAppointmentStats
-- Amaç: Her gün için randevu özet istatistikleri (dashboard / raporlama)
-- ----------------------------------------------------------------------------
IF OBJECT_ID('hospital.vw_DailyAppointmentStats', 'V') IS NOT NULL
    DROP VIEW hospital.vw_DailyAppointmentStats;
GO

CREATE VIEW hospital.vw_DailyAppointmentStats
AS
SELECT
    CAST(a.AppointmentDate AS DATE)         AS Gun,
    DATENAME(WEEKDAY, a.AppointmentDate)    AS HaftaninGunu,
    d.PolyclinicId,
    pol.Name                                AS Poliklinik,
    COUNT(a.Id)                             AS ToplamRandevu,
    SUM(CASE WHEN a.Status = 'Scheduled'  THEN 1 ELSE 0 END)  AS Planlanan,
    SUM(CASE WHEN a.Status = 'Completed'  THEN 1 ELSE 0 END)  AS Tamamlanan,
    SUM(CASE WHEN a.Status = 'NoShow'     THEN 1 ELSE 0 END)  AS Gelmedi,
    SUM(CASE WHEN a.Status = 'Cancelled'  THEN 1 ELSE 0 END)  AS Iptal,
    CAST(
        SUM(CASE WHEN a.Status = 'NoShow' THEN 1.0 ELSE 0.0 END)
        / NULLIF(COUNT(a.Id), 0) * 100
    AS DECIMAL(5,2))                        AS GunlukGelmemeOrani_Pct,
    AVG(nsa.RiskScore)                      AS OrtRiskSkoru,
    SUM(CASE WHEN a.IsFirstVisit = 1  THEN 1 ELSE 0 END)      AS IlkZiyaretSayisi
FROM hospital.Appointments a
INNER JOIN hospital.Doctors d        ON d.Id = a.DoctorId
INNER JOIN hospital.Polyclinics pol  ON pol.Id = d.PolyclinicId
LEFT  JOIN hospital.NoShowAnalytics nsa ON nsa.AppointmentId = a.Id
GROUP BY CAST(a.AppointmentDate AS DATE),
         DATENAME(WEEKDAY, a.AppointmentDate),
         d.PolyclinicId, pol.Name;
GO


-- =============================================================================
-- BÖLÜM 2: STORED PROCEDURES (SAKLANAN YORDAMLAR)
-- =============================================================================

-- ----------------------------------------------------------------------------
-- SP 1: sp_GetDoctorDailySchedule
-- Amaç: Belirli bir doktorun belirli bir gündeki randevu listesini döndür
-- Parametreler: @DoctorId INT, @TargetDate DATE
-- ----------------------------------------------------------------------------
IF OBJECT_ID('hospital.sp_GetDoctorDailySchedule', 'P') IS NOT NULL
    DROP PROCEDURE hospital.sp_GetDoctorDailySchedule;
GO

CREATE PROCEDURE hospital.sp_GetDoctorDailySchedule
    @DoctorId   INT,
    @TargetDate DATE = NULL   -- NULL ise bugün
AS
BEGIN
    SET NOCOUNT ON;

    -- Default: bugün
    IF @TargetDate IS NULL
        SET @TargetDate = CAST(GETUTCDATE() AS DATE);

    -- Geçersiz doktor kontrolü
    IF NOT EXISTS (SELECT 1 FROM hospital.Doctors WHERE Id = @DoctorId)
    BEGIN
        RAISERROR('Doktor bulunamadı. DoctorId: %d', 16, 1, @DoctorId);
        RETURN;
    END

    -- Randevu listesi
    SELECT
        a.Id                                AS RandevuId,
        a.SlotOrderInDay                    AS Slot,
        a.AppointmentTime                   AS Saat,
        up.FirstName + ' ' + up.LastName    AS HastaAdSoyad,
        DATEDIFF(YEAR, p.DateOfBirth, @TargetDate) AS HastaYasi,
        p.InsuranceType                     AS Sigorta,
        a.Status                            AS Durum,
        a.IsFirstVisit                      AS IlkZiyaret,
        a.Notes                             AS Notlar,
        nsa.RiskScore                       AS RiskSkoru,
        CASE
            WHEN nsa.RiskScore >= 0.80 THEN 'Kritik'
            WHEN nsa.RiskScore >= 0.60 THEN 'Yüksek'
            WHEN nsa.RiskScore >= 0.30 THEN 'Orta'
            WHEN nsa.RiskScore IS NOT NULL THEN 'Düşük'
            ELSE '-'
        END                                 AS RiskSeviyesi,
        nsa.SmsResponse                     AS SmsYaniti
    FROM hospital.Appointments a
    INNER JOIN hospital.Patients p  ON p.Id = a.PatientId
    INNER JOIN hospital.Users up    ON up.Id = p.UserId
    LEFT  JOIN hospital.NoShowAnalytics nsa ON nsa.AppointmentId = a.Id
    WHERE a.DoctorId = @DoctorId
      AND CAST(a.AppointmentDate AS DATE) = @TargetDate
    ORDER BY a.SlotOrderInDay, a.AppointmentTime;

    -- Günlük özet (ikinci resultset)
    SELECT
        COUNT(*)                            AS ToplamRandevu,
        SUM(CASE WHEN a.Status = 'Completed' THEN 1 ELSE 0 END)  AS Tamamlanan,
        SUM(CASE WHEN a.Status = 'NoShow'    THEN 1 ELSE 0 END)  AS Gelmedi,
        SUM(CASE WHEN a.Status = 'Cancelled' THEN 1 ELSE 0 END)  AS Iptal,
        SUM(CASE WHEN a.Status = 'Scheduled' THEN 1 ELSE 0 END)  AS Planlanan,
        SUM(CASE WHEN nsa.RiskScore >= 0.60  THEN 1 ELSE 0 END)  AS YuksekRiskliSayisi
    FROM hospital.Appointments a
    LEFT JOIN hospital.NoShowAnalytics nsa ON nsa.AppointmentId = a.Id
    WHERE a.DoctorId = @DoctorId
      AND CAST(a.AppointmentDate AS DATE) = @TargetDate;
END
GO

-- Kullanım:
-- EXEC hospital.sp_GetDoctorDailySchedule @DoctorId = 1, @TargetDate = '2026-03-15';
-- EXEC hospital.sp_GetDoctorDailySchedule @DoctorId = 1;  -- bugün

-- ----------------------------------------------------------------------------
-- SP 2: sp_UpdateAppointmentStatus
-- Amaç: Randevu durumunu güncelle + audit log'a kaydet
-- Parametreler: @AppointmentId, @NewStatus, @CancellationReason, @ChangedBy
-- ----------------------------------------------------------------------------
IF OBJECT_ID('hospital.sp_UpdateAppointmentStatus', 'P') IS NOT NULL
    DROP PROCEDURE hospital.sp_UpdateAppointmentStatus;
GO

CREATE PROCEDURE hospital.sp_UpdateAppointmentStatus
    @AppointmentId      INT,
    @NewStatus          NVARCHAR(30),
    @CancellationReason NVARCHAR(500) = NULL,
    @ChangedBy          NVARCHAR(256) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Parametrik validasyon
        IF @NewStatus NOT IN ('Scheduled','Completed','NoShow','Cancelled','Rescheduled')
        BEGIN
            RAISERROR('Geçersiz status değeri: %s', 16, 1, @NewStatus);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @NewStatus = 'Cancelled' AND (@CancellationReason IS NULL OR LEN(LTRIM(@CancellationReason)) = 0)
        BEGIN
            RAISERROR('İptal için gerekçe girilmesi zorunludur.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Mevcut değerleri al
        DECLARE @OldStatus NVARCHAR(30);
        DECLARE @PatientId INT;

        SELECT @OldStatus = Status, @PatientId = PatientId
        FROM hospital.Appointments
        WHERE Id = @AppointmentId;

        IF @OldStatus IS NULL
        BEGIN
            RAISERROR('Randevu bulunamadı. AppointmentId: %d', 16, 1, @AppointmentId);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Aynı değerse güncelleme yapma
        IF @OldStatus = @NewStatus
        BEGIN
            PRINT 'Durum zaten ' + @NewStatus + ', değişiklik yapılmadı.';
            COMMIT TRANSACTION;
            RETURN;
        END

        -- Randevuyu güncelle
        UPDATE hospital.Appointments
        SET
            Status              = @NewStatus,
            CancellationReason  = CASE WHEN @NewStatus = 'Cancelled' THEN @CancellationReason ELSE CancellationReason END,
            UpdatedAt           = GETUTCDATE()
        WHERE Id = @AppointmentId;

        -- Hasta istatistiklerini güncelle
        IF @NewStatus = 'NoShow'
        BEGIN
            UPDATE hospital.Patients
            SET NoShowCount = NoShowCount + 1, UpdatedAt = GETUTCDATE()
            WHERE Id = @PatientId;
        END
        ELSE IF @OldStatus = 'NoShow' AND @NewStatus != 'NoShow'
        BEGIN
            -- NoShow'dan geri alınırsa sayacı azalt
            UPDATE hospital.Patients
            SET NoShowCount = CASE WHEN NoShowCount > 0 THEN NoShowCount - 1 ELSE 0 END,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @PatientId;
        END

        -- Audit log'a kaydet
        INSERT INTO hospital.AuditLog
            (TableName, RecordId, ColumnName, OldValue, NewValue, ChangedBy, ChangedAt, ChangeType)
        VALUES
            ('Appointments', @AppointmentId, 'Status', @OldStatus, @NewStatus,
             ISNULL(@ChangedBy, SYSTEM_USER), GETUTCDATE(), 'UPDATE');

        COMMIT TRANSACTION;
        PRINT 'Randevu #' + CAST(@AppointmentId AS NVARCHAR) + ' durumu ' + @OldStatus + ' → ' + @NewStatus + ' olarak güncellendi.';

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrMsg NVARCHAR(2048) = ERROR_MESSAGE();
        RAISERROR(@ErrMsg, 16, 1);
    END CATCH
END
GO

-- Kullanım:
-- EXEC hospital.sp_UpdateAppointmentStatus @AppointmentId=3, @NewStatus='Completed', @ChangedBy='dr.ahmet';
-- EXEC hospital.sp_UpdateAppointmentStatus @AppointmentId=9, @NewStatus='Cancelled', @CancellationReason='Hasta seyahatte';

-- ----------------------------------------------------------------------------
-- SP 3: sp_GetHighRiskPatients
-- Amaç: Filtreli yüksek risk hasta listesi döndür (Admin/Doktor paneli için)
-- ----------------------------------------------------------------------------
IF OBJECT_ID('hospital.sp_GetHighRiskPatients', 'P') IS NOT NULL
    DROP PROCEDURE hospital.sp_GetHighRiskPatients;
GO

CREATE PROCEDURE hospital.sp_GetHighRiskPatients
    @MinNoShowRate          FLOAT   = 0.40,   -- Min no-show oranı (0.0-1.0)
    @MinAppointmentCount    INT     = 3,       -- Min randevu sayısı (istatistik güvenilirliği)
    @TopN                   INT     = 50       -- Maks kaç kayıt dönsün
AS
BEGIN
    SET NOCOUNT ON;

    -- Parametrik validasyon
    IF @MinNoShowRate < 0 OR @MinNoShowRate > 1
    BEGIN
        RAISERROR('MinNoShowRate 0.0 ile 1.0 arasında olmalıdır.', 16, 1);
        RETURN;
    END

    SELECT TOP (@TopN)
        p.Id                                AS HastaId,
        u.FirstName + ' ' + u.LastName     AS AdSoyad,
        u.Email,
        u.PhoneNumber,
        DATEDIFF(YEAR, p.DateOfBirth, GETDATE()) AS Yas,
        p.City,
        p.InsuranceType,
        p.DistanceToHospitalKm,
        p.HasChronicDisease,
        p.TotalAppointments,
        p.NoShowCount,
        CAST(p.NoShowCount * 100.0 / p.TotalAppointments AS DECIMAL(5,2)) AS GelmemeOrani_Pct,
        -- Önümüzdeki planlı randevu
        (SELECT TOP 1 CAST(a2.AppointmentDate AS DATE)
         FROM hospital.Appointments a2
         WHERE a2.PatientId = p.Id AND a2.Status = 'Scheduled'
           AND a2.AppointmentDate > GETUTCDATE()
         ORDER BY a2.AppointmentDate) AS SonrakiRandevuTarihi,
        -- Ortalama risk skoru (tüm randevuları için)
        (SELECT AVG(nsa2.RiskScore)
         FROM hospital.Appointments a2
         INNER JOIN hospital.NoShowAnalytics nsa2 ON nsa2.AppointmentId = a2.Id
         WHERE a2.PatientId = p.Id) AS OrtRiskSkoru
    FROM hospital.Patients p
    INNER JOIN hospital.Users u ON u.Id = p.UserId
    WHERE p.TotalAppointments >= @MinAppointmentCount
      AND (CAST(p.NoShowCount AS FLOAT) / p.TotalAppointments) >= @MinNoShowRate
      AND u.IsActive = 1
    ORDER BY (CAST(p.NoShowCount AS FLOAT) / p.TotalAppointments) DESC;
END
GO

-- Kullanım:
-- EXEC hospital.sp_GetHighRiskPatients;                          -- default: >%40, min 3 randevu
-- EXEC hospital.sp_GetHighRiskPatients @MinNoShowRate=0.6, @MinAppointmentCount=5, @TopN=20;


-- =============================================================================
-- BÖLÜM 3: TRIGGER'LAR
-- =============================================================================

-- ----------------------------------------------------------------------------
-- TRIGGER 1: tr_Appointments_UpdatePatientStats
-- Amaç: Appointments tablosunda INSERT veya UPDATE olduğunda
--        Patient tablosundaki TotalAppointments ve NoShowCount'u otomatik güncelle
-- ----------------------------------------------------------------------------
IF OBJECT_ID('hospital.tr_Appointments_UpdatePatientStats', 'TR') IS NOT NULL
    DROP TRIGGER hospital.tr_Appointments_UpdatePatientStats;
GO

CREATE TRIGGER hospital.tr_Appointments_UpdatePatientStats
ON hospital.Appointments
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- INSERT: TotalAppointments arttır
    IF EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted)
    BEGIN
        -- Yeni randevular için TotalAppointments sayacını güncelle
        UPDATE hospital.Patients
        SET
            TotalAppointments = TotalAppointments + ins_count.Adet,
            UpdatedAt = GETUTCDATE()
        FROM hospital.Patients p
        INNER JOIN (
            SELECT PatientId, COUNT(*) AS Adet
            FROM inserted
            GROUP BY PatientId
        ) AS ins_count ON ins_count.PatientId = p.Id;

        -- INSERT ile birlikte zaten NoShow olarak eklendiyse sayacı da güncelle
        UPDATE hospital.Patients
        SET
            NoShowCount = NoShowCount + ns_count.Adet,
            UpdatedAt = GETUTCDATE()
        FROM hospital.Patients p
        INNER JOIN (
            SELECT PatientId, COUNT(*) AS Adet
            FROM inserted
            WHERE Status = 'NoShow'
            GROUP BY PatientId
        ) AS ns_count ON ns_count.PatientId = p.Id;
    END

    -- UPDATE: Status değişikliklerinde NoShowCount güncelle
    IF EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
    BEGIN
        -- Yeni NoShow olan randevular (+1)
        UPDATE hospital.Patients
        SET NoShowCount = NoShowCount + ns_new.Adet, UpdatedAt = GETUTCDATE()
        FROM hospital.Patients p
        INNER JOIN (
            SELECT i.PatientId, COUNT(*) AS Adet
            FROM inserted i
            INNER JOIN deleted d ON d.Id = i.Id
            WHERE i.Status = 'NoShow' AND d.Status <> 'NoShow'
            GROUP BY i.PatientId
        ) AS ns_new ON ns_new.PatientId = p.Id;

        -- NoShow'dan çıkarılan randevular (-1)
        UPDATE hospital.Patients
        SET NoShowCount = CASE WHEN NoShowCount > 0 THEN NoShowCount - 1 ELSE 0 END,
            UpdatedAt = GETUTCDATE()
        FROM hospital.Patients p
        INNER JOIN (
            SELECT i.PatientId, COUNT(*) AS Adet
            FROM inserted i
            INNER JOIN deleted d ON d.Id = i.Id
            WHERE d.Status = 'NoShow' AND i.Status <> 'NoShow'
            GROUP BY i.PatientId
        ) AS ns_rem ON ns_rem.PatientId = p.Id;
    END
END
GO

-- ----------------------------------------------------------------------------
-- TRIGGER 2: tr_NoShowAnalytics_AuditLog
-- Amaç: NoShowAnalytics tablosunda RiskScore güncellendiğinde
--        AuditLog tablosuna otomatik kayıt ekle
-- ----------------------------------------------------------------------------
IF OBJECT_ID('hospital.tr_NoShowAnalytics_AuditLog', 'TR') IS NOT NULL
    DROP TRIGGER hospital.tr_NoShowAnalytics_AuditLog;
GO

CREATE TRIGGER hospital.tr_NoShowAnalytics_AuditLog
ON hospital.NoShowAnalytics
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Yalnızca RiskScore değişmişse log'a kaydet
    INSERT INTO hospital.AuditLog
        (TableName, RecordId, ColumnName, OldValue, NewValue, ChangedBy, ChangedAt, ChangeType)
    SELECT
        'NoShowAnalytics',
        i.Id,
        'RiskScore',
        CAST(d.RiskScore AS NVARCHAR(20)),
        CAST(i.RiskScore AS NVARCHAR(20)),
        SYSTEM_USER,
        GETUTCDATE(),
        'UPDATE'
    FROM inserted i
    INNER JOIN deleted d ON d.Id = i.Id
    WHERE i.RiskScore <> d.RiskScore;

    -- SMS yanıtı değişmişse de log'a kaydet
    INSERT INTO hospital.AuditLog
        (TableName, RecordId, ColumnName, OldValue, NewValue, ChangedBy, ChangedAt, ChangeType)
    SELECT
        'NoShowAnalytics',
        i.Id,
        'SmsResponse',
        d.SmsResponse,
        i.SmsResponse,
        SYSTEM_USER,
        GETUTCDATE(),
        'UPDATE'
    FROM inserted i
    INNER JOIN deleted d ON d.Id = i.Id
    WHERE i.SmsResponse <> d.SmsResponse;
END
GO


-- =============================================================================
-- BÖLÜM 4: FONKSİYONLAR
-- =============================================================================

-- ----------------------------------------------------------------------------
-- FUNCTION 1: fn_GetRiskLevel (Scalar-valued)
-- Amaç: RiskScore değerini (0.0-1.0) metin risk seviyesine çevir
-- Parametreler: @RiskScore DECIMAL(5,4)
-- Döndürür: NVARCHAR(10) → 'Düşük' | 'Orta' | 'Yüksek' | 'Kritik'
-- ----------------------------------------------------------------------------
IF OBJECT_ID('hospital.fn_GetRiskLevel', 'FN') IS NOT NULL
    DROP FUNCTION hospital.fn_GetRiskLevel;
GO

CREATE FUNCTION hospital.fn_GetRiskLevel
(
    @RiskScore DECIMAL(5,4)
)
RETURNS NVARCHAR(10)
AS
BEGIN
    RETURN
        CASE
            WHEN @RiskScore IS NULL     THEN 'Bilinmiyor'
            WHEN @RiskScore >= 0.80     THEN 'Kritik'
            WHEN @RiskScore >= 0.60     THEN 'Yüksek'
            WHEN @RiskScore >= 0.30     THEN 'Orta'
            ELSE                             'Düşük'
        END;
END
GO

-- Kullanım:
-- SELECT Id, RiskScore, hospital.fn_GetRiskLevel(RiskScore) AS RiskSeviyesi
-- FROM hospital.NoShowAnalytics;

-- ----------------------------------------------------------------------------
-- FUNCTION 2: fn_GetPatientAppointmentHistory (Table-valued)
-- Amaç: Verilen hasta için tüm randevu özetini tablo olarak döndür
-- Parametreler: @PatientId INT
-- Döndürür: Randevu özet tablosu
-- ----------------------------------------------------------------------------
IF OBJECT_ID('hospital.fn_GetPatientAppointmentHistory', 'TF') IS NOT NULL
    DROP FUNCTION hospital.fn_GetPatientAppointmentHistory;
GO

CREATE FUNCTION hospital.fn_GetPatientAppointmentHistory
(
    @PatientId INT
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        a.Id                                    AS RandevuId,
        CAST(a.AppointmentDate AS DATE)         AS Tarih,
        a.AppointmentTime                       AS Saat,
        a.Status                                AS Durum,
        ud.FirstName + ' ' + ud.LastName        AS DoktorAdSoyad,
        d.Specialty                             AS Uzmanlik,
        pol.Name                                AS Poliklinik,
        a.IsFirstVisit                          AS IlkZiyaret,
        nsa.RiskScore                           AS RiskSkoru,
        hospital.fn_GetRiskLevel(nsa.RiskScore) AS RiskSeviyesi,
        nsa.SmsResponse                         AS SmsYaniti,
        a.Notes                                 AS Notlar,
        a.CancellationReason                    AS IptalNedeni
    FROM hospital.Appointments a
    INNER JOIN hospital.Doctors d        ON d.Id = a.DoctorId
    INNER JOIN hospital.Users ud         ON ud.Id = d.UserId
    INNER JOIN hospital.Polyclinics pol  ON pol.Id = d.PolyclinicId
    LEFT  JOIN hospital.NoShowAnalytics nsa ON nsa.AppointmentId = a.Id
    WHERE a.PatientId = @PatientId
);
GO

-- Kullanım:
-- SELECT * FROM hospital.fn_GetPatientAppointmentHistory(3) ORDER BY Tarih DESC;
-- SELECT * FROM hospital.fn_GetPatientAppointmentHistory(3) WHERE Durum = 'NoShow';


-- =============================================================================
-- DOĞRULAMA: Tüm nesnelerin oluşturulduğunu kontrol et
-- =============================================================================
SELECT
    o.type_desc AS NesneTipi,
    s.name      AS Schema,
    o.name      AS NesneAdi,
    o.create_date AS OlusturmaTarihi
FROM sys.objects o
INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE s.name = 'hospital'
  AND o.type IN ('V','P','TR','FN','TF')  -- View, Procedure, Trigger, Function
ORDER BY o.type_desc, o.name;
GO
-- Beklenen:
-- FN  → fn_GetRiskLevel
-- P   → sp_GetDoctorDailySchedule, sp_GetHighRiskPatients, sp_UpdateAppointmentStatus
-- TF  → fn_GetPatientAppointmentHistory
-- TR  → tr_Appointments_UpdatePatientStats, tr_NoShowAnalytics_AuditLog
-- V   → vw_AppointmentDetails, vw_DailyAppointmentStats, vw_PatientRiskSummary
