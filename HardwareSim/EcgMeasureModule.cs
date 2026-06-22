using System;
using System.Collections.Generic;
using System.IO;

namespace HardwareSim
{
    public class EcgMeasurementResult
    {
        public string EcgJson { get; set; } = string.Empty;
        public int HeartRate { get; set; }
        public double Rmssd { get; set; }
    }

    public class EcgMeasureModule
    {
        private readonly List<double> _samplePoints = new List<double>();
        private readonly Random _random = new Random();

        public EcgMeasureModule()
        {
            LoadSampleData();
        }

        /// <summary>
        /// Đọc các điểm tín hiệu ECG thực tế từ tệp tin data.csv mẫu của dự án.
        /// </summary>
        private void LoadSampleData()
        {
            string csvPath = Path.Combine(Directory.GetCurrentDirectory(), "data.csv");
            if (!File.Exists(csvPath))
            {
                csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.csv");
            }

            try
            {
                if (File.Exists(csvPath))
                {
                    string[] lines = File.ReadAllLines(csvPath);
                    // Bỏ qua dòng tiêu đề (Header: xi,oi,qi,envelope,pred_peak_mask)
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var cols = lines[i].Split(',');
                        if (cols.Length >= 2 && double.TryParse(cols[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double oiValue))
                        {
                            _samplePoints.Add(oiValue);
                        }
                    }
                    Console.WriteLine($"[EcgMeasureModule] Loaded {_samplePoints.Count} real ECG sample points from data.csv.");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[EcgMeasureModule] Warning: Failed to load data.csv: {ex.Message}. Using synthetic fallback.");
                Console.ResetColor();
            }

            // Fallback nếu không có tệp hoặc lỗi đọc tệp
            if (_samplePoints.Count == 0)
            {
                for (int i = 0; i < 1000; i++)
                {
                    _samplePoints.Add(0.0);
                }
            }
        }

        /// <summary>
        /// Sinh 1000 điểm mẫu (4 giây ở tần số 250Hz) liên tục bằng cách cắt một đoạn ngẫu nhiên
        /// từ chuỗi mẫu của tệp data.csv để sóng hiển thị chân thực 100%.
        /// </summary>
        public string GenerateEcgJson(int heartRate)
        {
            var signal = new List<int>();
            int totalSamples = 1000; // 4 giây ở tần số 250Hz (250 * 4 = 1000)
            
            // Chọn ngẫu nhiên vị trí bắt đầu trong mảng mẫu để tạo sự khác biệt giữa các ca đo
            int startIndex = _random.Next(0, _samplePoints.Count);

            for (int i = 0; i < totalSamples; i++)
            {
                int index = (startIndex + i) % _samplePoints.Count;
                double baseVal = _samplePoints[index];
                
                // Thêm độ nhiễu cảm biến rất nhỏ (±0.002) để tránh các bản ghi giống hệt nhau
                double noise = (_random.NextDouble() * 0.004) - 0.002;
                
                double sampleVal = baseVal + noise;
                
                // Nhân với 1000 để chuyển đổi thành số nguyên cho tệp JSON tương thích với API
                int intVal = (int)Math.Round(sampleVal * 1000.0);
                signal.Add(intVal);
            }
            
            return $"{{\"signal\": [{string.Join(", ", signal)}]}}";
        }

        public int GenerateHeartRate()
        {
            return _random.Next(60, 91); // 60-90 bpm
        }

        public double GenerateRmssd()
        {
            // RMSSD bình thường dao động từ 25ms đến 55ms
            return _random.NextDouble() * 30.0 + 25.0;
        }

        public EcgMeasurementResult Measure()
        {
            int heartRate = GenerateHeartRate();
            double rmssd = GenerateRmssd();
            string ecgJson = GenerateEcgJson(heartRate);
            
            return new EcgMeasurementResult
            {
                EcgJson = ecgJson,
                HeartRate = heartRate,
                Rmssd = rmssd
            };
        }
    }
}
