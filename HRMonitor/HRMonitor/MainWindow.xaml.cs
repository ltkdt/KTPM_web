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

        private HubConnection _hubConnection;
        public MainWindow()
        {
            InitializeComponent();
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
                        foreach (var r in recordsApi)
                        {
                            Records.Add(new EcgRecord
                            {
                                Id = r.EcgId,
                                Name = $"Bản ghi ECG #{r.EcgId}",
                                FilePath = r.RecordName,
                                Date = $"Trạng thái: {r.Status}",
                                PatientComplaint = r.Complaint,
                                ConsultationId = r.ConsultationId
                            });
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Backend Error: Không thể kết nối tới Web API.\n" + ex.Message, "Lỗi Server");
            }
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
                LoginTitleText.Text = "Admin Login";
                RoleToggleBtn.Content = "Doctor login";
            }
            else
            {
                LoginTitleText.Text = "Doctor Login";
                RoleToggleBtn.Content = "Admin login";
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
                    _ = LoadDoctorsFromApiAsync();
                    _ = LoadPatientsFromApiAsync();
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
                        var response = await _httpClient.GetAsync("http://localhost:5000/api/doctors");
                        if (response.IsSuccessStatusCode)
                        {
                            var doctorsList = await response.Content.ReadFromJsonAsync<List<Doctor>>();
                            var doc = doctorsList?.FirstOrDefault(d => d.Id == docId && d.Password == PasswordBox.Password);
                            
                            if (doc != null)
                            {
                                App.LoggedInDoctorId = docId;
                                LoginGrid.Visibility = Visibility.Collapsed;
                                DashboardGrid.Visibility = Visibility.Visible;
                                LoginErrorText.Visibility = Visibility.Collapsed;
                                DoctorListGrid.Visibility = Visibility.Collapsed;
                                DoctorListRow.Height = new GridLength(0);
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

                RecordDetailTitle.Text = record.Name;

                // Nếu ConsultationId == 0 (Chưa có bệnh nhân nào gửi complaint)
                if (_currentConsultationId == 0)
                {
                    MessageBox.Show("Bệnh nhân chưa gửi yêu cầu khám cho file này!", "Thông báo");
                    return; // Chặn không cho bác sĩ gửi khuyên
                }

                // 2. Đổ nội dung
                ComplaintTextBox.Text = string.IsNullOrEmpty(record.PatientComplaint)
                    ? "Bệnh nhân không có phàn nàn gì."
                    : record.PatientComplaint;

                FindingsTextBox.Text = "";
                TreatmentTextBox.Text = "";

                RecordDetailModal.Visibility = Visibility.Visible;
                PlotEcgData(record.FilePath);
            }
        }

        private async void SubmitFeedback_Click(object sender, RoutedEventArgs e)
        {
            // Nếu chưa có ID ca khám thì không làm gì cả
            if (_currentConsultationId == 0) return;

            // Lấy chữ bác sĩ vừa gõ trên màn hình
            string findings = FindingsTextBox.Text;
            string treatment = TreatmentTextBox.Text;

            if (string.IsNullOrWhiteSpace(findings) || string.IsNullOrWhiteSpace(treatment))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Findings và Treatment!", "Cảnh báo");
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

        private void PlotEcgData(string csvPath)
        {
            try
            {
                if (File.Exists(csvPath))
                {
                    using (var reader = new StreamReader(csvPath))
                    using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                    {
                        var records = csv.GetRecords<CsvDataRow>().ToList();
                        int dataCount = Math.Min(1000, records.Count);
                        double[] ys = new double[dataCount];
                        double[] xs = new double[dataCount];

                        for (int i = 0; i < dataCount; i++)
                        {
                            ys[i] = records[i].oi;
                            // 250Hz sample rate -> 4 seconds for 1000 points
                            xs[i] = i / 250.0;
                        }

                        EcgPlot.Plot.Clear();
                        var sig = EcgPlot.Plot.Add.Scatter(xs, ys);
                        sig.LineWidth = 2;
                        sig.MarkerSize = 0;
                        
                        EcgPlot.Plot.XLabel("Time (s)");
                        EcgPlot.Plot.YLabel("Voltage (mV)");
                        EcgPlot.Plot.Axes.Rules.Add(new ScottPlot.AxisRules.MaximumBoundary(
                            EcgPlot.Plot.Axes.Bottom, 
                            EcgPlot.Plot.Axes.Left, 
                            new ScottPlot.AxisLimits(0, 4, -1, 1)));
                        EcgPlot.Refresh();
                    }
                }
                else
                {
                    MessageBox.Show("data.csv not found.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading plot data: " + ex.Message);
            }
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
        public string GenderAgeText => $"{Gender}, {Age} years old";
    }

    public class EcgRecord
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; }
        public string Date { get; set; }

        // Thuộc tính để hứng dữ liệu API
        public int ConsultationId { get; set; }
        public string PatientComplaint { get; set; }
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
        public string Password { get; set; }
    }
    
    // Class nhận JSON từ API trả về 
    public class ConsultationDto
    {
        public int EcgId { get; set; }
        public string RecordName { get; set; }
        public int ConsultationId { get; set; }
        public string Complaint { get; set; }
        public string Findings { get; set; }
        public string Treatment { get; set; }
        public string Status { get; set; }
    }
    #endregion
}