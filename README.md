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

Nếu không có 8.0 có thể tự chỉnh file .csproj thành 10.0

```
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.3" />
    <PackageReference Include="Microsoft.Data.SqlClient" Version="7.0.1" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
  </ItemGroup>

</Project>
```

**SQL Server**: Hệ thống sử dụng cơ sở dữ liệu SQL Server.
  * Bạn cần cài đặt SQL Server (ví dụ: SQL Server Express) và một công cụ quản lý như SQL Server Management Studio (SSMS).
  * **Tạo Database**: Mở SSMS, kết nối vào Server của bạn. Mở file script `SQL/Database_BenhVien.sql` có sẵn trong source code và chạy (Execute) toàn bộ file này. File script sẽ tự động tạo Database tên là `BenhVienDB`, các bảng liên quan và nạp sẵn dữ liệu mẫu.

### 2. Cấu hình & Chạy API (Backend)
1. **Sửa Connection String**: 
   Mở file `API/EcgRecordAPI/Program.cs` (khoảng dòng 20) và cấu hình Server SQL của bạn. Theo mặc định nếu bạn cài SQL Server Express, nó sẽ là `localhost\SQLEXPRESS`:
   ```csharp
   string connString = @"Server=localhost\SQLEXPRESS; Database=BenhVienDB; Integrated Security=True; TrustServerCertificate=True;";
   ```
2. **Khởi chạy API**:
   Mở Terminal/PowerShell, di chuyển vào thư mục API và chạy các lệnh sau:
   ```bash
   cd API\EcgRecordAPI
   dotnet restore
   dotnet run
   ```
   *Lưu ý: API sẽ lắng nghe tại `http://localhost:5000`. Hãy giữ cửa sổ Terminal này luôn chạy ngầm để phần Web và WPF có thể gọi tới.*

### Kế hoạch Gộp Frontend và API thành 1 Project duy nhất (Đề xuất)
Phương pháp này giúp đưa toàn bộ giao diện tĩnh của WebFrontend vào trong Web API, giúp chạy cả hai trên cùng một địa chỉ `localhost:5000`.


**Trải nghiệm**:
- Chạy lệnh `dotnet run` tại `API/EcgRecordAPI`.
- Mở trình duyệt và truy cập `http://localhost:5000`. Cả giao diện Web và API backend đều khởi động cùng lúc trên cổng này. Các hàm fetch trong JS gọi tới API chỉ cần gọi endpoint dạng `/api/...`.

**Ưu điểm:**
- **Chạy 1 lần là xong**: Chỉ cần gõ lệnh `dotnet run`, cả giao diện Web và API đều khởi động.
- **Không lo lỗi CORS**: Vì Web và API chạy chung 1 tên miền (`localhost:5000`), bạn không cần cấu hình CORS phức tạp, không lo trình duyệt chặn.
- **Dễ dàng đóng gói (Deploy)**: Việc đưa lên server thật sau này vì tất cả nằm trong 1 khối duy nhất.

**Nhược điểm:**
- Nếu Frontend của bạn sau này phình to (dùng React, Next.js, Vue, v.v.), việc gộp chung có thể làm code base bị rối. Với HTML/JS/CSS thuần, phương pháp này rất gọn và tiện.

### 3. Cấu hình & Chạy ứng dụng WPF (HR Monitor)
Ứng dụng WPF đã được tích hợp sẵn HTTP Client và SignalR để kết nối realtime tới API (`http://localhost:5000`).

* **Cách khởi chạy**:
  1. Hãy đảm bảo **API đang được chạy** (ở bước 2).
  2. Mở file thư mục `HRMonitor` bằng **Visual Studio 2022**. (Bạn có thể mở qua file `.sln`).
  3. Nhấn **F5** (hoặc nút Start) để chạy ứng dụng WPF. 
  
  *(Hoặc bạn cũng có thể mở Terminal tại `HRMonitor\HRMonitor` và dùng lệnh `dotnet run`)*.

### 4. Tổng quan dự án Web App (EcgRecordAPI)
Dự án **EcgRecordAPI** được xây dựng theo mô hình **ASP.NET Core Web API (Minimal API)** kết hợp với khả năng phục vụ file tĩnh. Dưới đây là các đặc điểm chính:
* **Kiến trúc**: Sử dụng **Minimal API** của .NET (tức là định nghĩa các endpoint qua `app.MapGet()`, `app.MapPost()` ngay trong `Program.cs`) thay vì mô hình MVC (Model-View-Controller) hay Controller-based API truyền thống. Mô hình này giúp tối ưu hóa hiệu suất và giữ mã nguồn ngắn gọn nhất có thể.
* **Giao diện (Frontend)**: Tích hợp trực tiếp Frontend (chỉ dùng HTML, CSS, JS thuần) bằng middleware `app.UseStaticFiles()`. Các file giao diện tĩnh được đặt tại thư mục `wwwroot` và server sẽ tự động phục vụ giao diện này mà không cần đến View Engine như Razor Pages.
* **Cơ sở dữ liệu**: Truy xuất cơ sở dữ liệu SQL Server trực tiếp thông qua ADO.NET (`SqlConnection`, `SqlCommand`) để lấy/ghi dữ liệu với hiệu năng cao.
* **Giao tiếp Realtime**: Tích hợp **SignalR** để đẩy sự kiện tức thì (realtime) giữa ứng dụng Web của Bệnh nhân và ứng dụng Desktop WPF của Bác sĩ.

### 5. Thông tin Dữ liệu Mẫu (Sample Data)
Sau khi chạy script SQL, hệ thống sẽ được khởi tạo với dữ liệu mẫu như sau:

**Danh sách Bác sĩ (Đăng nhập qua Desktop App HRMonitor):**
*Cách đăng nhập: Nhập số **ID** vào ô Account và nhập Mật khẩu.*
1. **Bs. Jane Doe** (Account: `1`) - Mật khẩu: `2222`
2. **Bs. John Doe** (Account: `2`) - Mật khẩu: `1111`

**Danh sách Bệnh nhân (Đăng nhập qua Web `http://localhost:5000`):**
*Cách đăng nhập: Nhập chính xác **Họ và tên** vào ô Full Name và nhập Mật khẩu.*
- **Nguyen Van A** - Mật khẩu: `8392` (Được phân công cho Bs. John Doe)
- **Tran Thi B** - Mật khẩu: `1234` (Được phân công cho Bs. John Doe)
- **Le Van C** - Mật khẩu: `5678` (Được phân công cho Bs. John Doe)
- **Pham Thi D** - Mật khẩu: `4321` (Được phân công cho Bs. Jane Doe)
- **Hoang Van E** - Mật khẩu: `8765` (Được phân công cho Bs. Jane Doe)

**Quản trị viên (Admin) (Đăng nhập qua Desktop App HRMonitor):**
*Cách đăng nhập: Nhấn nút **Admin login** trên màn hình ứng dụng WPF để chuyển chế độ.*
- Account: `admin`
- Password: `admin`

**Phân bổ Bản ghi ECG (ECG Records):**
Hệ thống hiện có 15 bản ghi ECG mẫu, chia đều cho 5 bệnh nhân (mỗi người 3 bản ghi):
- **Bệnh nhân 1 (Nguyen Van A)**: Sở hữu các bản ghi ID 1, 2, 3 (Ví dụ: `Record_1_1.csv`)
- **Bệnh nhân 2 (Tran Thi B)**: Sở hữu các bản ghi ID 4, 5, 6
- **Bệnh nhân 3 (Le Van C)**: Sở hữu các bản ghi ID 7, 8, 9
- **Bệnh nhân 4 (Pham Thi D)**: Sở hữu các bản ghi ID 10, 11, 12
- **Bệnh nhân 5 (Hoang Van E)**: Sở hữu các bản ghi ID 13, 14, 15
