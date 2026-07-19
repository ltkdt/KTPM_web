USE HeThong_ECG;
GO

-- ============================================================
-- SCRIPT CHÍNH (03/4): Dữ liệu ECG + Ca tư vấn
-- ============================================================

CREATE TABLE DuLieuECG
(
    Id INT PRIMARY KEY IDENTITY(1,1),
    BenhNhanId INT FOREIGN KEY REFERENCES BenhNhan(Id),
    ThoiGianDo DATETIME DEFAULT GETDATE(),
    NhipTim INT NULL,
    Rmssd FLOAT NULL,
    HuyetAp VARCHAR(20) NULL,
    DuongDanCsv NVARCHAR(500) NULL,
    DuLieuGocJson NVARCHAR(MAX) NULL,
    MucDoCanhBao VARCHAR(20) CHECK (MucDoCanhBao IN ('NORMAL', 'WARNING', 'DANGEROUS')) DEFAULT 'NORMAL'
);
GO

CREATE TABLE CaTuVan
(
    Id INT PRIMARY KEY IDENTITY(1,1),
    BenhNhanId INT FOREIGN KEY REFERENCES BenhNhan(Id),
    BacSiId INT FOREIGN KEY REFERENCES BacSi(Id),
    DuLieuECGId INT FOREIGN KEY REFERENCES DuLieuECG(Id),
    TrieuChung NVARCHAR(MAX),
    ThoiGianGui DATETIME DEFAULT GETDATE(),
    ChanDoan NVARCHAR(MAX),
    PhacDoDieuTri NVARCHAR(MAX),
    ThoiGianPhanHoi DATETIME NULL,
    TrangThai VARCHAR(20) CHECK (TrangThai IN ('PENDING', 'RESPONDED')) DEFAULT 'PENDING'
);
GO

-- ============================================================
-- DỮ LIỆU MẪU ECG (15 bản ghi: mỗi bệnh nhân 3 bản ghi, ID 1-15)
-- RecordName tương ứng file CSV: Record_{PatientId}_{seq}.csv
-- ============================================================
SET IDENTITY_INSERT DuLieuECG ON;
GO

INSERT INTO DuLieuECG (Id, BenhNhanId, ThoiGianDo, NhipTim, Rmssd, HuyetAp, DuongDanCsv, DuLieuGocJson, MucDoCanhBao)
VALUES
    (1,  1, '2024-03-01 08:00:00', 72, 35.2, '120/80', N'C:\SQLdata\CSV\Record_1_1.csv',  NULL, 'NORMAL'),
    (2,  1, '2024-03-02 08:00:00', 75, 38.1, '118/78', N'C:\SQLdata\CSV\Record_1_2.csv',  NULL, 'NORMAL'),
    (3,  1, '2024-03-03 08:00:00', 78, 40.5, '122/82', N'C:\SQLdata\CSV\Record_1_3.csv',  NULL, 'NORMAL'),
    (4,  2, '2024-03-04 08:00:00', 80, 42.0, '125/85', N'C:\SQLdata\CSV\Record_2_1.csv',  NULL, 'NORMAL'),
    (5,  2, '2024-03-05 08:00:00', 82, 44.3, '128/86', N'C:\SQLdata\CSV\Record_2_2.csv',  NULL, 'WARNING'),
    (6,  2, '2024-03-06 08:00:00', 85, 46.7, '130/88', N'C:\SQLdata\CSV\Record_2_3.csv',  NULL, 'WARNING'),
    (7,  3, '2024-03-07 08:00:00', 70, 33.8, '115/75', N'C:\SQLdata\CSV\Record_3_1.csv',  NULL, 'NORMAL'),
    (8,  3, '2024-03-08 08:00:00', 73, 36.4, '117/76', N'C:\SQLdata\CSV\Record_3_2.csv',  NULL, 'NORMAL'),
    (9,  3, '2024-03-09 08:00:00', 76, 39.0, '119/77', N'C:\SQLdata\CSV\Record_3_3.csv',  NULL, 'NORMAL'),
    (10, 4, '2024-03-10 08:00:00', 88, 48.2, '132/90', N'C:\SQLdata\CSV\Record_4_1.csv',  NULL, 'WARNING'),
    (11, 4, '2024-03-11 08:00:00', 90, 50.1, '135/92', N'C:\SQLdata\CSV\Record_4_2.csv',  NULL, 'WARNING'),
    (12, 4, '2024-03-12 08:00:00', 92, 52.5, '138/94', N'C:\SQLdata\CSV\Record_4_3.csv',  NULL, 'DANGEROUS'),
    (13, 5, '2024-03-13 08:00:00', 68, 31.5, '112/72', N'C:\SQLdata\CSV\Record_5_1.csv',  NULL, 'NORMAL'),
    (14, 5, '2024-03-14 08:00:00', 71, 34.0, '114/74', N'C:\SQLdata\CSV\Record_5_2.csv',  NULL, 'NORMAL'),
    (15, 5, '2024-03-15 08:00:00', 74, 37.2, '116/76', N'C:\SQLdata\CSV\Record_5_3.csv',  NULL, 'NORMAL');
GO

SET IDENTITY_INSERT DuLieuECG OFF;
GO

-- Ca tư vấn mẫu: bệnh nhân 1 gửi complaint về bản ghi nguy hiểm (id 12 của BN4 minh họa pending)
INSERT INTO CaTuVan (BenhNhanId, BacSiId, DuLieuECGId, TrieuChung, TrangThai)
VALUES
    (1, 2, 2, N'Tôi cảm thấy hơi choáng và tim đập nhanh sau khi leo cầu thang.', 'PENDING'),
    (4, 1, 12, N'Bác sĩ ơi, thiết bị vừa báo động đỏ, nhịp tim tôi đập rất nhanh.', 'PENDING');
GO
