using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using ScottPlot;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             


namespace HRMonitor
{
    public partial class MainWindow : Window

    {
        public ObservableCollection<Patient> Patients { get; set; }
        public ObservableCollection<Doctor> Doctors { get; set; }
        public ObservableCollection<EcgRecord> Records { get; set; }
        private bool isAdminLogin = false;
        private Patient _selectedPatient;
        private Doctor _selectedDoctor;

        //  Khởi tạo HttpClient để gọi C# Web API cổng 5000
        private static readonly HttpClient _httpClient = new HttpClient();

        //nhớ xem Bác sĩ đang xem ca khám nào
        private int _currentConsultationId = 0;
        private bool _isDemoRecord;

        private HubConnection _hubConnection;
        public MainWindow()
        {
            InitializeComponent();
            Patients = new ObservableCollection<Patient>();
            Doctors = new ObservableCollection<Doctor>();
            Records = new ObservableCollection<EcgRecord>();
            
            PatientListView.ItemsSource = Patients;
            DoctorListView.ItemsSource = Doctors;
            RecordListView.ItemsSource = Records;
            AssignDoctorComboBox.ItemsSource = Doctors;

            _ = LoadPatientsFromApiAsync();

            // [BACKEND] Gọi API ngay khi mở App để lấy data thật
            // Load data khởi tạo
            _ = LoadDataFromApiAsync(1);

            // Khởi tạo kết nối SignalR
            InitializeSignalR();
        }
        private async void InitializeSignalR()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/ecghub")
                .WithAutomaticReconnect()
                .Build();

            // Lắng nghe sự kiện "PatientSentComplaint" từ Backend
            _hubConnection.On<int>("PatientSentComplaint", async (ecgRecordId) =>
            {
                // Khi nhận được thông báo, tự động tải lại danh sách
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    // Tải lại API
                    await LoadDataFromApiAsync(1);

                    MessageBox.Show($"Bệnh nhân vừa gửi yêu cầu cho Record ID: {ecgRecordId}");
                });
            });

            try
            {
                await _hubConnection.StartAsync();
            }
            catch (Exception ex)
            {
            }
        }

        // Hàm này gọi API lấy 15 file của bệnh nhân
        private async Task LoadDataFromApiAsync(int patientId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"http://localhost:5000/api/records/{patientId}");
                if (response.IsSuccessStatusCode)
                {
                    var recordsApi = await response.Content.ReadFromJsonAsync<List<ConsultationDto>>();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Records.Clear();
                        foreach (var r in recordsApi ?? [])
                        {
                            Records.Add(new EcgRecord
                            {
                                Id = r.EcgId,
                                Name = $"Bản ghi ECG #{r.EcgId}",
                                FilePath = r.RecordName,
                                Date = $"Trạng thái: {FormatConsultationStatus(r.Status)}",
                                PatientComplaint = r.Complaint,
                                ConsultationId = r.ConsultationId,
                                Findings = r.Findings,
                                Treatment = r.Treatment,
                                HeartRate = r.NhipTim,
                                Rmssd = r.Rmssd
                            });
                        }
                        if (Records.Count == 0) LoadDemoRecords();
                    });
                }
            }
            catch (Exception ex)
            {
                LoadDemoRecords();
            }
        }

        private static string FormatConsultationStatus(string? status) => status switch
        {
            "PENDING" or "Pending" => "⏳ Đang chờ bác sĩ tư vấn",
            "RESPONDED" or "Responded" => "✅ Bác sĩ đã phản hồi",
            _ => "📝 Chưa gửi yêu cầu tư vấn"
        };

        private void LoadDemoRecords()
        {
            Records.Clear();
            Records.Add(new EcgRecord { Id = 901, Name = "Mẫu ECG bình thường", Date = "Trạng thái: ✅ Bác sĩ đã phản hồi", PatientComplaint = "Khám sức khỏe định kỳ, không có triệu chứng bất thường.", Findings = "Nhịp xoang đều, sóng ECG trong giới hạn bình thường.", Treatment = "Duy trì vận động nhẹ và tái khám định kỳ.", ConsultationId = 901, HeartRate = 72, Rmssd = 38.6, IsDemo = true, WaveVariant = 0 });
            Records.Add(new EcgRecord { Id = 902, Name = "Mẫu ECG cần theo dõi", Date = "Trạng thái: ⏳ Đang chờ bác sĩ tư vấn", PatientComplaint = "Cảm giác hồi hộp sau khi vận động mạnh.", ConsultationId = 902, HeartRate = 96, Rmssd = 29.4, IsDemo = true, WaveVariant = 1 });
            Records.Add(new EcgRecord { Id = 903, Name = "Mẫu ECG nhịp nhanh", Date = "Trạng thái: 📝 Chưa gửi yêu cầu tư vấn", ConsultationId = 0, HeartRate = 108, Rmssd = 24.8, IsDemo = true, WaveVariant = 2 });
        }

        // Hàm này gửi lời khuyên của Bác sĩ lên C# API
        public async Task SubmitFeedbackToApi(int consultationId, string findings, string treatment)
        {
            var payload = new
            {
                ConsultationId = consultationId,
                DoctorId = App.LoggedInDoctorId > 0 ? App.LoggedInDoctorId : 1, 
                Findings = findings,
                Treatment = treatment
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("http://localhost:5000/api/doctor/feedback", payload);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Đã lưu lời khuyên xuống Database thành công!", "Thông báo");
                    await LoadDataFromApiAsync(1); // Load lại danh sách sau khi lưu
                }
                else
                {
                    MessageBox.Show("Lỗi khi lưu Database!", "Lỗi Server");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Mất kết nối tới API.\n" + ex.Message, "Lỗi Server");
            }
        }



        // Xoá LoadDummyRecords()

        private async Task LoadPatientsFromApiAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:5000/api/patients");
                if (response.IsSuccessStatusCode)
                {
                    var patientsList = await response.Content.ReadFromJsonAsync<List<Patient>>();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Patients.Clear();
                        if (patientsList != null)
                        {
                            foreach (var p in patientsList)
                            {
                                if (!isAdminLogin && App.LoggedInDoctorId > 0 && p.DoctorId != App.LoggedInDoctorId)
                                {
                                    continue;
                                }
                                Patients.Add(p);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải danh sách bệnh nhân từ Server.\n" + ex.Message);
            }
        }

        private async Task LoadDoctorsFromApiAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:5000/api/doctors");
                if (response.IsSuccessStatusCode)
                {
                    var doctorsList = await response.Content.ReadFromJsonAsync<List<Doctor>>();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Doctors.Clear();
                        if (doctorsList != null)
                        {
                            foreach (var d in doctorsList)
                                Doctors.Add(d);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải danh sách bác sĩ từ Server.\n" + ex.Message);
            }
        }

        private void RoleToggle_Click(object sender, RoutedEventArgs e)
        {
            isAdminLogin = !isAdminLogin;
            if (isAdminLogin)
            {
                LoginTitleText.Text = "Đăng nhập Quản trị viên";
                RoleToggleBtn.Content = "Đăng nhập Bác sĩ";
            }
            else
            {
                LoginTitleText.Text = "Đăng nhập Bác sĩ";
                RoleToggleBtn.Content = "Đăng nhập Quản trị viên";
            }
        }

        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (isAdminLogin)
            {
                if (IdTextBox.Text == "admin" && PasswordBox.Password == "admin")
                {
                    LoginGrid.Visibility = Visibility.Collapsed;
                    DashboardGrid.Visibility = Visibility.Visible;
                    LoginErrorText.Visibility = Visibility.Collapsed;
                    DoctorListGrid.Visibility = Visibility.Visible;
                    DoctorListRow.Height = new GridLength(1, GridUnitType.Star);
                    AdminDashboardPanel.Visibility = Visibility.Visible;
                    _ = LoadDoctorsFromApiAsync();
                    _ = LoadPatientsFromApiAsync();
                    _ = LoadAdminDashboardAsync();
                }
                else
                {
                    LoginErrorText.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (int.TryParse(IdTextBox.Text, out int docId))
                {
                    try
                    {
                        var response = await _httpClient.PostAsJsonAsync("http://localhost:5000/api/doctors/login", new
                        {
                            Username = IdTextBox.Text.Trim(),
                            Password = PasswordBox.Password
                        });
                        if (response.IsSuccessStatusCode)
                        {
                            var login = await response.Content.ReadFromJsonAsync<DoctorLoginResponse>();
                            if (login != null && login.DoctorId == docId)
                            {
                                App.LoggedInDoctorId = docId;
                                LoginGrid.Visibility = Visibility.Collapsed;
                                DashboardGrid.Visibility = Visibility.Visible;
                                LoginErrorText.Visibility = Visibility.Collapsed;
                                DoctorListGrid.Visibility = Visibility.Collapsed;
                                DoctorListRow.Height = new GridLength(0);
                                AdminDashboardPanel.Visibility = Visibility.Collapsed;
                                _ = LoadPatientsFromApiAsync();
                                return;
                            }
                        }
                    }
                    catch { }
                }
                LoginErrorText.Visibility = Visibility.Visible;
            }
        }

        private async Task LoadAdminDashboardAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:5000/api/admin/dashboard");
                var dashboard = response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<AdminDashboard>() : null;
                if (dashboard is null) return;
                TotalPatientsText.Text = dashboard.TotalPatients.ToString();
                TotalDoctorsText.Text = dashboard.TotalDoctors.ToString();
                ActiveDevicesText.Text = dashboard.ActiveDevices.ToString();
                PendingConsultationsText.Text = dashboard.PendingConsultations.ToString();
            }
            catch { }
        }

        private void PatientListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PatientListView.SelectedItem is Patient patient)
            {
                _selectedPatient = patient;
                _selectedDoctor = null;
                // Update profile details
                ProfileName.Text = patient.Name;
                ProfileAge.Text = patient.Age.ToString();
                ProfileGender.Text = patient.Gender;
                ProfilePhone.Text = patient.PhoneNumber;
                ProfileEmail.Text = patient.Email;
                ProfileAddress.Text = patient.Address;

                // Toggle visibility
                EmptyProfileText.Visibility = Visibility.Collapsed;
                ProfileDetailsPanel.Visibility = Visibility.Visible;
                
                if (isAdminLogin)
                {
                    DoctorAssignmentGrid.Visibility = Visibility.Visible;
                    ViewRecordsFromProfileBtn.Visibility = Visibility.Collapsed;
                    DeleteAccountBtn.Visibility = Visibility.Visible;
                    AssignDoctorComboBox.SelectionChanged -= AssignDoctorComboBox_SelectionChanged;
                    AssignDoctorComboBox.SelectedValue = patient.DoctorId;
                    AssignDoctorComboBox.SelectionChanged += AssignDoctorComboBox_SelectionChanged;
                }
                else
                {
                    DoctorAssignmentGrid.Visibility = Visibility.Collapsed;
                    ViewRecordsFromProfileBtn.Visibility = Visibility.Visible;
                    DeleteAccountBtn.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async void AssignDoctorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectedPatient != null && AssignDoctorComboBox.SelectedValue is int doctorId)
            {
                _selectedPatient.DoctorId = doctorId;
                try
                {
                    var payload = new { DoctorId = doctorId };
                    var response = await _httpClient.PostAsJsonAsync($"http://localhost:5000/api/patients/{_selectedPatient.Id}/assign-doctor", payload);
                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Gán bác sĩ thất bại!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi kết nối API: " + ex.Message);
                }
            }
        }

        private void DoctorListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DoctorListView.SelectedItem is Doctor doctor)
            {
                _selectedDoctor = doctor;
                _selectedPatient = null;
                // Update profile details for Doctor
                ProfileName.Text = doctor.FullName;
                ProfileAge.Text = doctor.Age.ToString();
                ProfileGender.Text = doctor.Gender;
                ProfilePhone.Text = doctor.PhoneNumber;
                ProfileEmail.Text = doctor.Email;
                ProfileAddress.Text = doctor.Address;

                // Toggle visibility
                EmptyProfileText.Visibility = Visibility.Collapsed;
                ProfileDetailsPanel.Visibility = Visibility.Visible;
                ViewRecordsFromProfileBtn.Visibility = Visibility.Collapsed;
                DoctorAssignmentGrid.Visibility = Visibility.Collapsed;
                DeleteAccountBtn.Visibility = Visibility.Visible;
            }
        }

        private void ViewRecordFromProfile_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPatient != null)
            {
                PatientListGrid.Visibility = Visibility.Collapsed;
                DoctorListGrid.Visibility = Visibility.Collapsed;
                RightProfilePanel.Visibility = Visibility.Collapsed;
                Grid.SetColumnSpan(LeftAreaGrid, 2);
                RecordListGrid.Visibility = Visibility.Visible;

                // Cập nhật lại data từ DB khi ấn View Bệnh Nhân
                _ = LoadDataFromApiAsync(_selectedPatient.Id); // Wait, Patient doesn't have Id in dummy model yet? Let's check.
            }
        }
        private void BackToPatients_Click(object sender, RoutedEventArgs e)
        {
            RecordListGrid.Visibility = Visibility.Collapsed;
            Grid.SetColumnSpan(LeftAreaGrid, 1);
            RightProfilePanel.Visibility = Visibility.Visible;
            PatientListGrid.Visibility = Visibility.Visible;
            if (isAdminLogin)
            {
                DoctorListGrid.Visibility = Visibility.Visible;
            }
        }

        private async void DeleteAccountBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPatient != null)
            {
                if (MessageBox.Show($"Bạn có chắc chắn muốn xóa bệnh nhân '{_selectedPatient.Name}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await _httpClient.DeleteAsync($"http://localhost:5000/api/patients/{_selectedPatient.Id}");
                        if (response.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Đã xóa bệnh nhân thành công.");
                            ProfileDetailsPanel.Visibility = Visibility.Collapsed;
                            EmptyProfileText.Visibility = Visibility.Visible;
                            _selectedPatient = null;
                            _ = LoadPatientsFromApiAsync();
                        }
                        else
                        {
                            MessageBox.Show("Xóa thất bại!");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi: " + ex.Message);
                    }
                }
            }
            else if (_selectedDoctor != null)
            {
                if (MessageBox.Show($"Bạn có chắc chắn muốn xóa bác sĩ '{_selectedDoctor.FullName}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await _httpClient.DeleteAsync($"http://localhost:5000/api/doctors/{_selectedDoctor.Id}");
                        if (response.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Đã xóa bác sĩ thành công.");
                            ProfileDetailsPanel.Visibility = Visibility.Collapsed;
                            EmptyProfileText.Visibility = Visibility.Visible;
                            _selectedDoctor = null;
                            _ = LoadDoctorsFromApiAsync();
                            _ = LoadPatientsFromApiAsync();
                        }
                        else
                        {
                            MessageBox.Show("Xóa thất bại!");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi: " + ex.Message);
                    }
                }
            }
        }

        private void ViewRecordDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is EcgRecord record)
            {
                // 1. Lưu ConsultationId
                _currentConsultationId = record.ConsultationId;
                _isDemoRecord = record.IsDemo;

                RecordDetailTitle.Text = record.Name;
                HrTextBox.Text = record.HeartRate?.ToString() ?? "--";
                RmssdTextBox.Text = record.Rmssd?.ToString("F1") ?? "--";

                // Hiển thị biểu đồ cho cả bản ghi chưa có ca tư vấn để thuận tiện xem minh họa.
                ComplaintTextBox.Text = string.IsNullOrEmpty(record.PatientComplaint)
                    ? "Chưa có triệu chứng được gửi."
                    : record.PatientComplaint;

                FindingsTextBox.Text = record.Findings ?? "";
                TreatmentTextBox.Text = record.Treatment ?? "";
                FindingsTextBox.IsReadOnly = !string.IsNullOrEmpty(record.Findings);
                TreatmentTextBox.IsReadOnly = !string.IsNullOrEmpty(record.Treatment);

                RecordDetailModal.Visibility = Visibility.Visible;
                PlotEcgData(record.FilePath, record.WaveVariant);
            }
        }

        private async void SubmitFeedback_Click(object sender, RoutedEventArgs e)
        {
            // Nếu chưa có ID ca khám thì không làm gì cả
            if (_currentConsultationId == 0) return;
            if (_isDemoRecord)
            {
                MessageBox.Show("Đây là dữ liệu minh họa, không ghi thay đổi vào cơ sở dữ liệu.", "Chế độ minh họa");
                return;
            }

            // Lấy chữ bác sĩ vừa gõ trên màn hình
            string findings = FindingsTextBox.Text;
            string treatment = TreatmentTextBox.Text;

            if (string.IsNullOrWhiteSpace(findings) || string.IsNullOrWhiteSpace(treatment))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ nhận xét và phác đồ điều trị!", "Cảnh báo");
                return;
            }

            // Gọi hàm Backend bạn đã viết sẵn để bắn lên API
            await SubmitFeedbackToApi(_currentConsultationId, findings, treatment);

            // Gửi xong thì tự động đóng cái Popup lại
            RecordDetailModal.Visibility = Visibility.Collapsed;
        }

        private void CloseModal_Click(object sender, RoutedEventArgs e)
        {
            RecordDetailModal.Visibility = Visibility.Collapsed;
        }

        private void PlotEcgData(string csvPath, int waveVariant = 0)
        {
            try
            {
                double[] ys;
                if (File.Exists(csvPath))
                {
                    using (var reader = new StreamReader(csvPath))
                    using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                    {
                        var records = csv.GetRecords<CsvDataRow>().ToList();
                        int dataCount = Math.Min(1000, records.Count);
                        ys = new double[dataCount];

                        for (int i = 0; i < dataCount; i++)
                        {
                            ys[i] = records[i].oi;
                            // 250Hz sample rate -> 4 seconds for 1000 points
                        }
                    }
                }
                else
                {
                    ys = CreateDemoEcgWave(waveVariant);
                }

                double[] xs = Enumerable.Range(0, ys.Length).Select(index => index / 250.0).ToArray();
                EcgPlot.Plot.Clear();
                var signal = EcgPlot.Plot.Add.Scatter(xs, ys);
                signal.LineWidth = 2;
                signal.MarkerSize = 0;
                EcgPlot.Plot.XLabel("Thời gian (giây)");
                EcgPlot.Plot.YLabel("Điện áp (mV)");
                EcgPlot.Plot.Axes.SetLimits(0, 4, -0.5, 1.1);
                EcgPlot.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải biểu đồ ECG: " + ex.Message);
            }
        }

        private static double[] CreateDemoEcgWave(int variant)
        {
            var data = new double[1000];
            double heartRate = 72 + variant * 8;
            for (int index = 0; index < data.Length; index++)
            {
                double phase = ((index / 250.0) * heartRate / 60.0) % 1.0;
                double pulse(double center, double width, double amplitude) => amplitude * Math.Exp(-Math.Pow((phase - center) / width, 2));
                data[index] = pulse(.18, .035, .10) + pulse(.39, .012, -.13) + pulse(.42, .016, .88) + pulse(.46, .02, -.25) + pulse(.68, .065, .25) + .015 * Math.Sin(index * .09);
            }
            return data;
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            App.LoggedInDoctorId = 0;
            isAdminLogin = false;
            _selectedPatient = null;
            _selectedDoctor = null;
            _currentConsultationId = 0;
            _isDemoRecord = false;
            Patients.Clear();
            Doctors.Clear();
            Records.Clear();
            RecordDetailModal.Visibility = Visibility.Collapsed;
            RecordListGrid.Visibility = Visibility.Collapsed;
            PatientListGrid.Visibility = Visibility.Visible;
            RightProfilePanel.Visibility = Visibility.Visible;
            DoctorListGrid.Visibility = Visibility.Collapsed;
            DoctorListRow.Height = new GridLength(0);
            AdminDashboardPanel.Visibility = Visibility.Collapsed;
            ProfileDetailsPanel.Visibility = Visibility.Collapsed;
            EmptyProfileText.Visibility = Visibility.Visible;
            IdTextBox.Clear();
            PasswordBox.Clear();
            LoginGrid.Visibility = Visibility.Visible;
            DashboardGrid.Visibility = Visibility.Collapsed;
            LoginTitleText.Text = "Đăng nhập bác sĩ";
            RoleToggleBtn.Content = "Đăng nhập quản trị viên";
        }
    }

    #region MODELS 
    public class Patient
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public int? DoctorId { get; set; }
        public string GenderAgeText => $"{Gender}, {Age} tuổi";
    }

    public class EcgRecord
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; }
        public string Date { get; set; }
        public int? HeartRate { get; set; }
        public double? Rmssd { get; set; }
        public int WaveVariant { get; set; }
        public bool IsDemo { get; set; }

        // Thuộc tính để hứng dữ liệu API
        public int ConsultationId { get; set; }
        public string PatientComplaint { get; set; }
        public string Findings { get; set; }
        public string Treatment { get; set; }
    }

    public class CsvDataRow
    {
        public double xi { get; set; }
        public double oi { get; set; }
        public double qi { get; set; }
        public double envelope { get; set; }
        public int pred_peak_mask { get; set; }
    }

    public class Doctor
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Specialty { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string PhoneNumber { get; set; }
        public string Hospital { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
    }

    public class DoctorLoginResponse
    {
        public int DoctorId { get; set; }
        public string FullName { get; set; }
    }

    public class AdminDashboard
    {
        public int TotalPatients { get; set; }
        public int TotalDoctors { get; set; }
        public int ActiveDevices { get; set; }
        public int PendingConsultations { get; set; }
        public int RespondedConsultations { get; set; }
    }
    
    // Class nhận JSON từ API trả về 
    public class ConsultationDto
    {
        public int EcgId { get; set; }
        public string RecordName { get; set; }
        public int? NhipTim { get; set; }
        public double? Rmssd { get; set; }
        public int ConsultationId { get; set; }
        public string Complaint { get; set; }
        public string Findings { get; set; }
        public string Treatment { get; set; }
        public string Status { get; set; }
    }
    #endregion
}
