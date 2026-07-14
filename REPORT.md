## 1. Tổng quan hệ thống

Hệ thống theo dõi ECG và tư vấn cho bệnh nhân được thiết kế bao gồm 4 thành phần chính:

1. **Backend API (`API/EcgRecordAPI`):** Đóng vai trò xử lý trung tâm, cung cấp các API để truy xuất và thao tác với cơ sở dữ liệu.
2. **Web Frontend:** Ứng dụng Web phục vụ giao diện cho **Bệnh nhân**. Dựa theo giả định kiến trúc mới, Frontend này hoạt động hoàn toàn tách rời và độc lập với Backend API.
3. **Hardware (Thiết bị IoT):** Phân hệ mô phỏng phần cứng IoT có nhiệm vụ thu thập dữ liệu nhịp tim/điện tâm đồ từ bệnh nhân và gửi tự động về hệ thống thông qua API.
4. **Desktop App (`HRMonitor`):** Ứng dụng quản lý chạy nền tảng WPF phía **Bác sĩ**. Cung cấp giao diện trực quan giúp bác sĩ quản lý danh sách bệnh nhân, theo dõi biểu đồ ECG chi tiết và cập nhật các dữ liệu chẩn đoán lên hệ thống.

### 1.1. Yêu cầu chức năng

Hệ thống theo dõi ECG và tư vấn cho bệnh nhân cần đáp ứng các chức năng chính nhằm phục vụ hiệu quả công tác thu thập, lưu trữ, truy xuất và phân tích thông tin liên quan đến sức khỏe tim mạch của bệnh nhân, dẫn đến yêu cầu về chức năng như sau:

#### 1.1.1. Quản trị hệ thống

Các chức năng phục vụ công tác quản trị, phân quyền và theo dõi hoạt động của hệ thống:

| Hạng mục | Chức năng chi tiết |
| :--- | :--- |
| QUẢN LÝ NGƯỜI DÙNG | Quản lý danh sách, tìm kiếm và cập nhật trạng thái của các tài khoản (Quản trị viên, Bác sĩ, Bệnh nhân). |
| PHÂN QUYỀN TRUY CẬP | Đảm bảo giới hạn quyền truy cập: Quản trị viên quản lý chung, Bác sĩ theo dõi bệnh nhân được phân công, Bệnh nhân xem dữ liệu cá nhân. |

#### 1.1.2. Quản lý CSDL Y tế và Điện tâm đồ (ECG)

Các chức năng nghiệp vụ chuyên môn phục vụ theo dõi dữ liệu y tế:

| Phân hệ dữ liệu | Nội dung quản lý |
| :--- | :--- |
| HỒ SƠ BỆNH NHÂN | Quản lý thông tin cá nhân, lịch sử khám; Hiển thị danh sách bệnh nhân thuộc phạm vi quản lý của từng bác sĩ. |
| HỒ SƠ BÁC SĨ | Quản lý thông tin nghiệp vụ của bác sĩ, số lượng bệnh nhân đang phụ trách. |
| BẢN GHI ECG | Lưu trữ và quản lý chi tiết các bản ghi ECG của từng bệnh nhân; Phân tích và hiển thị biểu đồ điện tâm đồ; Đồng bộ dữ liệu bản ghi realtime. |

### 1.2. Yêu cầu phi chức năng

Hệ thống tuân thủ các chuẩn mực về công nghệ và chất lượng dịch vụ nhằm đảm bảo sự ổn định, tốc độ và tính bảo mật cao trong môi trường y tế.

**KIẾN TRÚC CÔNG NGHỆ**
- *Ngôn ngữ lập trình:* C# (.NET 8.0), HTML/CSS/JS thuần, XAML
- *Nền tảng phát triển:* ASP.NET Core Web API (Minimal API), WPF (Desktop App)
- *Hệ quản trị CSDL:* Microsoft SQL Server

#### 1.2.1. Tiêu chuẩn Chất lượng & Vận hành

| Tiêu chí | Mô tả chi tiết |
| :--- | :--- |
| HIỆU NĂNG | Truy xuất dữ liệu tối ưu với ADO.NET; Hệ thống giao tiếp thời gian thực (SignalR) độ trễ thấp đảm bảo thông tin ECG được cập nhật tức thì. |
| AN TOÀN BẢO MẬT | Bảo mật dữ liệu y tế chặt chẽ: Bệnh nhân chỉ xem được hồ sơ của mình; Bác sĩ chỉ truy cập được dữ liệu bệnh nhân được phân công (Rào chắn xác thực quyền). |
| TRẢI NGHIỆM (UI/UX) | Tương thích đa nền tảng: Giao diện Web thân thiện cho bệnh nhân; Desktop App (WPF) mạnh mẽ, trực quan cho bác sĩ để phân tích biểu đồ ECG. |
| TÍNH ĐỒNG BỘ | Mọi thay đổi dữ liệu từ phía Web (Bệnh nhân) hoặc Desktop (Bác sĩ) đều được cập nhật và phản hồi đồng bộ trên toàn hệ thống 24/7. |
| KHẢ NĂNG MỞ RỘNG | Kiến trúc tách bạch giữa API và UI tĩnh cho phép dễ dàng tích hợp, nâng cấp sang các Framework Frontend hiện đại (React/Next.js) khi cần. |
