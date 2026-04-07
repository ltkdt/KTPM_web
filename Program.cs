using KTPM_Project;
using System.Text;
Console.OutputEncoding = Encoding.UTF8;
// 1. Chạy hàm phụ trước (Nếu bạn muốn nạp dữ liệu mới từ file CSV vào SQL)
DatabaseManager.ImportCsvToSql("data_input.csv");

Console.WriteLine("--- Đang thực hiện xuất dữ liệu cho Web ---");

// 2. Chạy hàm chính (Lấy từ SQL đổ ra file data.csv để Web hiển thị)
DatabaseManager.ExportSqlToCsv("data.csv");

Console.WriteLine("Xong rồi! Hãy mở file data.csv xem kết quả.");