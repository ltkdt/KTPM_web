# 🫀 TeleCardio - Nền tảng Y tế Từ xa: Cảnh báo Cấp cứu & Giám sát ECG

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-Desktop_App-blue)
![SignalR](https://img.shields.io/badge/SignalR-Real_Time-brightgreen)
![SQL Server](https://img.shields.io/badge/SQL_Server-Database-CC2927?style=flat&logo=microsoft-sql-server&logoColor=white)

**TeleCardio** là hệ thống giám sát Điện tâm đồ (ECG) định hướng sự kiện, áp dụng kiến trúc **Edge Computing** và **Lưu trữ/Chuyển tiếp (Store-and-Forward)** nhằm giải quyết bài toán nghẽn băng thông, cạn kiệt tài nguyên và hội chứng "mệt mỏi vì cảnh báo" trong các hệ thống IoT Y tế truyền thống.

![Demo TeleCardio](demo.png)

## ✨ Tính năng Nổi bật
* **🚑 Giao thức Cấp cứu Đa tầng (Smart SOS - Dead-man's Switch):** Tự động phát tín hiệu cấp cứu (SOS) về trạm bác sĩ nếu phát hiện nhịp tim nguy hiểm và bệnh nhân mất nhận thức (không phản hồi sau 60 giây).
* **⚡ Đồng bộ Real-time & Event-Driven:** Sử dụng SignalR để đẩy cảnh báo tức thời tới màn hình bác sĩ trực mà không cần refresh trang.
* **🗄️ Chiến lược Phân tách Dữ liệu (Data Separation):** Lưu trữ Metadata gọn nhẹ trên SQL Server, trong khi Dữ liệu thô (Raw Data ECG) được lưu dưới dạng file vật lý (`.csv` / `.json`), loại bỏ hoàn toàn rủi ro "nghẽn cổ chai" cơ sở dữ liệu.
* **📈 Render Đồ thị Hiệu năng cao:** Sử dụng Chart.js (trên Web Bệnh nhân) và ScottPlot (trên WPF Bác sĩ) để hiển thị mượt mà hàng ngàn điểm dữ liệu chuỗi thời gian (time-series).

---

## 🏗️ Cấu trúc Dự án

```text
TELECARDIO_REPO/
├── Backend-API/            # ASP.NET Core Web API 8.0 & SignalR Hub
├── Frontend-Patient/       # Web Client (HTML/JS/CSS) cho Bệnh nhân (Edge Device)
├── Desktop-Doctor/         # Ứng dụng WPF C# cho Bác sĩ (Trạm Triage)
├── Database/               # Chứa các Script khởi tạo SQL Server
└── Tools/                  # Các module phụ trợ (VD: ConvertSQL_to_CSV)
```

## ⚙️ Yêu cầu Môi trường (Prerequisites)
Để biên dịch và chạy toàn bộ hệ thống, máy tính cần cài đặt:
* .NET 8.0 SDK
* SQL Server (Bản Developer/Express) & SQL Server Management Studio (SSMS)
* Visual Studio 2022 (Để chạy WPF & Backend)
* Visual Studio Code kèm Extension Live Server (Để chạy Frontend Web)

---

## 🚀 Hướng Dẫn Cài Đặt Và Khởi Chạy Hệ Thống

### Bước 1: Cấu hình Chuỗi Kết Nối Cơ Sở Dữ Liệu (Database Connection)
Trước khi khởi chạy, hãy đảm bảo bạn đã cấu hình đúng chuỗi kết nối SQL Server trong mã nguồn C#:
```csharp
string connString = @"Server=TEN_SERVER_CUA_BAN\SQLEXPRESS; Database=BenhVienDB; Integrated Security=True; TrustServerCertificate=True;";
```

### Bước 2: Khởi Chạy Backend API
1. Mở **Terminal** hoặc **PowerShell** trên máy tính của bạn.
2. Di chuyển vào thư mục chứa dự án API và chạy các lệnh sau:
   ```bash
   cd Backend-API/API_EcgRecordAPI
   dotnet restore
   dotnet run
   ```
3. ⚠️ **Lưu ý Quan trọng:** Backend sẽ khởi chạy tại địa chỉ `http://localhost:5000`. Hãy giữ cửa sổ Terminal này luôn mở suốt quá trình test hệ thống (chạy ngầm).

### Bước 3: Khởi Chạy Client Trạm Bác Sĩ (Desktop WPF)
*Yêu cầu: Hãy đảm bảo Backend API ở Bước 2 đang hoạt động ổn định.*

1. Sử dụng **Visual Studio 2022** để mở file Solution (`.sln`) nằm trong thư mục `Desktop-Doctor/HRMonitor`.
2. Nhấn phím **F5** (hoặc click vào nút ▶️ **Start** trên thanh công cụ) để biên dịch và chạy ứng dụng.
3. Giao diện **Giám sát trực ban (Triage Dashboard)** sẽ hiện lên và tự động thiết lập kết nối SignalR với Server.

### Bước 4: Khởi Chạy Client Bệnh Nhân (Giao Diện Web)
1. Mở thư mục `Frontend-Patient` bằng trình soạn thảo **Visual Studio Code**.
2. Click chuột phải vào file `index.html` và chọn **Open with Live Server**.
3. Trang web sẽ tự động mở trên trình duyệt (thường chạy tại cổng `5500`). Tại đây, bệnh nhân có thể xem trực quan biểu đồ ECG cục bộ.

---

## 🧪 Kịch Bản Kiểm Thử Tính Năng Cấp Cứu (SOS Flow)

Để kiểm thử nghiệp vụ cốt lõi của phần mềm (Tính năng Store-and-Forward kết hợp Real-time Alarm), xin vui lòng làm theo các bước sau:

1. Đảm bảo cả **Màn hình Bác sĩ (WPF)** và **Web Bệnh nhân (HTML)** đang được mở song song trên màn hình.
2. Trên Web Bệnh nhân, hệ thống sẽ kích hoạt kịch bản mô phỏng nhịp tim (BPM) giảm đột ngột xuống dưới mức an toàn (`< 45 BPM`).
3. Màn hình Web sẽ hiển thị thông báo xác nhận: *"Bạn có ổn không?"*.
4. **Giả định bệnh nhân bị ngất xỉu:** Hãy giữ nguyên màn hình và không thực hiện bất kỳ thao tác gì trong vòng `60 giây`.
5. **Quan sát App WPF của Bác sĩ:** Hệ thống sẽ ngay lập tức chớp viền đỏ báo động khẩn cấp (nhờ tín hiệu thời gian thực từ SignalR) và tự động tải file *Raw Data* lên hệ thống để bác sĩ có thể chẩn đoán từ xa ngay lập tức.

---

## 🛠️ Công Cụ Phụ Trợ (Utilities)

### Data Migration Tool (ConvertSQL_to_CSV)
* **Vị trí:** Thư mục `Tools/ConvertSQL_to_CSV`
* **Mục đích:** Đây là công cụ hỗ trợ tính tương thích ngược cho hệ thống. Tool giúp đọc dữ liệu ECG cũ từ các cơ sở dữ liệu quan hệ (SQL Server) truyền thống, tiến hành phân tách (parse) thành định dạng cấu trúc `.csv` vật lý nhằm đáp ứng chuẩn lưu trữ mới (**Store-and-Forward**) của TeleCardio.
* **Cách vận hành:** Mở terminal tại thư mục của công cụ này và thực hiện lệnh:
  ```bash
  dotnet run
  ```