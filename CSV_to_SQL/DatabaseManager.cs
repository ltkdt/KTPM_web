using System.Text;
using System.Globalization;
using Microsoft.Data.SqlClient;
using CsvHelper; //Cần cài NuGet CsvHelper

namespace KTPM_Project
{
    public class DatabaseManager
    {
        private static string connString = @"Server=.; Database=HeThong_ECG; Integrated Security=True; TrustServerCertificate=True;";

        // --- Nạp dữ liệu từ CSV vào SQL  ---
        public static void ImportCsvToSql(string csvPath)
        {
            try
            {
                using (var reader = new StreamReader(csvPath, Encoding.UTF8))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    // Đọc dữ liệu từ CSV thành một danh sách (List) các hàng kiểu dynamic
                    var records = csv.GetRecords<dynamic>();

                    using (SqlConnection connection = new SqlConnection(connString))
                    {
                        connection.Open();
                        Console.WriteLine("Đã kết nối SQL Server. Đang bắt đầu đẩy dữ liệu...");

                        // Xóa dữ liệu cũ trước khi nạp mới
                        using (SqlCommand truncateCmd = new SqlCommand("TRUNCATE TABLE DataBenhNhan", connection))
                        {
                            truncateCmd.ExecuteNonQuery();
                        }

                        foreach (var record in records)
                        {
                            // 2. INSERT
                            string sql = "INSERT INTO DataBenhNhan (oi) VALUES (@oi)";

                            using (SqlCommand command = new SqlCommand(sql, connection))
                            {
                                // Ép kiểu dữ liệu từ CSV sang kiểu số để khớp với SQL
                                // Dùng .ToString() trước khi Parse để đảm bảo dynamic hoạt động tốt
                                command.Parameters.AddWithValue("@oi", double.Parse(record.oi.ToString()));

                                command.ExecuteNonQuery();
                            }
                        }
                        Console.WriteLine("=> Đã đẩy toàn bộ dữ liệu từ CSV vào SQL Server thành công!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        // --- Lấy dữ liệu từ SQL ra CSV ---
        public static void ExportSqlToCsv(string fileName)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    string sql = "SELECT oi FROM DataBenhNhan ORDER BY id ASC";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    using (StreamWriter sw = new StreamWriter(fileName, false, Encoding.UTF8))
                    {
                        sw.WriteLine("oi"); // Header
                        while (reader.Read())
                        {
                            sw.WriteLine(reader["oi"].ToString());
                        }
                        Console.WriteLine($"=> Đã xuất dữ liệu ra {fileName}.");
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Error: " + ex.Message); }
        }
    }
}