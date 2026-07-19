USE HeThong_ECG;
GO

/* Chạy sau script 05. Bổ sung danh mục thiết bị ECG hợp lệ cho Admin. */
/* Đồng bộ tên bác sĩ mẫu cho cả CSDL đã tạo từ các script phiên bản cũ. */
UPDATE dbo.BacSi SET HoTen = N'BS. Nguyễn Minh An', ChuyenKhoa = N'Tim mạch', Email = N'an.nguyen@hospital.com' WHERE TaiKhoanId = 2;
UPDATE dbo.BacSi SET HoTen = N'BS. Trần Thu Hà', ChuyenKhoa = N'Tim mạch', Email = N'ha.tran@hospital.com' WHERE TaiKhoanId = 3;
GO

IF OBJECT_ID('dbo.ThietBiEcg', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ThietBiEcg
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        MaThietBi NVARCHAR(100) NOT NULL UNIQUE,
        TrangThai VARCHAR(20) NOT NULL CONSTRAINT DF_ThietBiEcg_TrangThai DEFAULT 'AVAILABLE'
            CHECK (TrangThai IN ('AVAILABLE', 'ASSIGNED', 'INACTIVE')),
        NgayKhaiBao DATETIME NOT NULL CONSTRAINT DF_ThietBiEcg_NgayKhaiBao DEFAULT GETDATE(),
        GhiChu NVARCHAR(255) NULL
    );
END
GO

/* Đưa các MAC đã liên kết từ phiên bản cũ vào danh mục để không làm gián đoạn simulator. */
INSERT INTO dbo.ThietBiEcg (MaThietBi, TrangThai)
SELECT DISTINCT p.MaThietBi, 'ASSIGNED'
FROM dbo.ThietBiBenhNhan p
WHERE NOT EXISTS (SELECT 1 FROM dbo.ThietBiEcg d WHERE d.MaThietBi = p.MaThietBi);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TaiKhoan_TrangThai_QuyenId')
    CREATE INDEX IX_TaiKhoan_TrangThai_QuyenId ON dbo.TaiKhoan(TrangThai, QuyenId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BenhNhan_BacSiId')
    CREATE INDEX IX_BenhNhan_BacSiId ON dbo.BenhNhan(BacSiId);
GO

/* Bổ sung trạng thái COMPLETED cho báo cáo vòng đời ca tư vấn. */
DECLARE @ConstraintName SYSNAME;
DECLARE @DropConstraintSql NVARCHAR(MAX);
SELECT @ConstraintName = dc.name
FROM sys.check_constraints dc
WHERE dc.parent_object_id = OBJECT_ID('dbo.CaTuVan') AND dc.definition LIKE '%PENDING%RESPONDED%';

IF @ConstraintName IS NOT NULL
BEGIN
    SET @DropConstraintSql = N'ALTER TABLE dbo.CaTuVan DROP CONSTRAINT ' + QUOTENAME(@ConstraintName) + N';';
    EXEC sys.sp_executesql @DropConstraintSql;
END

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.CaTuVan') AND name = 'CK_CaTuVan_TrangThai')
    ALTER TABLE dbo.CaTuVan ADD CONSTRAINT CK_CaTuVan_TrangThai CHECK (TrangThai IN ('PENDING', 'RESPONDED', 'COMPLETED'));
GO
