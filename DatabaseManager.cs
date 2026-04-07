using System.Text;
using System.Globalization;
using Microsoft.Data.SqlClient;
using CsvHelper; 

namespace KTPM_Project
{
    public class DatabaseManager
    {
        // Thay Sever = tên sever
        private static string connString = "Server=DESKTOP-58NU873\\SQLEXPRESS; Database=BenhVienDB; Integrated Security=True; TrustServerCertificate=True;";

        // --- HÀM CHÍNH: Lấy dữ liệu từ SQL và xuất ra CSV để Web hiển thị ---
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
                        sw.WriteLine("oi");
                        while (reader.Read())
                        {
                            sw.WriteLine(reader["oi"].ToString());
                        }
                        Console.WriteLine($"[SUCCESS] Đã xuất từ SQL ra {fileName}.");
                    }
                }
            }
            catch (Exception ex)
            { 
                Console.WriteLine("[ERROR Export]: " + ex.Message);
            }
        }

        // --- HÀM PHỤ: Đọc dữ liệu từ CSV và nạp ngược lại vào SQL ---
        public static void ImportCsvToSql(string fileName)
        {
            try
            {
                using (var reader = new StreamReader(fileName, Encoding.UTF8))
                using (var csv = new CsvHelper.CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<dynamic>();

                    using (SqlConnection conn = new SqlConnection(connString))
                    {
                        conn.Open();
                        // Xóa dữ liệu cũ để tránh trùng lặp
                        new SqlCommand("TRUNCATE TABLE DataBenhNhan", conn).ExecuteNonQuery();

                        foreach (var record in records)
                        {
                            string sql = "INSERT INTO DataBenhNhan (oi) VALUES (@oi)";
                            using (SqlCommand cmd = new SqlCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("@oi", double.Parse(record.oi.ToString()));
                                cmd.ExecuteNonQuery();
                            }
                        }
                        Console.WriteLine($"[SUCCESS] Đã nạp dữ liệu từ {fileName} vào SQL thành công.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR]: " + ex.Message);
            }
        }
    }
}