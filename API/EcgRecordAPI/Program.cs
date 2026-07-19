using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "ClientApplications";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5000", "https://localhost:5001"];
builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddSignalR();

var app = builder.Build();
app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new { message = "Đã xảy ra lỗi máy chủ không mong muốn." });
}));
app.UseCors(CorsPolicy);

var defaultFiles = new DefaultFilesOptions();
defaultFiles.DefaultFileNames.Clear();
defaultFiles.DefaultFileNames.Add("login.html");
app.UseDefaultFiles(defaultFiles);
app.UseStaticFiles();

var connectionString = builder.Configuration.GetConnectionString("HeThongEcg")
    ?? throw new InvalidOperationException("ConnectionStrings:HeThongEcg is not configured.");
var csvDirectory = builder.Configuration["Storage:EcgCsvPath"];
if (string.IsNullOrWhiteSpace(csvDirectory))
    csvDirectory = Path.Combine(app.Environment.ContentRootPath, "App_Data", "ecg");
csvDirectory = Path.GetFullPath(csvDirectory);
Directory.CreateDirectory(csvDirectory);

app.MapHub<EcgHub>("/ecghub");
app.MapGet("/api/health", async () =>
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new SqlCommand("SELECT 1", connection);
    await command.ExecuteScalarAsync();
    return Results.Ok(new { status = "healthy" });
});

app.MapPost("/api/login", async (PatientLoginRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { message = "Họ tên và mật khẩu là bắt buộc." });

    const string sql = """
        SELECT bn.Id, bn.HoTen
        FROM BenhNhan bn INNER JOIN TaiKhoan tk ON bn.TaiKhoanId = tk.Id
        WHERE bn.HoTen = @FullName AND tk.MatKhau = @Password AND tk.TrangThai = 'ACTIVE';
        """;
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new SqlCommand(sql, connection);
    command.Parameters.Add("@FullName", SqlDbType.NVarChar, 100).Value = request.FullName.Trim();
    command.Parameters.Add("@Password", SqlDbType.VarChar, 255).Value = request.Password;
    await using var reader = await command.ExecuteReaderAsync();
    return await reader.ReadAsync()
        ? Results.Ok(new { patientId = reader.GetInt32(0), fullName = reader.GetString(1) })
        : Results.Unauthorized();
});

app.MapPost("/api/doctors/login", async (DoctorLoginRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { message = "Tên đăng nhập và mật khẩu là bắt buộc." });

    const string sql = """
        SELECT bs.Id, bs.HoTen
        FROM BacSi bs INNER JOIN TaiKhoan tk ON bs.TaiKhoanId = tk.Id
        WHERE tk.TenDangNhap = @Username AND tk.MatKhau = @Password AND tk.TrangThai = 'ACTIVE';
        """;
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new SqlCommand(sql, connection);
    command.Parameters.Add("@Username", SqlDbType.VarChar, 50).Value = request.Username.Trim();
    command.Parameters.Add("@Password", SqlDbType.VarChar, 255).Value = request.Password;
    await using var reader = await command.ExecuteReaderAsync();
    return await reader.ReadAsync()
        ? Results.Ok(new { doctorId = reader.GetInt32(0), fullName = reader.GetString(1) })
        : Results.Unauthorized();
});

app.MapPost("/api/register", async (RegisterRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { message = "Họ tên và mật khẩu là bắt buộc." });
    if (request.Age is < 0 or > 150)
        return Results.BadRequest(new { message = "Tuổi phải nằm trong khoảng từ 0 đến 150." });

    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    try
    {
        const string accountSql = """
            INSERT INTO TaiKhoan (TenDangNhap, MatKhau, QuyenId, TrangThai)
            OUTPUT INSERTED.Id
            SELECT @FullName, @Password, Id, 'ACTIVE' FROM Quyen WHERE TenQuyen = 'BENHNHAN';
            """;
        await using var accountCommand = new SqlCommand(accountSql, connection, transaction);
        accountCommand.Parameters.Add("@FullName", SqlDbType.NVarChar, 100).Value = request.FullName.Trim();
        accountCommand.Parameters.Add("@Password", SqlDbType.VarChar, 255).Value = request.Password;
        var accountId = Convert.ToInt32(await accountCommand.ExecuteScalarAsync());

        const string patientSql = """
            INSERT INTO BenhNhan (TaiKhoanId, HoTen, Tuoi, GioiTinh, SoDienThoai, Email, DiaChi)
            OUTPUT INSERTED.Id VALUES (@AccountId, @FullName, @Age, @Gender, @Phone, @Email, @Address);
            """;
        await using var patientCommand = new SqlCommand(patientSql, connection, transaction);
        patientCommand.Parameters.Add("@AccountId", SqlDbType.Int).Value = accountId;
        patientCommand.Parameters.Add("@FullName", SqlDbType.NVarChar, 100).Value = request.FullName.Trim();
        patientCommand.Parameters.Add("@Age", SqlDbType.Int).Value = request.Age;
        patientCommand.Parameters.Add("@Gender", SqlDbType.NVarChar, 10).Value = DbValue(request.Gender);
        patientCommand.Parameters.Add("@Phone", SqlDbType.VarChar, 15).Value = DbValue(request.PhoneNumber);
        patientCommand.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = DbValue(request.Email);
        patientCommand.Parameters.Add("@Address", SqlDbType.NVarChar, -1).Value = DbValue(request.Address);
        var patientId = Convert.ToInt32(await patientCommand.ExecuteScalarAsync());
        await transaction.CommitAsync();
        return Results.Created($"/api/patients/{patientId}", new { patientId, fullName = request.FullName.Trim() });
    }
    catch (SqlException ex) when (ex.Number is 2601 or 2627)
    {
        await transaction.RollbackAsync();
        return Results.Conflict(new { message = "Bệnh nhân có họ tên này đã tồn tại." });
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
});

app.MapGet("/api/patients", async () =>
{
    const string sql = "SELECT Id, HoTen, Tuoi, GioiTinh, SoDienThoai, Email, DiaChi, BacSiId FROM BenhNhan ORDER BY HoTen";
    var patients = new List<object>();
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new SqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        patients.Add(new { id = reader.GetInt32(0), name = reader.GetString(1), age = GetNullableInt(reader, 2), gender = GetString(reader, 3), phoneNumber = GetString(reader, 4), email = GetString(reader, 5), address = GetString(reader, 6), doctorId = GetNullableInt(reader, 7) });
    return Results.Ok(patients);
});

app.MapGet("/api/doctors", async () =>
{
    const string sql = "SELECT Id, HoTen, ChuyenKhoa, SoDienThoai, Email, SoNamKinhNghiem FROM BacSi ORDER BY HoTen";
    var doctors = new List<object>();
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new SqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        doctors.Add(new { id = reader.GetInt32(0), fullName = reader.GetString(1), specialty = GetString(reader, 2), phoneNumber = GetString(reader, 3), email = GetString(reader, 4), yearsOfExperience = GetNullableInt(reader, 5) });
    return Results.Ok(doctors);
});

app.MapPut("/api/patients/{patientId:int}/doctor", async (int patientId, AssignDoctorRequest request) =>
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    if (request.DoctorId is not null)
    {
        await using var doctorCheck = new SqlCommand("SELECT COUNT(1) FROM BacSi WHERE Id = @DoctorId", connection);
        doctorCheck.Parameters.Add("@DoctorId", SqlDbType.Int).Value = request.DoctorId;
        if (Convert.ToInt32(await doctorCheck.ExecuteScalarAsync()) == 0) return Results.BadRequest(new { message = "Không tìm thấy bác sĩ." });
    }
    await using var command = new SqlCommand("UPDATE BenhNhan SET BacSiId = @DoctorId WHERE Id = @PatientId", connection);
    command.Parameters.Add("@DoctorId", SqlDbType.Int).Value = request.DoctorId is null ? DBNull.Value : request.DoctorId;
    command.Parameters.Add("@PatientId", SqlDbType.Int).Value = patientId;
    return await command.ExecuteNonQueryAsync() == 0 ? Results.NotFound() : Results.NoContent();
});
// Compatibility route used by the existing desktop client.
app.MapPost("/api/patients/{patientId:int}/assign-doctor", async (int patientId, AssignDoctorRequest request) => await SetDoctor(patientId, request, connectionString));

app.MapGet("/api/records/{patientId:int}", async (int patientId) =>
{
    const string sql = """
        SELECT d.Id, d.DuongDanCsv, d.NhipTim, d.Rmssd, d.ThoiGianDo, d.MucDoCanhBao,
               c.Id, c.TrieuChung, c.ChanDoan, c.PhacDoDieuTri, c.TrangThai
        FROM DuLieuECG d
        OUTER APPLY (SELECT TOP 1 * FROM CaTuVan WHERE DuLieuECGId = d.Id ORDER BY Id DESC) c
        WHERE d.BenhNhanId = @PatientId ORDER BY d.ThoiGianDo DESC, d.Id DESC;
        """;
    var records = new List<object>();
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new SqlCommand(sql, connection);
    command.Parameters.Add("@PatientId", SqlDbType.Int).Value = patientId;
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        records.Add(new { ecgId = reader.GetInt32(0), recordName = GetString(reader, 1), nhipTim = GetNullableInt(reader, 2), rmssd = GetNullableDouble(reader, 3), measuredAt = reader.GetDateTime(4), alertLevel = GetString(reader, 5), consultationId = GetNullableInt(reader, 6) ?? 0, complaint = GetString(reader, 7), findings = GetString(reader, 8), treatment = GetString(reader, 9), status = GetString(reader, 10, "NotConsulted") });
    return Results.Ok(records);
});

app.MapGet("/api/records/{recordId:int}/csv", async (int recordId) =>
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new SqlCommand("SELECT DuongDanCsv FROM DuLieuECG WHERE Id = @Id", connection);
    command.Parameters.Add("@Id", SqlDbType.Int).Value = recordId;
    var storedPath = (string?)await command.ExecuteScalarAsync();
    if (string.IsNullOrWhiteSpace(storedPath) || !File.Exists(storedPath)) return Results.NotFound(new { message = "Không tìm thấy tệp CSV." });
    return Results.File(storedPath, "text/csv", enableRangeProcessing: true);
});
// Old route retained for browser clients already deployed.
app.MapGet("/api/records/csv/{recordId:int}", async (int recordId) => await GetCsv(recordId, connectionString));

app.MapPost("/api/patient/complaint", async (ComplaintRequest request, IHubContext<EcgHub> hub) =>
{
    if (string.IsNullOrWhiteSpace(request.Complaint)) return Results.BadRequest(new { message = "Vui lòng nhập triệu chứng." });
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    const string sql = """
        INSERT INTO CaTuVan (BenhNhanId, BacSiId, DuLieuECGId, TrieuChung, TrangThai)
        OUTPUT INSERTED.Id
        SELECT b.Id, b.BacSiId, e.Id, @Complaint, 'PENDING'
        FROM BenhNhan b INNER JOIN DuLieuECG e ON e.Id = @RecordId AND e.BenhNhanId = b.Id
        WHERE b.Id = @PatientId;
        """;
    await using var command = new SqlCommand(sql, connection);
    command.Parameters.Add("@PatientId", SqlDbType.Int).Value = request.PatientId;
    command.Parameters.Add("@RecordId", SqlDbType.Int).Value = request.EcgRecordId;
    command.Parameters.Add("@Complaint", SqlDbType.NVarChar, -1).Value = request.Complaint.Trim();
    var consultationId = await command.ExecuteScalarAsync();
    if (consultationId is null) return Results.BadRequest(new { message = "Bản ghi ECG không thuộc bệnh nhân này." });
    await hub.Clients.All.SendAsync("PatientSentComplaint", request.EcgRecordId);
    return Results.Created($"/api/consultations/{consultationId}", new { consultationId });
});

app.MapPost("/api/doctor/feedback", async (FeedbackRequest request, IHubContext<EcgHub> hub) =>
{
    if (string.IsNullOrWhiteSpace(request.Findings) || string.IsNullOrWhiteSpace(request.Treatment)) return Results.BadRequest(new { message = "Vui lòng nhập nhận xét và phác đồ điều trị." });
    const string sql = """
        UPDATE CaTuVan SET BacSiId = @DoctorId, ChanDoan = @Findings, PhacDoDieuTri = @Treatment,
            TrangThai = 'RESPONDED', ThoiGianPhanHoi = GETDATE()
        WHERE Id = @ConsultationId AND EXISTS (SELECT 1 FROM BacSi WHERE Id = @DoctorId);
        """;
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new SqlCommand(sql, connection);
    command.Parameters.Add("@DoctorId", SqlDbType.Int).Value = request.DoctorId;
    command.Parameters.Add("@Findings", SqlDbType.NVarChar, -1).Value = request.Findings.Trim();
    command.Parameters.Add("@Treatment", SqlDbType.NVarChar, -1).Value = request.Treatment.Trim();
    command.Parameters.Add("@ConsultationId", SqlDbType.Int).Value = request.ConsultationId;
    if (await command.ExecuteNonQueryAsync() == 0) return Results.NotFound(new { message = "Không tìm thấy ca tư vấn hoặc bác sĩ." });
    await hub.Clients.All.SendAsync("DoctorSentFeedback", request.ConsultationId);
    return Results.NoContent();
});

app.MapPost("/api/devices/link", async (DeviceLinkRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.MacAddress)) return Results.BadRequest(new { message = "Địa chỉ MAC là bắt buộc." });
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    try
    {
        await using var patientCheck = new SqlCommand("SELECT COUNT(1) FROM BenhNhan WHERE Id = @Id", connection, transaction);
        patientCheck.Parameters.Add("@Id", SqlDbType.Int).Value = request.PatientId;
        if (Convert.ToInt32(await patientCheck.ExecuteScalarAsync()) == 0) return Results.NotFound(new { message = "Không tìm thấy bệnh nhân." });
        await using var deviceCheck = new SqlCommand("SELECT COUNT(1) FROM ThietBiEcg WHERE MaThietBi = @MacAddress AND TrangThai <> 'INACTIVE'", connection, transaction);
        deviceCheck.Parameters.Add("@MacAddress", SqlDbType.NVarChar, 100).Value = request.MacAddress.Trim();
        if (Convert.ToInt32(await deviceCheck.ExecuteScalarAsync()) == 0) return Results.BadRequest(new { message = "Thiết bị chưa được Admin khai báo hoặc đã bị ngưng hoạt động." });
        await using var remove = new SqlCommand("DELETE FROM ThietBiBenhNhan WHERE MaThietBi = @MacAddress", connection, transaction);
        remove.Parameters.Add("@MacAddress", SqlDbType.NVarChar, 100).Value = request.MacAddress.Trim();
        await remove.ExecuteNonQueryAsync();
        await using var insert = new SqlCommand("INSERT INTO ThietBiBenhNhan (BenhNhanId, MaThietBi) VALUES (@PatientId, @MacAddress)", connection, transaction);
        insert.Parameters.Add("@PatientId", SqlDbType.Int).Value = request.PatientId;
        insert.Parameters.Add("@MacAddress", SqlDbType.NVarChar, 100).Value = request.MacAddress.Trim();
        await insert.ExecuteNonQueryAsync();
        await using var markAssigned = new SqlCommand("UPDATE ThietBiEcg SET TrangThai = 'ASSIGNED' WHERE MaThietBi = @MacAddress", connection, transaction);
        markAssigned.Parameters.Add("@MacAddress", SqlDbType.NVarChar, 100).Value = request.MacAddress.Trim();
        await markAssigned.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
        return Results.Ok(new { success = true, message = "Đã liên kết thiết bị thành công." });
    }
    catch { await transaction.RollbackAsync(); throw; }
});

app.MapPost("/api/records/upload", async (EcgUploadRequest request, IHubContext<EcgHub> hub) =>
{
    if (string.IsNullOrWhiteSpace(request.MacAddress) || string.IsNullOrWhiteSpace(request.FileJson)) return Results.BadRequest(new { message = "Địa chỉ MAC và dữ liệu ECG là bắt buộc." });
    if (request.NhipTim is < 0 or > 300 || request.Rmssd < 0 || double.IsInfinity(request.Rmssd) || double.IsNaN(request.Rmssd)) return Results.BadRequest(new { message = "Chỉ số ECG không hợp lệ." });
    if (!TryBuildCsv(request.FileJson, out var csv, out var error)) return Results.BadRequest(new { message = error });

    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var patientCommand = new SqlCommand("SELECT BenhNhanId FROM ThietBiBenhNhan WHERE MaThietBi = @MacAddress", connection);
    patientCommand.Parameters.Add("@MacAddress", SqlDbType.NVarChar, 100).Value = request.MacAddress.Trim();
    var patientValue = await patientCommand.ExecuteScalarAsync();
    if (patientValue is null) return Results.BadRequest(new { message = "Thiết bị chưa được liên kết với bệnh nhân." });
    var patientId = Convert.ToInt32(patientValue);
    var fileName = $"Record_{patientId}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.csv";
    var fullPath = Path.Combine(csvDirectory, fileName);
    await File.WriteAllTextAsync(fullPath, csv, Encoding.UTF8);
    try
    {
        const string insertSql = """
            INSERT INTO DuLieuECG (BenhNhanId, DuongDanCsv, DuLieuGocJson, NhipTim, Rmssd, ThoiGianDo, MucDoCanhBao)
            OUTPUT INSERTED.Id VALUES (@PatientId, @Path, @RawJson, @HeartRate, @Rmssd, GETDATE(), @AlertLevel);
            """;
        await using var insert = new SqlCommand(insertSql, connection);
        insert.Parameters.Add("@PatientId", SqlDbType.Int).Value = patientId;
        insert.Parameters.Add("@Path", SqlDbType.NVarChar, 500).Value = fullPath;
        insert.Parameters.Add("@RawJson", SqlDbType.NVarChar, -1).Value = request.FileJson;
        insert.Parameters.Add("@HeartRate", SqlDbType.Int).Value = request.NhipTim;
        insert.Parameters.Add("@Rmssd", SqlDbType.Float).Value = request.Rmssd;
        insert.Parameters.Add("@AlertLevel", SqlDbType.VarChar, 20).Value = AlertLevel(request.NhipTim);
        var recordId = Convert.ToInt32(await insert.ExecuteScalarAsync());
        await hub.Clients.All.SendAsync("NewRecordUploaded", recordId, patientId);
        return Results.Created($"/api/records/{recordId}/csv", new { recordId, patientId, fileName });
    }
    catch
    {
        File.Delete(fullPath);
        throw;
    }
});

// ===== Phân hệ quản trị viên: tài khoản, điều phối, thống kê, thiết bị =====
app.MapGet("/api/admin/dashboard", async () =>
{
    const string sql = """
        SELECT
          (SELECT COUNT(*) FROM BenhNhan) AS TotalPatients,
          (SELECT COUNT(*) FROM BacSi) AS TotalDoctors,
          (SELECT COUNT(*) FROM ThietBiEcg WHERE TrangThai = 'ASSIGNED') AS ActiveDevices,
          (SELECT COUNT(*) FROM CaTuVan WHERE TrangThai = 'PENDING') AS PendingConsultations,
          (SELECT COUNT(*) FROM CaTuVan WHERE TrangThai = 'RESPONDED') AS RespondedConsultations,
          (SELECT COUNT(*) FROM CaTuVan WHERE TrangThai = 'COMPLETED') AS CompletedConsultations;
        """;
    await using var connection = new SqlConnection(connectionString); await connection.OpenAsync();
    await using var command = new SqlCommand(sql, connection); await using var reader = await command.ExecuteReaderAsync();
    await reader.ReadAsync();
    return Results.Ok(new { totalPatients = reader.GetInt32(0), totalDoctors = reader.GetInt32(1), activeDevices = reader.GetInt32(2), pendingConsultations = reader.GetInt32(3), respondedConsultations = reader.GetInt32(4), completedConsultations = reader.GetInt32(5) });
});

app.MapGet("/api/admin/analytics/consultations", async (int days = 7) =>
{
    days = Math.Clamp(days, 1, 90);
    const string sql = """
        SELECT CAST(ThoiGianGui AS DATE) AS Ngay, COUNT(*) AS SoCaMoi
        FROM CaTuVan WHERE ThoiGianGui >= DATEADD(DAY, -@Days, CAST(GETDATE() AS DATE))
        GROUP BY CAST(ThoiGianGui AS DATE) ORDER BY Ngay;
        """;
    var points = new List<object>(); await using var connection = new SqlConnection(connectionString); await connection.OpenAsync(); await using var command = new SqlCommand(sql, connection); command.Parameters.Add("@Days", SqlDbType.Int).Value = days; await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync()) points.Add(new { date = reader.GetDateTime(0), count = reader.GetInt32(1) }); return Results.Ok(points);
});

app.MapGet("/api/admin/users", async (string? role, string? status) =>
{
    const string sql = """
        SELECT tk.Id, tk.TenDangNhap, q.TenQuyen, tk.TrangThai, tk.NgayTao,
               COALESCE(bs.HoTen, bn.HoTen, tk.TenDangNhap) AS HoTen
        FROM TaiKhoan tk JOIN Quyen q ON q.Id = tk.QuyenId
        LEFT JOIN BacSi bs ON bs.TaiKhoanId = tk.Id
        LEFT JOIN BenhNhan bn ON bn.TaiKhoanId = tk.Id
        WHERE (@Role IS NULL OR q.TenQuyen = @Role) AND (@Status IS NULL OR tk.TrangThai = @Status)
        ORDER BY tk.Id;
        """;
    var users = new List<object>();
    await using var connection = new SqlConnection(connectionString); await connection.OpenAsync();
    await using var command = new SqlCommand(sql, connection);
    command.Parameters.Add("@Role", SqlDbType.VarChar, 50).Value = string.IsNullOrWhiteSpace(role) ? DBNull.Value : role.ToUpperInvariant();
    command.Parameters.Add("@Status", SqlDbType.VarChar, 20).Value = string.IsNullOrWhiteSpace(status) ? DBNull.Value : status.ToUpperInvariant();
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync()) users.Add(new { id = reader.GetInt32(0), username = reader.GetString(1), role = reader.GetString(2), status = reader.GetString(3), createdAt = reader.GetDateTime(4), fullName = reader.GetString(5) });
    return Results.Ok(users);
});

app.MapPut("/api/admin/users/{accountId:int}/lock", async (int accountId) => await UpdateAccountStatus(accountId, "LOCKED", connectionString));
app.MapPut("/api/admin/users/{accountId:int}/unlock", async (int accountId) => await UpdateAccountStatus(accountId, "ACTIVE", connectionString));

app.MapDelete("/api/admin/users/{accountId:int}", async (int accountId) =>
{
    const string sql = """
        DECLARE @HasMedicalData BIT = CASE WHEN EXISTS (
            SELECT 1 FROM BenhNhan b WHERE b.TaiKhoanId = @AccountId AND
            (EXISTS (SELECT 1 FROM DuLieuECG e WHERE e.BenhNhanId = b.Id) OR EXISTS (SELECT 1 FROM CaTuVan c WHERE c.BenhNhanId = b.Id))
        ) OR EXISTS (
            SELECT 1 FROM BacSi bs WHERE bs.TaiKhoanId = @AccountId AND EXISTS (SELECT 1 FROM CaTuVan c WHERE c.BacSiId = bs.Id)
        ) THEN 1 ELSE 0 END;
        SELECT @HasMedicalData;
        """;
    await using var connection = new SqlConnection(connectionString); await connection.OpenAsync();
    await using var check = new SqlCommand(sql, connection); check.Parameters.Add("@AccountId", SqlDbType.Int).Value = accountId;
    if (Convert.ToBoolean(await check.ExecuteScalarAsync())) return Results.Conflict(new { message = "Tài khoản đã có hồ sơ y tế, chỉ có thể khóa tạm thời." });
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    try
    {
        await using var removePatient = new SqlCommand("DELETE FROM BenhNhan WHERE TaiKhoanId = @Id", connection, transaction); removePatient.Parameters.Add("@Id", SqlDbType.Int).Value = accountId; await removePatient.ExecuteNonQueryAsync();
        await using var removeDoctor = new SqlCommand("DELETE FROM BacSi WHERE TaiKhoanId = @Id", connection, transaction); removeDoctor.Parameters.Add("@Id", SqlDbType.Int).Value = accountId; await removeDoctor.ExecuteNonQueryAsync();
        await using var removeAccount = new SqlCommand("DELETE FROM TaiKhoan WHERE Id = @Id", connection, transaction); removeAccount.Parameters.Add("@Id", SqlDbType.Int).Value = accountId;
        if (await removeAccount.ExecuteNonQueryAsync() == 0) return Results.NotFound();
        await transaction.CommitAsync(); return Results.NoContent();
    }
    catch { await transaction.RollbackAsync(); throw; }
});

app.MapPost("/api/admin/doctors", async (CreateDoctorRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password)) return Results.BadRequest(new { message = "Họ tên, tên đăng nhập và mật khẩu là bắt buộc." });
    await using var connection = new SqlConnection(connectionString); await connection.OpenAsync(); await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    try
    {
        const string accountSql = "INSERT INTO TaiKhoan (TenDangNhap, MatKhau, QuyenId, TrangThai) OUTPUT INSERTED.Id SELECT @Username, @Password, Id, 'ACTIVE' FROM Quyen WHERE TenQuyen = 'BACSI';";
        await using var account = new SqlCommand(accountSql, connection, transaction); account.Parameters.Add("@Username", SqlDbType.VarChar, 50).Value = request.Username.Trim(); account.Parameters.Add("@Password", SqlDbType.VarChar, 255).Value = request.Password; var accountId = Convert.ToInt32(await account.ExecuteScalarAsync());
        const string doctorSql = "INSERT INTO BacSi (TaiKhoanId, HoTen, ChuyenKhoa, SoDienThoai, Email, SoNamKinhNghiem) OUTPUT INSERTED.Id VALUES (@AccountId, @FullName, @Specialty, @Phone, @Email, @Experience);";
        await using var doctor = new SqlCommand(doctorSql, connection, transaction); doctor.Parameters.Add("@AccountId", SqlDbType.Int).Value = accountId; doctor.Parameters.Add("@FullName", SqlDbType.NVarChar, 100).Value = request.FullName.Trim(); doctor.Parameters.Add("@Specialty", SqlDbType.NVarChar, 100).Value = DbValue(request.Specialty); doctor.Parameters.Add("@Phone", SqlDbType.VarChar, 15).Value = DbValue(request.PhoneNumber); doctor.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = DbValue(request.Email); doctor.Parameters.Add("@Experience", SqlDbType.Int).Value = request.YearsOfExperience;
        var doctorId = Convert.ToInt32(await doctor.ExecuteScalarAsync()); await transaction.CommitAsync(); return Results.Created($"/api/doctors/{doctorId}", new { doctorId, accountId });
    }
    catch (SqlException ex) when (ex.Number is 2601 or 2627) { await transaction.RollbackAsync(); return Results.Conflict(new { message = "Tên đăng nhập đã tồn tại." }); }
    catch { await transaction.RollbackAsync(); throw; }
});

app.MapGet("/api/admin/patients/unassigned", async () =>
{
    const string sql = "SELECT Id, HoTen, Tuoi, SoDienThoai FROM BenhNhan WHERE BacSiId IS NULL ORDER BY Id DESC";
    var patients = new List<object>(); await using var connection = new SqlConnection(connectionString); await connection.OpenAsync(); await using var command = new SqlCommand(sql, connection); await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync()) patients.Add(new { id = reader.GetInt32(0), fullName = reader.GetString(1), age = GetNullableInt(reader, 2), phoneNumber = GetString(reader, 3) }); return Results.Ok(patients);
});

app.MapPut("/api/admin/assignments", async (AssignmentRequest request, IHubContext<EcgHub> hub) =>
{
    const string sql = """
        UPDATE BenhNhan SET BacSiId = @DoctorId
        WHERE Id = @PatientId AND EXISTS (
            SELECT 1 FROM BacSi b JOIN TaiKhoan t ON t.Id = b.TaiKhoanId
            WHERE b.Id = @DoctorId AND t.TrangThai = 'ACTIVE');
        IF @@ROWCOUNT = 0 SELECT CAST(NULL AS NVARCHAR(100));
        ELSE SELECT HoTen FROM BenhNhan WHERE Id = @PatientId;
        """;
    await using var connection = new SqlConnection(connectionString); await connection.OpenAsync(); await using var command = new SqlCommand(sql, connection); command.Parameters.Add("@PatientId", SqlDbType.Int).Value = request.PatientId; command.Parameters.Add("@DoctorId", SqlDbType.Int).Value = request.DoctorId;
    var patientName = await command.ExecuteScalarAsync(); if (patientName is null) return Results.BadRequest(new { message = "Không thể phân công. Bác sĩ không tồn tại, bị khóa hoặc bệnh nhân không tồn tại." });
    await hub.Clients.All.SendAsync("AdminAssignedPatient", new { doctorId = request.DoctorId, patientId = request.PatientId, patientName = patientName.ToString() }); return Results.NoContent();
});

app.MapGet("/api/admin/devices", async () =>
{
    const string sql = "SELECT d.Id, d.MaThietBi, d.TrangThai, d.NgayKhaiBao, b.Id, b.HoTen FROM ThietBiEcg d LEFT JOIN ThietBiBenhNhan l ON l.MaThietBi = d.MaThietBi LEFT JOIN BenhNhan b ON b.Id = l.BenhNhanId ORDER BY d.NgayKhaiBao DESC";
    var devices = new List<object>(); await using var connection = new SqlConnection(connectionString); await connection.OpenAsync(); await using var command = new SqlCommand(sql, connection); await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync()) devices.Add(new { id = reader.GetInt32(0), macAddress = reader.GetString(1), status = reader.GetString(2), registeredAt = reader.GetDateTime(3), patientId = GetNullableInt(reader, 4), patientName = GetString(reader, 5) }); return Results.Ok(devices);
});

app.MapPost("/api/admin/devices", async (DeviceRegistrationRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.MacAddress)) return Results.BadRequest(new { message = "Địa chỉ MAC là bắt buộc." }); await using var connection = new SqlConnection(connectionString); await connection.OpenAsync(); await using var command = new SqlCommand("INSERT INTO ThietBiEcg (MaThietBi, GhiChu) OUTPUT INSERTED.Id VALUES (@MacAddress, @Note)", connection); command.Parameters.Add("@MacAddress", SqlDbType.NVarChar, 100).Value = request.MacAddress.Trim(); command.Parameters.Add("@Note", SqlDbType.NVarChar, 255).Value = DbValue(request.Note); try { return Results.Created("/api/admin/devices", new { deviceId = Convert.ToInt32(await command.ExecuteScalarAsync()) }); } catch (SqlException ex) when (ex.Number is 2601 or 2627) { return Results.Conflict(new { message = "Thiết bị có địa chỉ MAC này đã tồn tại." }); }
});

app.MapDelete("/api/admin/devices/{macAddress}", async (string macAddress) =>
{
    await using var connection = new SqlConnection(connectionString); await connection.OpenAsync(); await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    try { await using var unlink = new SqlCommand("DELETE FROM ThietBiBenhNhan WHERE MaThietBi = @MacAddress", connection, transaction); unlink.Parameters.Add("@MacAddress", SqlDbType.NVarChar, 100).Value = macAddress; await unlink.ExecuteNonQueryAsync(); await using var available = new SqlCommand("UPDATE ThietBiEcg SET TrangThai = 'AVAILABLE' WHERE MaThietBi = @MacAddress", connection, transaction); available.Parameters.Add("@MacAddress", SqlDbType.NVarChar, 100).Value = macAddress; if (await available.ExecuteNonQueryAsync() == 0) return Results.NotFound(); await transaction.CommitAsync(); return Results.NoContent(); } catch { await transaction.RollbackAsync(); throw; }
});

// Development/demo utility: it clears consultations only, never patient or ECG measurements.
app.MapPost("/api/reset-database", async () =>
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new SqlCommand("DELETE FROM CaTuVan; DBCC CHECKIDENT ('CaTuVan', RESEED, 0);", connection);
    await command.ExecuteNonQueryAsync();
    return Results.NoContent();
});

app.MapDelete("/api/patients/{patientId:int}", async (int patientId) =>
{
    await using (var guardConnection = new SqlConnection(connectionString))
    {
        await guardConnection.OpenAsync();
        await using var guard = new SqlCommand("SELECT CASE WHEN EXISTS (SELECT 1 FROM DuLieuECG WHERE BenhNhanId = @Id) OR EXISTS (SELECT 1 FROM CaTuVan WHERE BenhNhanId = @Id) THEN 1 ELSE 0 END", guardConnection);
        guard.Parameters.Add("@Id", SqlDbType.Int).Value = patientId;
        if (Convert.ToInt32(await guard.ExecuteScalarAsync()) == 1) return Results.Conflict(new { message = "Tài khoản đã có hồ sơ y tế, chỉ có thể khóa tạm thời." });
    }
    const string sql = """
        SET XACT_ABORT ON; BEGIN TRANSACTION;
        DECLARE @AccountId INT = (SELECT TaiKhoanId FROM BenhNhan WHERE Id = @PatientId);
        DELETE c FROM CaTuVan c INNER JOIN DuLieuECG e ON e.Id = c.DuLieuECGId WHERE e.BenhNhanId = @PatientId;
        DELETE FROM CaTuVan WHERE BenhNhanId = @PatientId;
        DELETE FROM ThietBiBenhNhan WHERE BenhNhanId = @PatientId;
        DELETE FROM DuLieuECG WHERE BenhNhanId = @PatientId;
        DELETE FROM BenhNhan WHERE Id = @PatientId;
        IF @AccountId IS NOT NULL DELETE FROM TaiKhoan WHERE Id = @AccountId;
        COMMIT TRANSACTION;
        """;
    await using var connection = new SqlConnection(connectionString); await connection.OpenAsync();
    await using var command = new SqlCommand(sql, connection); command.Parameters.Add("@PatientId", SqlDbType.Int).Value = patientId;
    await command.ExecuteNonQueryAsync(); return Results.NoContent();
});

app.MapDelete("/api/doctors/{doctorId:int}", async (int doctorId) =>
{
    await using (var guardConnection = new SqlConnection(connectionString))
    {
        await guardConnection.OpenAsync();
        await using var guard = new SqlCommand("SELECT CASE WHEN EXISTS (SELECT 1 FROM CaTuVan WHERE BacSiId = @Id) THEN 1 ELSE 0 END", guardConnection);
        guard.Parameters.Add("@Id", SqlDbType.Int).Value = doctorId;
        if (Convert.ToInt32(await guard.ExecuteScalarAsync()) == 1) return Results.Conflict(new { message = "Tài khoản đã có hồ sơ y tế, chỉ có thể khóa tạm thời." });
    }
    const string sql = """
        SET XACT_ABORT ON; BEGIN TRANSACTION;
        DECLARE @AccountId INT = (SELECT TaiKhoanId FROM BacSi WHERE Id = @DoctorId);
        UPDATE BenhNhan SET BacSiId = NULL WHERE BacSiId = @DoctorId;
        UPDATE CaTuVan SET BacSiId = NULL WHERE BacSiId = @DoctorId;
        DELETE FROM BacSi WHERE Id = @DoctorId;
        IF @AccountId IS NOT NULL DELETE FROM TaiKhoan WHERE Id = @AccountId;
        COMMIT TRANSACTION;
        """;
    await using var connection = new SqlConnection(connectionString); await connection.OpenAsync();
    await using var command = new SqlCommand(sql, connection); command.Parameters.Add("@DoctorId", SqlDbType.Int).Value = doctorId;
    await command.ExecuteNonQueryAsync(); return Results.NoContent();
});

app.Run();

static object DbValue(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
static async Task<IResult> UpdateAccountStatus(int accountId, string status, string connectionString)
{
    await using var connection = new SqlConnection(connectionString); await connection.OpenAsync();
    await using var command = new SqlCommand("UPDATE TaiKhoan SET TrangThai = @Status WHERE Id = @Id", connection);
    command.Parameters.Add("@Status", SqlDbType.VarChar, 20).Value = status;
    command.Parameters.Add("@Id", SqlDbType.Int).Value = accountId;
    return await command.ExecuteNonQueryAsync() == 0 ? Results.NotFound() : Results.NoContent();
}
static string GetString(SqlDataReader reader, int ordinal, string fallback = "") => reader.IsDBNull(ordinal) ? fallback : reader.GetString(ordinal);
static int? GetNullableInt(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
static double? GetNullableDouble(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDouble(ordinal);
static string AlertLevel(int heartRate) => heartRate is < 50 or > 120 ? "DANGEROUS" : heartRate is < 60 or > 100 ? "WARNING" : "NORMAL";
static bool TryBuildCsv(string rawJson, out string csv, out string error)
{
    csv = ""; error = "";
    try
    {
        using var document = JsonDocument.Parse(rawJson);
        if (!document.RootElement.TryGetProperty("signal", out var signal) || signal.ValueKind != JsonValueKind.Array || signal.GetArrayLength() == 0) { error = "FileJson must contain a non-empty signal array."; return false; }
        if (signal.GetArrayLength() > 100_000) { error = "Signal is too large."; return false; }
        var builder = new StringBuilder("xi,oi,qi,envelope,pred_peak_mask\n");
        foreach (var value in signal.EnumerateArray()) builder.Append(',').Append((value.GetDouble() / 1000d).ToString(CultureInfo.InvariantCulture)).Append(",,,\n");
        csv = builder.ToString(); return true;
    }
    catch (Exception) { error = "FileJson is not valid ECG JSON."; return false; }
}

static async Task<IResult> SetDoctor(int patientId, AssignDoctorRequest request, string connectionString)
{
    await using var connection = new SqlConnection(connectionString); await connection.OpenAsync();
    if (request.DoctorId is not null) { await using var check = new SqlCommand("SELECT COUNT(1) FROM BacSi WHERE Id = @Id", connection); check.Parameters.Add("@Id", SqlDbType.Int).Value = request.DoctorId; if (Convert.ToInt32(await check.ExecuteScalarAsync()) == 0) return Results.BadRequest(new { message = "Không tìm thấy bác sĩ." }); }
    await using var command = new SqlCommand("UPDATE BenhNhan SET BacSiId = @DoctorId WHERE Id = @PatientId", connection); command.Parameters.Add("@DoctorId", SqlDbType.Int).Value = request.DoctorId is null ? DBNull.Value : request.DoctorId; command.Parameters.Add("@PatientId", SqlDbType.Int).Value = patientId;
    return await command.ExecuteNonQueryAsync() == 0 ? Results.NotFound() : Results.NoContent();
}
static async Task<IResult> GetCsv(int recordId, string connectionString)
{
    await using var connection = new SqlConnection(connectionString); await connection.OpenAsync(); await using var command = new SqlCommand("SELECT DuongDanCsv FROM DuLieuECG WHERE Id = @Id", connection); command.Parameters.Add("@Id", SqlDbType.Int).Value = recordId;
    var path = (string?)await command.ExecuteScalarAsync(); return string.IsNullOrWhiteSpace(path) || !File.Exists(path) ? Results.NotFound(new { message = "Không tìm thấy tệp CSV." }) : Results.File(path, "text/csv", enableRangeProcessing: true);
}

public sealed class EcgHub : Hub { }
public sealed class ComplaintRequest { public int PatientId { get; set; } public int EcgRecordId { get; set; } public string Complaint { get; set; } = ""; }
public sealed class FeedbackRequest { public int ConsultationId { get; set; } public int DoctorId { get; set; } public string Findings { get; set; } = ""; public string Treatment { get; set; } = ""; }
public sealed class PatientLoginRequest { public string FullName { get; set; } = ""; public string Password { get; set; } = ""; }
public sealed class DoctorLoginRequest { public string Username { get; set; } = ""; public string Password { get; set; } = ""; }
public sealed class AssignDoctorRequest { public int? DoctorId { get; set; } }
public sealed class RegisterRequest { public string FullName { get; set; } = ""; public int Age { get; set; } public string Gender { get; set; } = ""; public string PhoneNumber { get; set; } = ""; public string Email { get; set; } = ""; public string Address { get; set; } = ""; public string Password { get; set; } = ""; }
public sealed class DeviceLinkRequest { public string MacAddress { get; set; } = ""; public int PatientId { get; set; } }
public sealed class EcgUploadRequest { public string MacAddress { get; set; } = ""; public string FileJson { get; set; } = ""; public int NhipTim { get; set; } public double Rmssd { get; set; } }
public sealed class CreateDoctorRequest { public string FullName { get; set; } = ""; public string Username { get; set; } = ""; public string Password { get; set; } = ""; public string Specialty { get; set; } = ""; public string PhoneNumber { get; set; } = ""; public string Email { get; set; } = ""; public int YearsOfExperience { get; set; } }
public sealed class AssignmentRequest { public int PatientId { get; set; } public int DoctorId { get; set; } }
public sealed class DeviceRegistrationRequest { public string MacAddress { get; set; } = ""; public string Note { get; set; } = ""; }
