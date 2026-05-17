UI cơ bản cho btl KTPM

Task cần làm:
- Liên kết, tạo export đọc từ SQL bằng C# rồi parse nó vào csv. Module csv_module hiện đã mô phỏng đọc từ file csv rồi chiếu lên frontend bằng chart.js

Tham khảo thêm nếu cần:
https://www.youtube.com/watch?v=FRlqMJWlpTI

Code hiện tại có thể chạy bằng live-server / (vscode / visual studio)

Hiện tại đã có thêm frontend app WPF (HRMonitor)

![demo](demo.png)
## Cài đặt và Chạy Hệ Thống

Hệ thống bao gồm một module API (.NET 8) làm backend và một ứng dụng desktop WPF (HR_Monitor) đóng vai trò client (dành cho bác sĩ) kết nối tới API này.

### 1. Yêu cầu hệ thống
* **.NET 8.0 SDK**: Máy của bạn cần cài đặt .NET 8.0 SDK để biên dịch và chạy được API và app WPF.
* **SQL Server**: Hệ thống sử dụng cơ sở dữ liệu SQL Server.
  * Bạn cần cài đặt SQL Server (ví dụ: SQL Server Express) và một công cụ quản lý như SQL Server Management Studio (SSMS).
  * **Tạo Database**: Mở SSMS, kết nối vào Server của bạn. Mở file script `SQL/Database_BenhVien.sql` có sẵn trong source code và chạy (Execute) toàn bộ file này. File script sẽ tự động tạo Database tên là `BenhVienDB`, các bảng liên quan và nạp sẵn dữ liệu mẫu.

### 2. Cấu hình & Chạy API (Backend)
1. **Sửa Connection String**: 
   Mở file `API/EcgRecordAPI/Program.cs` (khoảng dòng 20) và sửa tên Server SQL cho phù hợp với máy của bạn:
   ```csharp
   string connString = @"Server=TEN_SERVER_CUA_BAN\SQLEXPRESS; Database=BenhVienDB; Integrated Security=True; TrustServerCertificate=True;";
   ```
2. **Khởi chạy API**:
   Mở Terminal/PowerShell, di chuyển vào thư mục API và chạy các lệnh sau:
   ```bash
   cd API\EcgRecordAPI
   dotnet restore
   dotnet run
   ```
   *Lưu ý: API sẽ lắng nghe tại `http://localhost:5000`. Hãy giữ cửa sổ Terminal này luôn chạy ngầm để phần Web và WPF có thể gọi tới.*

### 3. Cấu hình & Chạy ứng dụng WPF (HR Monitor)
Ứng dụng WPF đã được tích hợp sẵn HTTP Client và SignalR để kết nối realtime tới API (`http://localhost:5000`).

* **Cách khởi chạy**:
  1. Hãy đảm bảo **API đang được chạy** (ở bước 2).
  2. Mở file thư mục `HRMonitor` bằng **Visual Studio 2022**. (Bạn có thể mở qua file `.sln`).
  3. Nhấn **F5** (hoặc nút Start) để chạy ứng dụng WPF. 
  
  *(Hoặc bạn cũng có thể mở Terminal tại `HRMonitor\HRMonitor` và dùng lệnh `dotnet run`)*.