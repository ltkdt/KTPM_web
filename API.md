# KẾ HOẠCH CẢI TIẾN: TÁCH RỜI API VÀ FRONTEND (SCALE-UP ARCHITECTURE)

Tài liệu này mô tả kế hoạch nâng cấp hệ thống `EcgRecordAPI` từ mô hình gộp chung (phục vụ giao diện trực tiếp) sang mô hình Client-Server phân tán hoàn toàn, nhằm mục đích dễ dàng scale (mở rộng) hệ thống trong tương lai.

---

## 1. Kiến trúc hệ thống sau khi tách

Khi tách rời, ASP.NET Core Project sẽ trở thành một **Pure Backend API (Headless Backend)**. Các client khác nhau (Web tĩnh, WPF Desktop, Mobile App sau này) sẽ đóng vai trò là những ứng dụng độc lập gọi dữ liệu (API Callers) thông qua HTTP/REST hoặc WebSockets (SignalR).

### Dự án ASP.NET sẽ tuân theo kiến trúc gì? Có cần MVC không?

- **KHÔNG CẦN MVC:** Bạn **không cần** và **không nên** sử dụng mô hình MVC (Model-View-Controller) truyền thống của ASP.NET. Mô hình MVC thường được dùng khi Server có nhiệm vụ render HTML (View) để trả về cho người dùng (ví dụ: Razor Pages). Ở kiến trúc mới, Server không quan tâm đến giao diện (View), nó chỉ trả về dữ liệu thuần túy (thường là định dạng JSON).
- **Kiến trúc áp dụng (API Design):**
  1. **Minimal API:** Nếu dự án giữ nguyên mức độ phức tạp hiện tại, tiếp tục dùng Minimal API (`app.MapGet`, `app.MapPost` trong `Program.cs`) là tốt nhất vì nó đem lại hiệu năng cực cao và gọn nhẹ.
  2. **Controller-based API:** Nếu số lượng Endpoint (API) lên tới hàng chục, hàng trăm, bạn nên chuyển sang mô hình Controller (tạo thư mục `Controllers`, kế thừa `ControllerBase`). 
  3. **Clean Architecture / N-Tier (Khuyến nghị để Scale lớn):** Nếu hệ thống lớn lên, mã nguồn ASP.NET nên được cấu trúc thành các lớp (Layers) như: 
     - *Presentation Layer* (Controllers / Minimal API Endpoints)
     - *Application Layer* (Chứa Logic nghiệp vụ)
     - *Infrastructure Layer* (Truy cập DB, kết nối hệ thống ngoài)
     - *Domain Layer* (Các Entity, Interface cơ lõi).

---

## 2. Các bước triển khai (Dự kiến)

Để tách hệ thống mà không phá vỡ logic cũ, các bước sau cần được thực hiện trong tương lai:

### Bước 1: Trích xuất Frontend
- Loại bỏ thư mục `wwwroot` (chứa HTML/CSS/JS) ra khỏi project `EcgRecordAPI`.
- Đưa mã nguồn Web Frontend thành một dự án (Repository) độc lập hoàn toàn. Frontend này có thể chỉ cần chạy bằng Live Server hoặc được nâng cấp lên các Framework mạnh mẽ hơn như React, Next.js, Vue.js.

### Bước 2: Gỡ bỏ Middleware phục vụ file tĩnh
- Xóa bỏ hoặc vô hiệu hóa cấu hình `app.UseStaticFiles()` trong `Program.cs`. API giờ đây sẽ chỉ phản hồi lại các request gửi tới `/api/...` hoặc các endpoint của SignalR.

### Bước 3: Cấu hình CORS (Cross-Origin Resource Sharing) - RẤT QUAN TRỌNG
- Do Frontend Web lúc này chạy ở một tên miền/cổng khác (VD: `http://localhost:3000`) so với API (VD: `http://localhost:5000`), trình duyệt sẽ chặn các API request vì lý do bảo mật.
- Cần thêm cấu hình CORS vào `Program.cs` của API để cấp phép cho Frontend gọi tới API:
  ```csharp
  builder.Services.AddCors(options => {
      options.AddPolicy("AllowFrontend", policy => {
          policy.WithOrigins("http://localhost:3000") // URL của Frontend độc lập
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); // Rất quan trọng nếu dùng SignalR
      });
  });
  // ...
  app.UseCors("AllowFrontend");
  ```

### Bước 4: Cập nhật cấu hình gọi API ở các Client
- **Web Frontend:** Sửa lại base URL trong các hàm `fetch()` (VD: từ `fetch('/api/patient')` thành `fetch('http://api-domain.com/api/patient')`).
- **WPF (HRMonitor):** Về cơ bản không thay đổi vì WPF vốn dĩ đã là một API Caller độc lập, chỉ cần trỏ connection string/base URL về địa chỉ mới của API là được.

---

## 3. Lợi ích khi thực hiện kiến trúc này

1. **Khả năng Mở rộng (Scale) độc lập:**
   - Nếu lượng Bệnh nhân truy cập Web tăng đột biến, bạn chỉ cần thuê thêm Server/Tài nguyên để host Frontend tĩnh qua CDN mà không ảnh hưởng tới Backend.
   - Ngược lại, nếu logic phân tích ECG cần xử lý nặng, bạn có thể tăng tài nguyên cho cụm Server API riêng biệt.
2. **Linh hoạt về Công nghệ UI:**
   - Do API và Frontend đã hoàn toàn "ly hôn", sau này bạn có thể đập bỏ bản Web HTML cũ và viết lại bằng React/Next.js mà không cần đụng đến code C# Backend.
3. **Bảo mật và Phân tải:**
   - Backend API có thể được đưa ra phía sau một API Gateway (như Nginx, YARP, Ocelot), ẩn danh hệ thống nội bộ, thêm cơ chế giới hạn lượng truy cập (Rate Limiting) hoặc cân bằng tải (Load Balancing).
