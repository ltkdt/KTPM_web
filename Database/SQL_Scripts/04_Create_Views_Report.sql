USE HeThong_ECG;
GO

-- 1. VIEW: THỐNG KÊ TÌNH TRẠNG CẢNH BÁO ECG CỦA TỪNG BỆNH NHÂN
IF OBJECT_ID('dbo.View_ThongKeTanSuatDo', 'V') IS NOT NULL
    DROP VIEW dbo.View_ThongKeTanSuatDo;
GO

CREATE VIEW dbo.View_ThongKeTanSuatDo
AS
    SELECT
        b.Id AS MaBenhNhan,
        b.HoTen AS TenBenhNhan,
        COUNT(e.Id) AS TongSoLanDo,
        MAX(e.ThoiGianDo) AS LanDoGanNhat
    FROM BenhNhan b
        LEFT JOIN DuLieuECG e ON b.Id = e.BenhNhanId
    GROUP BY b.Id, b.HoTen;
GO
-- 2. VIEW: THỐNG KÊ HIỆU SUẤT XỬ LÝ CA TƯ VẤN CỦA BÁC SĨ
IF OBJECT_ID('dbo.View_HieuSuatBacSi', 'V') IS NOT NULL
    DROP VIEW dbo.View_HieuSuatBacSi;
GO

CREATE VIEW dbo.View_HieuSuatBacSi
AS
    SELECT
        bs.Id AS MaBacSi,
        bs.HoTen AS TenBacSi,
        bs.ChuyenKhoa AS ChuyenKhoa,
        COUNT(c.Id) AS TongSoCaPhuTrach,
        SUM(CASE WHEN c.TrangThai = 'RESPONDED' THEN 1 ELSE 0 END) AS SoCaDaXuLy,
        SUM(CASE WHEN c.TrangThai = 'PENDING' THEN 1 ELSE 0 END) AS SoCaTonDong
    FROM BacSi bs
        LEFT JOIN CaTuVan c ON bs.Id = c.BacSiId
    GROUP BY bs.Id, bs.HoTen, bs.ChuyenKhoa;
GO


-- LỆNH TRUY VẤN XEM KẾT QUẢ
-- SELECT * FROM dbo.Viewa_ThongKeCanhBao ORDER BY SoLanNguyHiem DESC;
-- SELECT * FROM dbo.View_HieuSuatBacSi ORDER BY SoCaTonDong DESC;