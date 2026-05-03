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

namespace HRMonitor
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Patient> Patients { get; set; }
        public ObservableCollection<EcgRecord> Records { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            LoadDummyData();
            LoadDummyRecords();
            PatientListView.ItemsSource = Patients;
            RecordListView.ItemsSource = Records;
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
                RecordDetailTitle.Text = record.Name;
                RecordDetailModal.Visibility = Visibility.Visible;
                PlotEcgData();
            }
        }

        private void CloseModal_Click(object sender, RoutedEventArgs e)
        {
            RecordDetailModal.Visibility = Visibility.Collapsed;
        }

        private void PlotEcgData()
        {
            try
            {
                string csvPath = "data.csv";
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
    }

    public class CsvDataRow
    {
        public double xi { get; set; }
        public double oi { get; set; }
        public double qi { get; set; }
        public double envelope { get; set; }
        public int pred_peak_mask { get; set; }
    }
}