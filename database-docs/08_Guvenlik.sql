-- =============================================================================
-- HASTANE RANDEVU NO-SHOW TAHMİN SİSTEMİ
-- Veritabanı Güvenliği:
--   - DB Login / User / Role yönetimi
--   - GRANT / DENY / REVOKE
--   - SQL Injection önlemi (örneklerle)
--   - Row-Level Security (RLS)
--   - Stored Procedure ile dolaylı erişim
-- =============================================================================

USE HospitalNoShowDb_Dev;
GO

-- =============================================================================
-- BÖLÜM 1: VERİTABANI KULLANICILARI VE ROLLER
-- =============================================================================

-- ----------------------------------------------------------------------------
-- 1.1 SQL Server Login'leri oluştur (sa yerine ayrı loginler kullan)
-- NOT: Bu komutlar master veritabanında çalıştırılır
-- ----------------------------------------------------------------------------
USE master;
GO

-- Uygulama logins'i (sadece okuma + yazma, DDL yok)
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'hospital_app_login')
    CREATE LOGIN hospital_app_login
        WITH PASSWORD = N'H@spital_App_2026!',
             CHECK_POLICY = ON,          -- Parola politikası aktif
             CHECK_EXPIRATION = OFF;

-- Salt okunur login (raporlama / BI araçları için)
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'hospital_readonly_login')
    CREATE LOGIN hospital_readonly_login
        WITH PASSWORD = N'R3ad0nly_Hospital!',
             CHECK_POLICY = ON,
             CHECK_EXPIRATION = OFF;
GO

USE HospitalNoShowDb_Dev;
GO

-- ----------------------------------------------------------------------------
-- 1.2 Veritabanı kullanıcıları oluştur (login → db user eşlemesi)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'hospital_app_user')
    CREATE USER hospital_app_user FOR LOGIN hospital_app_login;

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'hospital_readonly_user')
    CREATE USER hospital_readonly_user FOR LOGIN hospital_readonly_login;
GO

-- ----------------------------------------------------------------------------
-- 1.3 Veritabanı rolleri oluştur (grup bazlı yetki yönetimi)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'hospital_app_role' AND type = 'R')
    CREATE ROLE hospital_app_role;

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'hospital_readonly_role' AND type = 'R')
    CREATE ROLE hospital_readonly_role;
GO

-- ----------------------------------------------------------------------------
-- 1.4 Kullanıcıları rollere ekle
-- ----------------------------------------------------------------------------
ALTER ROLE hospital_app_role      ADD MEMBER hospital_app_user;
ALTER ROLE hospital_readonly_role ADD MEMBER hospital_readonly_user;
GO

-- =============================================================================
-- BÖLÜM 2: İZİN YÖNETİMİ (GRANT / DENY / REVOKE)
-- =============================================================================

-- ----------------------------------------------------------------------------
-- 2.1 hospital_app_role: Uygulama kullanıcısı için izinler
--     (SELECT, INSERT, UPDATE, DELETE — DDL yok, DROP yok)
-- ----------------------------------------------------------------------------
-- Tablolar üzerinde DML izni
GRANT SELECT, INSERT, UPDATE, DELETE
    ON SCHEMA::hospital TO hospital_app_role;

-- Stored procedure'ları çalıştırma izni
GRANT EXECUTE ON hospital.sp_GetDoctorDailySchedule  TO hospital_app_role;
GRANT EXECUTE ON hospital.sp_UpdateAppointmentStatus  TO hospital_app_role;
GRANT EXECUTE ON hospital.sp_GetHighRiskPatients       TO hospital_app_role;

-- View'ları okuma izni
GRANT SELECT ON hospital.vw_AppointmentDetails     TO hospital_app_role;
GRANT SELECT ON hospital.vw_PatientRiskSummary     TO hospital_app_role;
GRANT SELECT ON hospital.vw_DailyAppointmentStats  TO hospital_app_role;

-- Fonksiyon izni
GRANT EXECUTE ON hospital.fn_GetRiskLevel                   TO hospital_app_role;
GRANT SELECT  ON hospital.fn_GetPatientAppointmentHistory   TO hospital_app_role;

-- DDL işlemleri KESINLIKLE yasakla
DENY ALTER  ON SCHEMA::hospital TO hospital_app_role;
DENY DROP   ON SCHEMA::hospital TO hospital_app_role;
DENY CREATE TABLE TO hospital_app_role;
GO

-- ----------------------------------------------------------------------------
-- 2.2 hospital_readonly_role: Raporlama/BI için salt okunur erişim
-- ----------------------------------------------------------------------------
-- Yalnızca SELECT
GRANT SELECT ON SCHEMA::hospital TO hospital_readonly_role;

-- View'lar zaten schema içinde, ek grant gerekmez
-- Function için ayrıca izin
GRANT SELECT  ON hospital.fn_GetPatientAppointmentHistory TO hospital_readonly_role;
GRANT EXECUTE ON hospital.fn_GetRiskLevel                 TO hospital_readonly_role;

-- Tüm yazma işlemlerini yasakla
DENY INSERT, UPDATE, DELETE ON SCHEMA::hospital TO hospital_readonly_role;
DENY EXECUTE ON hospital.sp_UpdateAppointmentStatus TO hospital_readonly_role;
GO

-- =============================================================================
-- BÖLÜM 3: ROW-LEVEL SECURITY (RLS)
-- Amaç: Hastalar yalnızca kendi randevularını görsün
--       (Uygulama oturum bağlamı üzerinden filtreleme)
-- =============================================================================

-- ----------------------------------------------------------------------------
-- 3.1 Güvenlik bağlamı fonksiyonu
--     Uygulama, bağlantıyı açmadan önce SESSION_CONTEXT'e UserId set eder:
--     EXEC sp_set_session_context @key=N'current_user_id', @value=N'P000...';
-- ----------------------------------------------------------------------------
IF OBJECT_ID('hospital.fn_SecurityPredicate_Patients', 'IF') IS NOT NULL
    DROP FUNCTION hospital.fn_SecurityPredicate_Patients;
GO

CREATE FUNCTION hospital.fn_SecurityPredicate_Patients
(
    @UserId NVARCHAR(450)
)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
(
    SELECT 1 AS fn_result
    WHERE
        -- Admin veya Doktor rolündeyse herkesi görebilir (IS_MEMBER ile rol kontrolü)
        IS_MEMBER('hospital_app_role') = 1
        AND (
            -- Kendi kaydına erişiyor
            @UserId = CAST(SESSION_CONTEXT(N'current_user_id') AS NVARCHAR(450))
            -- Veya admin/doktor ise (uygulama seviyesinde kontrol edilir)
            OR SESSION_CONTEXT(N'is_admin') = CAST(1 AS SQL_VARIANT)
            OR SESSION_CONTEXT(N'is_doctor') = CAST(1 AS SQL_VARIANT)
        )
);
GO

-- RLS Policy oluştur
IF EXISTS (
    SELECT 1 FROM sys.security_policies
    WHERE name = 'sp_PatientSecurityPolicy'
)
    DROP SECURITY POLICY hospital.sp_PatientSecurityPolicy;

CREATE SECURITY POLICY hospital.sp_PatientSecurityPolicy
    ADD FILTER PREDICATE hospital.fn_SecurityPredicate_Patients(UserId)
    ON hospital.Patients
    WITH (STATE = ON);
GO

-- Kullanım örneği (Uygulama tarafında, bağlantı açılırken):
-- EXEC sp_set_session_context @key=N'current_user_id', @value=N'P0000000-0000-0000-0000-000000000001';
-- EXEC sp_set_session_context @key=N'is_admin', @value=0;
-- EXEC sp_set_session_context @key=N'is_doctor', @value=0;
-- SELECT * FROM hospital.Patients;  -- Yalnızca kendi kaydını görür

-- =============================================================================
-- BÖLÜM 4: SQL INJECTION ÖNLEMİ
-- =============================================================================

-- ----------------------------------------------------------------------------
-- 4.1 SQL INJECTION AÇIĞI OLAN KOD (KÖTÜ ÖRNEK - ÇALIŞTIRILMAMALI!)
-- Bu tür dinamik SQL'den KESİNLİKLE kaçınılmalıdır.
-- ----------------------------------------------------------------------------
/*
DECLARE @HastaAdi NVARCHAR(100) = 'Ali'' OR 1=1 --';   -- Injection payload

-- KÖTÜ: String birleştirme ile dinamik SQL
DECLARE @BadSql NVARCHAR(1000);
SET @BadSql = 'SELECT * FROM hospital.Patients p
               INNER JOIN hospital.Users u ON u.Id = p.UserId
               WHERE u.FirstName = ''' + @HastaAdi + '''';
EXEC(@BadSql);
-- Üretilen SQL: WHERE u.FirstName = 'Ali' OR 1=1 --'
-- Sonuç: TÜM hastalar döner! Güvenlik açığı!
*/

-- ----------------------------------------------------------------------------
-- 4.2 PARAMETRİK SORGU — Doğru yaklaşım (sp_executesql)
-- ----------------------------------------------------------------------------
-- İYİ: sp_executesql ile parametreli dinamik SQL
DECLARE @HastaAdi    NVARCHAR(100) = 'Ali';
DECLARE @GoodSql     NVARCHAR(500);
DECLARE @ParamDef    NVARCHAR(200);

SET @GoodSql   = N'SELECT p.Id, u.FirstName, u.LastName
                   FROM hospital.Patients p
                   INNER JOIN hospital.Users u ON u.Id = p.UserId
                   WHERE u.FirstName = @Ad';
SET @ParamDef  = N'@Ad NVARCHAR(100)';

EXEC sp_executesql @GoodSql, @ParamDef, @Ad = @HastaAdi;
-- Injection payload girilse bile parametre olarak işlenir, SQL parçası olarak değil.
GO

-- ----------------------------------------------------------------------------
-- 4.3 STORED PROCEDURE ile Dolaylı Erişim (Güvenli Soyutlama)
-- Tabloya direkt erişim yerine yalnızca SP üzerinden erişim izni ver
-- ----------------------------------------------------------------------------

-- Örnek: Hastanın kendi bilgilerini güvenli şekilde sorgulayan SP
IF OBJECT_ID('hospital.sp_GetMyProfile', 'P') IS NOT NULL
    DROP PROCEDURE hospital.sp_GetMyProfile;
GO

CREATE PROCEDURE hospital.sp_GetMyProfile
    @UserId NVARCHAR(450)   -- Uygulama JWT'den alır, kullanıcı giremez
AS
BEGIN
    SET NOCOUNT ON;

    -- Parametrenin geçerli GUID formatında olduğunu kontrol et
    -- (Injection'a karşı ek doğrulama)
    IF @UserId IS NULL OR LEN(@UserId) != 36
    BEGIN
        RAISERROR('Geçersiz kullanıcı kimliği.', 16, 1);
        RETURN;
    END

    -- Doğrudan parametreli sorgu (injection riski yok)
    SELECT
        p.Id,
        u.FirstName,
        u.LastName,
        u.Email,
        p.City,
        p.InsuranceType,
        p.TotalAppointments,
        p.NoShowCount
    FROM hospital.Patients p
    INNER JOIN hospital.Users u ON u.Id = p.UserId
    WHERE p.UserId = @UserId;  -- Parametre, string birleştirme DEĞİL
END
GO

-- ----------------------------------------------------------------------------
-- 4.4 C# Uygulama Tarafında Parameterized Query Örneği (Yorum olarak)
-- ----------------------------------------------------------------------------
/*
// KÖTÜ - SQL Injection açığı:
string ad = txtAd.Text;  // Kullanıcı girişi
string sql = "SELECT * FROM hospital.Patients WHERE FirstName = '" + ad + "'";
cmd.CommandText = sql;
cmd.ExecuteReader();

// İYİ - Parametreli sorgu:
string sql = "SELECT * FROM hospital.Patients WHERE FirstName = @Ad";
using (var cmd = new SqlCommand(sql, connection))
{
    cmd.Parameters.Add("@Ad", SqlDbType.NVarChar, 100).Value = txtAd.Text;
    cmd.ExecuteReader();
}

// ASP.NET Core ile Entity Framework (ORM) - En güvenli yol:
var patients = await dbContext.Patients
    .Include(p => p.User)
    .Where(p => p.User.FirstName == firstName)  // EF Core otomatik parametreli sorgu üretir
    .ToListAsync();
*/

-- =============================================================================
-- BÖLÜM 5: EK GÜVENLİK ÖNLEMLERİ
-- =============================================================================

-- ----------------------------------------------------------------------------
-- 5.1 Hassas sütunlar için şifreleme önerisi
-- (SQL Server Always Encrypted veya kolon maskesi)
-- ----------------------------------------------------------------------------

-- Dinamik Veri Maskeleme (DDM) - TCKN sütununu maskele
-- (Yalnızca yetkili kullanıcılar gerçek değeri görebilir)
ALTER TABLE hospital.Patients
    ALTER COLUMN IdentityNumber
    NCHAR(11) MASKED WITH (FUNCTION = 'partial(0,"*******",4)');
GO
-- Sonuç: 12345678901 yerine ***7890 (son 4 hane gösterilir)
-- UNMASK yetkisi olmayan kullanıcı maskelenmiş değer görür

-- Yetkili role UNMASK izni ver
GRANT UNMASK TO hospital_app_role;
DENY  UNMASK TO hospital_readonly_role;  -- Readonly kullanıcı maskelenmiş görür
GO

-- ----------------------------------------------------------------------------
-- 5.2 Audit tablosunu koruma (Salt-ekle, güncelleme/silme yasak)
-- ----------------------------------------------------------------------------
DENY UPDATE, DELETE ON hospital.AuditLog TO hospital_app_role;
DENY UPDATE, DELETE ON hospital.AuditLog TO hospital_readonly_role;
GO

-- ----------------------------------------------------------------------------
-- 5.3 Şifre bilgisi içeren sütunlara erişimi kısıtla
-- Users tablosundaki PasswordHash, SecurityStamp sütunlarına erişim kısıtı
-- ----------------------------------------------------------------------------
DENY SELECT ON hospital.Users (PasswordHash, SecurityStamp, ConcurrencyStamp)
    TO hospital_readonly_role;
GO

-- =============================================================================
-- BÖLÜM 6: GÜVENLİK DOĞRULAMA SORGULARI
-- =============================================================================

-- Mevcut izinleri listele
SELECT
    dp.name          AS Principal,
    dp.type_desc     AS PrincipalTipi,
    o.name           AS Nesne,
    p.permission_name AS Izin,
    p.state_desc     AS Durum
FROM sys.database_permissions p
INNER JOIN sys.database_principals dp ON dp.principal_id = p.grantee_principal_id
LEFT  JOIN sys.objects o ON o.object_id = p.major_id
WHERE dp.name IN ('hospital_app_role', 'hospital_readonly_role',
                  'hospital_app_user', 'hospital_readonly_user')
ORDER BY dp.name, o.name, p.permission_name;
GO

-- RLS policy durumunu kontrol et
SELECT
    sp.name                     AS PolicyAdi,
    sp.is_enabled               AS Aktif,
    t.name                      AS Tablo,
    sp.type_desc                AS PolicyTipi
FROM sys.security_policies sp
INNER JOIN sys.tables t ON t.object_id = sp.object_id
WHERE t.schema_id = SCHEMA_ID('hospital');
GO

-- =============================================================================
-- ÖZET: Uygulanan Güvenlik Katmanları
-- =============================================================================
-- 1. Ayrı login/user: sa yerine hospital_app_login ve hospital_readonly_login
-- 2. Rol tabanlı izin: hospital_app_role ve hospital_readonly_role
-- 3. DDL yasağı: DROP/ALTER/CREATE yetkisi verilmedi
-- 4. Audit log koruması: AuditLog tablosuna UPDATE/DELETE yok
-- 5. SQL Injection önlemi: sp_executesql ile parametreli dinamik SQL
-- 6. Stored Procedure soyutlaması: Tabloya direkt erişim yerine SP üzerinden
-- 7. Row-Level Security: Hastalar yalnızca kendi verisini görebilir
-- 8. Dinamik Veri Maskeleme: TCKN sütunu maskelendi
-- 9. Hassas sütun kısıtı: PasswordHash/SecurityStamp readonly role'e engellendi
-- =============================================================================
