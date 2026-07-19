USE HeThong_ECG;
GO

/*
   Run after scripts 01-04.  These constraints reflect the API contract:
   one physical device has one current patient assignment, and every lookup
   used by the API is indexed.
*/

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ThietBiBenhNhan_MaThietBi')
    CREATE UNIQUE INDEX UX_ThietBiBenhNhan_MaThietBi ON dbo.ThietBiBenhNhan(MaThietBi);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DuLieuECG_BenhNhan_ThoiGianDo')
    CREATE INDEX IX_DuLieuECG_BenhNhan_ThoiGianDo ON dbo.DuLieuECG(BenhNhanId, ThoiGianDo DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CaTuVan_DuLieuECG_Id')
    CREATE INDEX IX_CaTuVan_DuLieuECG_Id ON dbo.CaTuVan(DuLieuECGId, Id DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BenhNhan_TaiKhoanId')
    CREATE UNIQUE INDEX UX_BenhNhan_TaiKhoanId ON dbo.BenhNhan(TaiKhoanId) WHERE TaiKhoanId IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BacSi_TaiKhoanId')
    CREATE UNIQUE INDEX UX_BacSi_TaiKhoanId ON dbo.BacSi(TaiKhoanId) WHERE TaiKhoanId IS NOT NULL;
GO
