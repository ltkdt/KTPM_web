USE master;
GO

-- ============================================================
-- SCRIPT CHÍNH (01/04): Khởi tạo CSDL HeThong_ECG + Tài khoản
-- Chạy theo thứ tự 01 -> 02 -> 03 -> 04
-- ============================================================

-- Xóa CSDL cũ nếu tồn tại
IF DB_ID('HeThong_ECG') IS NOT NULL
BEGIN
    ALTER DATABASE HeThong_ECG SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE HeThong_ECG;
END
GO

CREATE DATABASE HeThong_ECG;
GO
USE HeThong_ECG;
GO

-- Bảng phân quyền
CREATE TABLE Quyen
(
    Id INT PRIMARY KEY IDENTITY(1,1),
    TenQuyen VARCHAR(50) NOT NULL,
    MoTa NVARCHAR(255)
);
GO

-- Bảng tài khoản đăng nhập chung (Admin / Bác sĩ / Bệnh nhân)
CREATE TABLE TaiKhoan
(
    Id INT PRIMARY KEY IDENTITY(1,1),
    TenDangNhap VARCHAR(50) UNIQUE NOT NULL,
    MatKhau VARCHAR(255) NOT NULL,
    QuyenId INT FOREIGN KEY REFERENCES Quyen(Id),
    TrangThai VARCHAR(20) CHECK (TrangThai IN ('ACTIVE', 'INACTIVE', 'LOCKED')) DEFAULT 'ACTIVE',
    NgayTao DATETIME DEFAULT GETDATE()
);
GO

INSERT INTO Quyen
    (TenQuyen, MoTa)
VALUES
    ('QUANTRI', N'Quản trị viên hệ thống'),
    ('BACSI', N'Bác sĩ tư vấn'),
    ('BENHNHAN', N'Bệnh nhân');
GO

-- ============================================================
-- DỮ LIỆU TÀI KHOẢN MẪU (đồng bộ với README / API hiện tại)
--
-- Admin (WPF):     TenDangNhap = admin,     MatKhau = admin
-- Bác sĩ (WPF):    TenDangNhap = 1 hoặc 2,  MatKhau = 2222 / 1111
-- Bệnh nhân (Web): TenDangNhap = Họ tên,    MatKhau = xem bảng bên dưới
-- ============================================================
SET IDENTITY_INSERT TaiKhoan ON;
GO

INSERT INTO TaiKhoan
    (Id, TenDangNhap, MatKhau, QuyenId, TrangThai)
VALUES
    (1, 'admin', 'admin', 1, 'ACTIVE'),
    (2, '1', '2222', 2, 'ACTIVE'),
    -- BS. Nguyễn Minh An
    (3, '2', '1111', 2, 'ACTIVE'),
    -- BS. Trần Thu Hà
    (4, 'Nguyen Van A', '8392', 3, 'ACTIVE'),
    (5, 'Tran Thi B', '1234', 3, 'ACTIVE'),
    (6, 'Le Van C', '5678', 3, 'ACTIVE'),
    (7, 'Pham Thi D', '4321', 3, 'ACTIVE'),
    (8, 'Hoang Van E', '8765', 3, 'ACTIVE');
GO

SET IDENTITY_INSERT TaiKhoan OFF;
GO
