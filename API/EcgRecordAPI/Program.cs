using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.SetIsOriginAllowed(_ => true) 
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); 
    });
});
builder.Services.AddSignalR();

var app = builder.Build();
var options = new DefaultFilesOptions();
options.DefaultFileNames.Clear();
options.DefaultFileNames.Add("login.html");
app.UseDefaultFiles(options);
app.UseStaticFiles();
app.UseCors("AllowAll");

string connString = @"Server=localhost\SQLEXPRESS; Database=BenhVienDB; Integrated Security=True; TrustServerCertificate=True;";  //SỬA TÊN SERVER CHO PHÙ HỢP

app.MapHub<EcgHub>("/ecghub");

// 1. WEB: Gửi Complaint lên
app.MapPost("/api/patient/complaint", async (ComplaintRequest req, IHubContext<EcgHub> hubContext) => {
    using SqlConnection conn = new SqlConnection(connString); await conn.OpenAsync();
    string sql = @"INSERT INTO Consultations (PatientId, EcgRecordId, PatientComplaint, Status) 
                   VALUES (@PatientId, @EcgRecordId, @Complaint, 'Pending')";
    using SqlCommand cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@PatientId", req.PatientId);
    cmd.Parameters.AddWithValue("@EcgRecordId", req.EcgRecordId);
    cmd.Parameters.AddWithValue("@Complaint", req.Complaint);
    await cmd.ExecuteNonQueryAsync();

    await hubContext.Clients.All.SendAsync("PatientSentComplaint", req.EcgRecordId);

    return Results.Ok();
});

// 2. WPF & WEB: Lấy danh sách
app.MapGet("/api/records/{patientId}", (int patientId) => {
    var list = new List<object>();
    using SqlConnection conn = new SqlConnection(connString); conn.Open();
    string sql = @"
        SELECT e.Id AS EcgId, e.RecordName, c.Id AS ConsultationId, 
               c.PatientComplaint, c.DoctorFindings, c.DoctorTreatment, c.Status
        FROM EcgRecords e
        LEFT JOIN (
            SELECT *, ROW_NUMBER() OVER(PARTITION BY EcgRecordId ORDER BY Id DESC) as rn 
            FROM Consultations
        ) c ON e.Id = c.EcgRecordId AND c.rn = 1
        WHERE e.PatientId = @patientId
        ORDER BY e.Id ASC";

    using SqlCommand cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@patientId", patientId);
    using SqlDataReader reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        list.Add(new
        {
            EcgId = reader["EcgId"],
            RecordName = reader["RecordName"].ToString(),
            ConsultationId = reader["ConsultationId"] != DBNull.Value ? reader["ConsultationId"] : 0,
            Complaint = reader["PatientComplaint"]?.ToString() ?? "",
            Findings = reader["DoctorFindings"]?.ToString() ?? "",
            Treatment = reader["DoctorTreatment"]?.ToString() ?? "",
            Status = reader["Status"]?.ToString() ?? "Chưa tư vấn"
        });
    }
    return Results.Ok(list);
});

// API endpoint để lấy file CSV theo EcgRecordId
app.MapGet("/api/records/csv/{id}", (int id) => {
    using SqlConnection conn = new SqlConnection(connString); conn.Open();
    string sql = "SELECT RecordName FROM EcgRecords WHERE Id = @id";
    using SqlCommand cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);
    string filePath = cmd.ExecuteScalar()?.ToString();
    
    if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
        return Results.NotFound("CSV file not found.");
    
    return Results.File(filePath, "text/csv");
});

// 3. WPF: Bác sĩ gửi lời khuyên
app.MapPost("/api/doctor/feedback", async (FeedbackRequest req, IHubContext<EcgHub> hubContext) => {
    using SqlConnection conn = new SqlConnection(connString); await conn.OpenAsync();
    string sql = @"UPDATE Consultations SET DoctorId = @DoctorId, DoctorFindings = @Findings, 
                   DoctorTreatment = @Treatment, Status = 'Responded', RespondedAt = GETDATE()
                   WHERE Id = @ConsultationId";
    using SqlCommand cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@DoctorId", req.DoctorId);
    cmd.Parameters.AddWithValue("@Findings", req.Findings);
    cmd.Parameters.AddWithValue("@Treatment", req.Treatment);
    cmd.Parameters.AddWithValue("@ConsultationId", req.ConsultationId);
    await cmd.ExecuteNonQueryAsync();

    await hubContext.Clients.All.SendAsync("DoctorSentFeedback");

    return Results.Ok();
});



// API Login
app.MapPost("/api/login", async (LoginRequest req) => {
    using SqlConnection conn = new SqlConnection(connString);
    await conn.OpenAsync();
    string sql = "SELECT Id, FullName FROM Patients WHERE FullName = @FullName AND Password = @Password";
    using SqlCommand cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@FullName", req.FullName);
    cmd.Parameters.AddWithValue("@Password", req.Password);
    
    using SqlDataReader reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        return Results.Ok(new { PatientId = reader["Id"], FullName = reader["FullName"].ToString() });
    }
    return Results.Unauthorized();
});

// API Register
app.MapPost("/api/register", async (RegisterRequest req) => {
    using SqlConnection conn = new SqlConnection(connString);
    await conn.OpenAsync();
    
    string checkSql = "SELECT COUNT(1) FROM Patients WHERE FullName = @FullName";
    using SqlCommand checkCmd = new SqlCommand(checkSql, conn);
    checkCmd.Parameters.AddWithValue("@FullName", req.FullName);
    int count = (int)await checkCmd.ExecuteScalarAsync();
    if (count > 0) return Results.BadRequest("User already exists.");

    string sql = @"INSERT INTO Patients (FullName, Age, Gender, PhoneNumber, Email, Address, Password) 
                   OUTPUT INSERTED.Id 
                   VALUES (@FullName, @Age, @Gender, @PhoneNumber, @Email, @Address, @Password)";
    using SqlCommand cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@FullName", req.FullName);
    cmd.Parameters.AddWithValue("@Age", req.Age);
    cmd.Parameters.AddWithValue("@Gender", req.Gender);
    cmd.Parameters.AddWithValue("@PhoneNumber", req.PhoneNumber);
    cmd.Parameters.AddWithValue("@Email", req.Email);
    cmd.Parameters.AddWithValue("@Address", req.Address);
    cmd.Parameters.AddWithValue("@Password", req.Password);
    
    int newId = (int)await cmd.ExecuteScalarAsync();
    return Results.Ok(new { PatientId = newId, FullName = req.FullName });
});

// API Lấy danh sách Patients
app.MapGet("/api/patients", async () => {
    var list = new List<object>();
    using SqlConnection conn = new SqlConnection(connString);
    await conn.OpenAsync();
    string sql = "SELECT Id, FullName, Age, Gender, PhoneNumber, Email, Address, DoctorId FROM Patients";
    using SqlCommand cmd = new SqlCommand(sql, conn);
    using SqlDataReader reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        list.Add(new {
            Id = reader["Id"],
            Name = reader["FullName"].ToString(),
            Age = reader["Age"] != DBNull.Value ? Convert.ToInt32(reader["Age"]) : 0,
            Gender = reader["Gender"].ToString(),
            PhoneNumber = reader["PhoneNumber"].ToString(),
            Email = reader["Email"].ToString(),
            Address = reader["Address"].ToString(),
            DoctorId = reader["DoctorId"] != DBNull.Value ? Convert.ToInt32(reader["DoctorId"]) : (int?)null
        });
    }
    return Results.Ok(list);
});

// API Gán bác sĩ cho bệnh nhân
app.MapPost("/api/patients/{patientId}/assign-doctor", async (int patientId, AssignDoctorRequest req) => {
    using SqlConnection conn = new SqlConnection(connString);
    await conn.OpenAsync();
    string sql = "UPDATE Patients SET DoctorId = @DoctorId WHERE Id = @PatientId";
    using SqlCommand cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@DoctorId", req.DoctorId.HasValue ? (object)req.DoctorId.Value : DBNull.Value);
    cmd.Parameters.AddWithValue("@PatientId", patientId);
    int rows = await cmd.ExecuteNonQueryAsync();
    if (rows > 0) return Results.Ok();
    return Results.NotFound();
});

// API Lấy danh sách Doctors
app.MapGet("/api/doctors", async () => {
    var list = new List<object>();
    using SqlConnection conn = new SqlConnection(connString);
    await conn.OpenAsync();
    string sql = "SELECT Id, FullName, Specialty, Username, Password, Age, Gender, PhoneNumber, Hospital, Email, Address FROM Doctors";
    using SqlCommand cmd = new SqlCommand(sql, conn);
    using SqlDataReader reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        list.Add(new {
            Id = reader["Id"],
            FullName = reader["FullName"].ToString(),
            Specialty = reader["Specialty"].ToString(),
            Username = reader["Username"]?.ToString(),
            Password = reader["Password"]?.ToString(),
            Age = reader["Age"] != DBNull.Value ? Convert.ToInt32(reader["Age"]) : 0,
            Gender = reader["Gender"]?.ToString() ?? "",
            PhoneNumber = reader["PhoneNumber"]?.ToString() ?? "",
            Hospital = reader["Hospital"]?.ToString() ?? "",
            Email = reader["Email"]?.ToString() ?? "",
            Address = reader["Address"]?.ToString() ?? ""
        });
    }
    return Results.Ok(list);
});

// API Delete Patient
app.MapDelete("/api/patients/{id}", async (int id) => {
    using SqlConnection conn = new SqlConnection(connString);
    await conn.OpenAsync();
    string sql = @"
        DELETE FROM Consultations WHERE PatientId = @id;
        DELETE FROM EcgRecords WHERE PatientId = @id;
        DELETE FROM PatientDevices WHERE PatientId = @id;
        DELETE FROM Patients WHERE Id = @id;
    ";
    using SqlCommand cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok();
});

// API Delete Doctor
app.MapDelete("/api/doctors/{id}", async (int id) => {
    using SqlConnection conn = new SqlConnection(connString);
    await conn.OpenAsync();
    string sql = @"
        UPDATE Patients SET DoctorId = NULL WHERE DoctorId = @id;
        UPDATE Consultations SET DoctorId = NULL WHERE DoctorId = @id;
        DELETE FROM Doctors WHERE Id = @id;
    ";
    using SqlCommand cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok();
});

// API ẨN DÙNG ĐỂ RESET DATABASE CHO MỚI
app.MapGet("/api/reset-database", async () => {
    using SqlConnection conn = new SqlConnection(connString);
    await conn.OpenAsync();

    // Xóa dữ liệu và reset ID
    string sql = @"
        DELETE FROM Consultations;
        DBCC CHECKIDENT('Consultations', RESEED, 0);
		";

    using SqlCommand cmd = new SqlCommand(sql, conn);
    await cmd.ExecuteNonQueryAsync();

    return Results.Ok("Đã làm sạch toàn bộ dữ liệu bảng Consultations. App đã như mới!");
});

Console.WriteLine("=====================================================");
Console.WriteLine("🚀 EcgRecordAPI đã khởi chạy thành công!");
Console.WriteLine("👉 Hãy mở trình duyệt và truy cập: http://localhost:5000");
Console.WriteLine("=====================================================");

app.Run("http://localhost:5000");

public class EcgHub : Hub { }

public class ComplaintRequest { public int PatientId { get; set; } public int EcgRecordId { get; set; } public string Complaint { get; set; } }
public class FeedbackRequest { public int ConsultationId { get; set; } public int DoctorId { get; set; } public string Findings { get; set; } public string Treatment { get; set; } }
public class LoginRequest { public string FullName { get; set; } public string Password { get; set; } }
public class AssignDoctorRequest { public int? DoctorId { get; set; } }
public class RegisterRequest { public string FullName { get; set; } public int Age { get; set; } public string Gender { get; set; } public string PhoneNumber { get; set; } public string Email { get; set; } public string Address { get; set; } public string Password { get; set; } }