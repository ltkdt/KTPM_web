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

app.Run("http://localhost:5000");

public class EcgHub : Hub { }

public class ComplaintRequest { public int PatientId { get; set; } public int EcgRecordId { get; set; } public string Complaint { get; set; } }
public class FeedbackRequest { public int ConsultationId { get; set; } public int DoctorId { get; set; } public string Findings { get; set; } public string Treatment { get; set; } }