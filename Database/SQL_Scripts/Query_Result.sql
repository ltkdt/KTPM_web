USE HeThong_ECG;
GO
-- BẢNG 1: THEO DÕI TOÀN BỘ TÀI KHOẢN VÀ PHÂN QUYỀN (Module Admin)
SELECT
    tk.Id AS MaTaiKhoan,
    tk.TenDangNhap AS TenDangNhap,
    q.TenQuyen AS QuyenTruyCap,
    tk.TrangThai AS TrangThaiTaiKhoan,
    tk.NgayTao AS NgayTao
FROM TaiKhoan tk
    JOIN Quyen q ON tk.QuyenId = q.Id
ORDER BY tk.Id;

-- BẢNG 2: THEO DÕI HỒ SƠ BỆNH NHÂN VÀ THIẾT BỊ MÔ PHỎNG
SELECT
    bn.Id AS MaBenhNhan,
    bn.HoTen AS TenBenhNhan,
    bn.Tuoi AS Tuoi,
    bn.TienSuBenh AS TienSuBenh,
    bn.MaThietBiMoPhong AS MaThietBiMoPhong,
    tk.TenDangNhap AS TaiKhoanLienKet
FROM BenhNhan bn
    JOIN TaiKhoan tk ON bn.TaiKhoanId = tk.Id;

-- BẢNG 3: THEO DÕI LUỒNG DỮ LIỆU ECG REAL-TIME TỪ THIẾT BỊ
SELECT
    d.Id AS MaLog,
    bn.HoTen AS TenBenhNhan,
    bn.MaThietBiMoPhong AS ThietBiDo,
    d.ThoiGianDo AS ThoiGianDo,
    d.NhipTim AS NhipTim,
    d.HuyetAp AS HuyetAp,
    d.MucDoCanhBao AS MucDoCanhBao,
    d.DuLieuGocJson AS DuLieuTho_JSON
FROM DuLieuECG d
    JOIN BenhNhan bn ON d.BenhNhanId = bn.Id
ORDER BY d.ThoiGianDo DESC;

-- BẢNG 4: THEO DÕI TIẾN ĐỘ XỬ LÝ CA TƯ VẤN CỦA BÁC SĨ
SELECT
    c.Id AS MaCaTuVan,
    bn.HoTen AS TenBenhNhan,
    bs.HoTen AS BacSiPhuTrach,
    d.MucDoCanhBao AS MucDoNguyHiem_ECG,
    c.TrieuChung AS TrieuChung,
    c.TrangThai AS TrangThaiXuLy,
    c.ThoiGianGui AS ThoiGianGui
FROM CaTuVan c
    JOIN BenhNhan bn ON c.BenhNhanId = bn.Id
    JOIN BacSi bs ON c.BacSiId = bs.Id
    JOIN DuLieuECG d ON c.DuLieuECGId = d.Id
ORDER BY c.ThoiGianGui DESC;
