using CsvHelper;
using Microsoft.Data.SqlClient;
using System.Formats.Asn1;
using System.Globalization;
using System.Text;

// 1. Cấu hình
Console.OutputEncoding = Encoding.UTF8;

string csvPath = "data.csv";
//Thay sever = sever cần xuất dữ liệu csv vào
string connString = "Server= .; Database=BenhVienDB; Integrated Security=True; TrustServerCertificate=True;";

try
{
    using (var reader = new StreamReader(csvPath, Encoding.UTF8))
    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture)) //tránh lỗi định dạng
    {
        //Đọc dữ liệu từ CSV thành một danh sách (List) các hàng
        //Dynamic: Đọc trước để biế định dạng
        var records = csv.GetRecords<dynamic>();

        using (SqlConnection connection = new SqlConnection(connString))
        {
            connection.Open();
            Console.WriteLine("Đã kết nối SQL Server. Đang bắt đầu đẩy dữ liệu...");

            foreach (var record in records)
            {
                // 2. INSERT
                string sql = "INSERT INTO DataBenhNhan (oi) VALUES (@oi)";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    // Ép kiểu dữ liệu từ CSV sang kiểu số để khớp với SQL
                    command.Parameters.AddWithValue("@oi", double.Parse(record.oi)); //Đọc chuỗi, Parse sang double, add vào ô oi, @để tăng bảo mật
                    command.ExecuteNonQuery();
                }
            }
        }
    }
    Console.WriteLine("=> Đã đẩy toàn bộ dữ liệu từ CSV vào SQL Server thành công!");
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}