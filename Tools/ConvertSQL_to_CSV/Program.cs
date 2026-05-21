using KTPM_Project;
using System.Text;
Console.OutputEncoding = Encoding.UTF8;
// 1. Nạp dữ liệu mới từ CSL vào SQL 
DatabaseManager.ImportCsvToSql("data_input.csv");
Console.WriteLine("Hoàn thành công việc nạp dữ liệu");


// 2. Lấy dữ liệu từ SQL và xuất ra CSV để phục vụ cho Web
Console.WriteLine("--- Đang thực hiện xuất dữ liệu cho Web ---");
DatabaseManager.ExportSqlToCsv("data.csv");
Console.WriteLine("Hoàn thành công việc xuất dữ liệu");