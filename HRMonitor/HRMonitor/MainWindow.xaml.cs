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
        public ObservableCollection<EcgRecord> Records { get; set; }

        //  Khởi tạo HttpClient để gọi C# Web API cổng 5000
        private static readonly HttpClient _httpClient = new HttpClient();

        //nhớ xem Bác sĩ đang xem ca khám nào
        private int _currentConsultationId = 0;

        private HubConnection _hubConnection;
        public MainWindow()
        {
            InitializeComponent();
            LoadDummyData();
            LoadDummyRecords();

            Records = new ObservableCollection<EcgRecord>();
            PatientListView.ItemsSource = Patients;
            RecordListView.ItemsSource = Records;

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
                                Name = r.RecordName,
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
                DoctorId = 1, 
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



        private void LoadDummyRecords()
        {
            Records = new ObservableCollection<EcgRecord>();
            for (int i = 1; i <= 15; i++)
            {
                Records.Add(new EcgRecord
                {
                    Id = i,
                    Name = $"ecg_record_{i}.json",
                    Date = "2026-03-30 " + (8 + (i * 3) / 60).ToString("D2") + ":" + ((i * 3) % 60).ToString("D2")
                });
            }
        }

        private void LoadDummyData()
        {
            Patients = new ObservableCollection<Patient>
            {
                new Patient { Name = "Nguyen Van A", Age = 45, Gender = "Male", PhoneNumber = "0901234567", Email = "nva@example.com", Address = "123 Le Loi, District 1, HCMC" },
                new Patient { Name = "Tran Thi B", Age = 32, Gender = "Female", PhoneNumber = "0912345678", Email = "ttb@example.com", Address = "456 Nguyen Hue, District 1, HCMC" },
                new Patient { Name = "Le Van C", Age = 58, Gender = "Male", PhoneNumber = "0923456789", Email = "lvc@example.com", Address = "789 Tran Hung Dao, District 5, HCMC" },
                new Patient { Name = "Pham Thi D", Age = 27, Gender = "Female", PhoneNumber = "0934567890", Email = "ptd@example.com", Address = "101 Nguyen Trai, District 1, HCMC" },
                new Patient { Name = "Hoang Van E", Age = 64, Gender = "Male", PhoneNumber = "0945678901", Email = "hve@example.com", Address = "202 Le Duan, District 1, HCMC" }
            };
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (IdTextBox.Text == "1" && PasswordBox.Password == "1")
            {
                LoginGrid.Visibility = Visibility.Collapsed;
                DashboardGrid.Visibility = Visibility.Visible;
                LoginErrorText.Visibility = Visibility.Collapsed;
            }
            else
            {
                LoginErrorText.Visibility = Visibility.Visible;
            }
        }

        private void ViewProfile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Patient patient)
            {
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
            }
        }

        private void ViewRecord_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Patient patient)
            {
                PatientListGrid.Visibility = Visibility.Collapsed;
                RightProfilePanel.Visibility = Visibility.Collapsed;
                Grid.SetColumnSpan(LeftAreaGrid, 2);
                RecordListGrid.Visibility = Visibility.Visible;

                // Cập nhật lại data từ DB khi ấn View Bệnh Nhân
                _ = LoadDataFromApiAsync(1);
            }
        }
        private void BackToPatients_Click(object sender, RoutedEventArgs e)
        {
            RecordListGrid.Visibility = Visibility.Collapsed;
            Grid.SetColumnSpan(LeftAreaGrid, 1);
            RightProfilePanel.Visibility = Visibility.Visible;
            PatientListGrid.Visibility = Visibility.Visible;
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
                PlotEcgData();
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

        private void PlotEcgData()
        {
            try
            {
                string csvPath = "D:\\PROGRAM\\KTPM_PROJECT\\KTPM_web\\HRMonitor\\HRMonitor\\data.csv";  //Đổi lại theo vị trị file CSV của bạn
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
        public string Name { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string GenderAgeText => $"{Gender}, {Age} years old";
    }

    public class EcgRecord
    {
        public int Id { get; set; }
        public string Name { get; set; }
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