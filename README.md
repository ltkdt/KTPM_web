# Hệ thống theo dõi điện tim ECG

Hệ thống quản lý dữ liệu điện tim gồm Web dành cho bệnh nhân, ứng dụng WPF dành cho bác sĩ/quản trị viên, API ASP.NET Core Minimal API, SQL Server và chương trình mô phỏng thiết bị IoT ECG.

## Thành phần

| Thành phần | Đường dẫn | Vai trò |
| --- | --- | --- |
| API + Web | `API/EcgRecordAPI` | Cung cấp REST API, SignalR và giao diện Web bệnh nhân tại `http://localhost:5000`. |
| WPF HRMonitor | `HRMonitor/HRMonitor` | Bác sĩ xem ECG, phản hồi tư vấn; Admin quản lý và điều phối. |
| HardwareSim | `HardwareSim` | Mô phỏng thiết bị ECG, gửi tín hiệu, nhịp tim và RMSSD. |
| SQL scripts | `Database/SQL_Scripts` | Khởi tạo schema, dữ liệu mẫu, index và chức năng Admin/thiết bị. |

## Yêu cầu

- .NET SDK 10.0
- SQL Server hoặc SQL Server Express
- SQL Server Management Studio (SSMS) hoặc công cụ chạy script SQL tương đương

## Khởi tạo cơ sở dữ liệu

Mở SSMS và chạy các script theo đúng thứ tự:

1. `01_Create_Database_And_Accounts.sql`
2. `02_Create_Patients_Doctors.sql`
3. `03_Create_EcgLogs_Consultations.sql`
4. `04_Create_Views_Report.sql`
5. `05_Api_Integration_Indexes.sql`
6. `06_Admin_Device_Management.sql`

Script 06 là bắt buộc với bản hiện tại: bổ sung danh mục thiết bị ECG hợp lệ và các API quản trị.

## Cấu hình và chạy API

Chuỗi kết nối nằm trong [appsettings.json](API/EcgRecordAPI/appsettings.json). Thay `Server=localhost\SQLEXPRESS` nếu SQL Server của bạn dùng instance khác.

```powershell
cd API\EcgRecordAPI
dotnet restore
dotnet run
```

Mở `http://localhost:5000`. Web hỗ trợ đăng ký/đăng nhập bệnh nhân, xem chỉ số ECG, gửi yêu cầu tư vấn, đăng xuất và dữ liệu ECG minh họa.

## Chạy ứng dụng WPF

Khởi động API trước, rồi chạy:

```powershell
cd HRMonitor\HRMonitor
dotnet run
```

Bác sĩ xem bệnh nhân được giao, mở bản ghi ECG và phản hồi tư vấn. Admin có dashboard số liệu, danh sách bệnh nhân/bác sĩ, phân công bác sĩ và nút đăng xuất.

## Tài khoản dữ liệu mẫu

### Quản trị viên

- Tài khoản: `admin`
- Mật khẩu: `admin`

Trong WPF, nhấn **Đăng nhập Quản trị viên** trước khi đăng nhập.

### Bác sĩ

| Bác sĩ | Tài khoản | Mật khẩu | Phụ trách mẫu |
| --- | --- | --- | --- |
| BS. Nguyễn Minh An | `1` | `2222` | Phạm Thị D, Hoàng Văn E |
| BS. Trần Thu Hà | `2` | `1111` | Nguyễn Văn A, Trần Thị B, Lê Văn C |

### Bệnh nhân

| Bệnh nhân | Mật khẩu |
| --- | --- |
| Nguyen Van A | `8392` |
| Tran Thi B | `1234` |
| Le Van C | `5678` |
| Pham Thi D | `4321` |
| Hoang Van E | `8765` |

## Phân hệ Admin

- `GET /api/admin/dashboard`: số bệnh nhân, bác sĩ, thiết bị ECG đang hoạt động và trạng thái tư vấn.
- `GET /api/admin/users?role=BENHNHAN&status=ACTIVE`: lọc tài khoản theo vai trò/trạng thái.
- `PUT /api/admin/users/{id}/lock` và `/unlock`: khóa/mở khóa tài khoản.
- `DELETE /api/admin/users/{id}`: chỉ xóa khi chưa có hồ sơ ECG hoặc ca tư vấn; tài khoản có hồ sơ y tế phải khóa tạm thời.
- `POST /api/admin/doctors`: tạo tài khoản bác sĩ.
- `GET /api/admin/patients/unassigned` và `PUT /api/admin/assignments`: theo dõi/phân công bệnh nhân, có SignalR `AdminAssignedPatient`.
- `GET/POST /api/admin/devices`, `DELETE /api/admin/devices/{macAddress}`: khai báo, xem và thu hồi thiết bị.

## HardwareSim

Trước khi chạy simulator, bảo đảm MAC trong `HardwareSim/config.json` đã có trong `ThietBiEcg`. Sau khi chạy script 06, MAC mẫu `00:1A:2B:3C:4D:5E` được tự động khai báo.

```powershell
cd HardwareSim
dotnet run
```

Simulator liên kết thiết bị với `PatientId` trong `config.json`, lưu tạm khi mất kết nối và tự tải dữ liệu ECG lên API khi kết nối lại.

## Lưu ý bảo mật

Đây là dự án học phần; dữ liệu mẫu dùng mật khẩu dạng plain text để dễ trình diễn. Trước khi triển khai thực tế, cần chuyển sang hash mật khẩu, JWT và policy phân quyền thực sự cho toàn bộ endpoint `/api/admin/*`.
