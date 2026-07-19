USE HeThong_ECG;
GO

-- ============================================================
-- SCRIPT CHÍNH (02/4): Hồ sơ Bác sĩ, Bệnh nhân, Thiết bị IoT
-- ============================================================

CREATE TABLE BacSi
(
    Id INT PRIMARY KEY IDENTITY(1,1),
    TaiKhoanId INT FOREIGN KEY REFERENCES TaiKhoan(Id),
    HoTen NVARCHAR(100) NOT NULL,
    ChuyenKhoa NVARCHAR(100),
    SoDienThoai VARCHAR(15),
    Email NVARCHAR(100),
    SoNamKinhNghiem INT
);
GO

CREATE TABLE BenhNhan
(
    Id INT PRIMARY KEY IDENTITY(1,1),
    TaiKhoanId INT FOREIGN KEY REFERENCES TaiKhoan(Id),
    BacSiId INT NULL FOREIGN KEY REFERENCES BacSi(Id),
    HoTen NVARCHAR(100) NOT NULL,
    Tuoi INT,
    GioiTinh NVARCHAR(10),
    SoDienThoai VARCHAR(15),
    Email NVARCHAR(100),
    DiaChi NVARCHAR(MAX),
    TienSuBenh NVARCHAR(MAX),
    MaThietBiMoPhong VARCHAR(50)
);
GO

-- Liên kết thiết bị IoT ↔ bệnh nhân
CREATE TABLE ThietBiBenhNhan
(
    Id INT PRIMARY KEY IDENTITY(1,1),
    BenhNhanId INT NOT NULL FOREIGN KEY REFERENCES BenhNhan(Id),
    MaThietBi NVARCHAR(100) NOT NULL,
    ThoiGianKetNoi DATETIME DEFAULT GETDATE()
);
GO

-- ============================================================
-- DỮ LIỆU MẪU BÁC SĨ
-- Đăng nhập WPF: nhập ID bác sĩ (1/2) + mật khẩu (2222/1111)
-- ============================================================
SET IDENTITY_INSERT BacSi ON;
GO

INSERT INTO BacSi (Id, TaiKhoanId, HoTen, ChuyenKhoa, SoDienThoai, Email, SoNamKinhNghiem)
VALUES
    (1, 2, N'BS. Nguyễn Minh An', N'Tim mạch', '0901111111', N'an.nguyen@hospital.com', 12),
    (2, 3, N'BS. Trần Thu Hà', N'Tim mạch', '0902222222', N'ha.tran@hospital.com', 8);
GO

SET IDENTITY_INSERT BacSi OFF;
GO

-- ============================================================
-- DỮ LIỆU MẪU BỆNH NHÂN
-- Đăng nhập Web: Họ tên + mật khẩu (theo README)
-- Phân công: A,B,C -> BS. Trần Thu Hà (2); D,E -> BS. Nguyễn Minh An (1)
-- ============================================================
SET IDENTITY_INSERT BenhNhan ON;
GO

INSERT INTO BenhNhan (Id, TaiKhoanId, BacSiId, HoTen, Tuoi, GioiTinh, SoDienThoai, Email, DiaChi, TienSuBenh, MaThietBiMoPhong)
VALUES
    (1, 4, 2, N'Nguyen Van A', 45, N'Nam', '0901234567', N'nva@example.com',  N'123 Le Loi, HCMC',    N'Tiền sử cao huyết áp',           '00:1A:2B:3C:4D:5E'),
    (2, 5, 2, N'Tran Thi B',   38, N'Nữ',  '0902345678', N'ttb@example.com',  N'456 Nguyen Hue, HCMC', N'Rối loạn nhịp tim nhẹ',         '00:1A:2B:3C:4D:5F'),
    (3, 6, 2, N'Le Van C',     52, N'Nam', '0903456789', N'lvc@example.com',  N'789 Tran Hung Dao, HCMC', N'Đái tháo đường type 2',      '00:1A:2B:3C:4D:60'),
    (4, 7, 1, N'Pham Thi D',   29, N'Nữ',  '0904567890', N'ptd@example.com',  N'321 Vo Van Tan, HCMC', N'Hen suyễn',                      '00:1A:2B:3C:4D:61'),
    (5, 8, 1, N'Hoang Van E',  61, N'Nam', '0905678901', N'hve@example.com',  N'654 Hai Ba Trung, HCMC', N'Suy tim giai đoạn II',        '00:1A:2B:3C:4D:62');
GO

SET IDENTITY_INSERT BenhNhan OFF;
GO

-- Liên kết thiết bị mặc định cho bệnh nhân 1 (dùng HardwareSim)
INSERT INTO ThietBiBenhNhan (BenhNhanId, MaThietBi)
VALUES (1, '00:1A:2B:3C:4D:5E');
GO
