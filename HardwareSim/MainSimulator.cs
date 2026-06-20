using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace HardwareSim
{
    public class MainSimulator
    {
        private readonly SimulatorConfig _config;
        private readonly IDeviceApiClient _apiClient;
        private readonly EcgMeasureModule _measureModule;
        private readonly StorageService _storageService;

        public MainSimulator(
            SimulatorConfig config, 
            IDeviceApiClient apiClient, 
            EcgMeasureModule measureModule, 
            StorageService storageService)
        {
            _config = config;
            _apiClient = apiClient;
            _measureModule = measureModule;
            _storageService = storageService;
        }

        public async Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("====================================================");
            Console.WriteLine("🚀 STARTING CONFIGURABLE MAIN SIMULATOR");
            Console.WriteLine($"MAC Address      : {_config.MacAddress}");
            Console.WriteLine($"Patient ID       : {_config.PatientId}");
            Console.WriteLine($"Measure Interval : {_config.MeasureIntervalSeconds}s");
            Console.WriteLine($"Upload Interval  : {_config.UploadIntervalSeconds}s");
            Console.WriteLine($"API Url          : {_config.BaseApiUrl}");
            Console.WriteLine("====================================================\n");
            Console.ResetColor();

            // Bước 1 (Khởi động): Gọi LinkDeviceAsync để mô phỏng tương tác ban đầu
            bool linkSuccess = await _apiClient.LinkDeviceAsync(_config.MacAddress, _config.PatientId);
            if (!linkSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[MainSimulator] Error: Initial device linking failed. Simulator aborted.");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n[MainSimulator] Starting periodic timers...");
            Console.WriteLine($"- Measurement Timer: Runs every {_config.MeasureIntervalSeconds} seconds to read sensor and save locally.");
            Console.WriteLine($"- Upload Timer: Runs every {_config.UploadIntervalSeconds} seconds to scan cache, upload, and clear files.\n");
            Console.ResetColor();

            // Bước 2 (Timer Đo): Định kỳ gọi MeasureModule.Measure(), in log, gọi StorageService.SaveRawData().
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(_config.MeasureIntervalSeconds * 1000);
                        
                        Console.WriteLine($"\n⏱️ [{DateTime.Now:HH:mm:ss}] [MEASURE TIMER] Triggered.");
                        var measurement = _measureModule.Measure();
                        Console.WriteLine($"[MEASURE TIMER] Generated signal data, Heart Rate: {measurement.HeartRate} bpm, RMSSD: {measurement.Rmssd:F1} ms.");
                        
                        // We wrap the ECG signal, measured heart rate, and RMSSD into a single cached JSON
                        string cacheData = $"{{\"heartRate\": {measurement.HeartRate}, \"rmssd\": {measurement.Rmssd.ToString(System.Globalization.CultureInfo.InvariantCulture)}, \"ecgSignal\": {measurement.EcgJson}}}";
                        
                        _storageService.SaveRawData(cacheData);
                        Console.WriteLine("[MEASURE TIMER] Saved measurement data to local cache folder measurements_cache/");
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[MEASURE TIMER ERROR] {ex.Message}");
                        Console.ResetColor();
                    }
                }
            });

            // Bước 3 (Timer Gửi): Định kỳ quét thư mục measurements_cache/.
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(_config.UploadIntervalSeconds * 1000);
                        
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"\n🚀 [{DateTime.Now:HH:mm:ss}] [UPLOAD TIMER] Triggered. Scanning cache directory...");
                        Console.ResetColor();
                        
                        const string cacheDir = "measurements_cache";
                        if (Directory.Exists(cacheDir))
                        {
                            var files = Directory.GetFiles(cacheDir, "*.json");
                            Console.WriteLine($"[UPLOAD TIMER] Found {files.Length} file(s) pending upload.");
                            
                            foreach (var file in files)
                            {
                                string fileName = Path.GetFileName(file);
                                try
                                {
                                    string content = await File.ReadAllTextAsync(file);
                                    
                                    // Parse combined cached JSON to extract the heart rate, rmssd and the signal JSON
                                    using (JsonDocument doc = JsonDocument.Parse(content))
                                    {
                                        JsonElement root = doc.RootElement;
                                        int heartRate = root.GetProperty("heartRate").GetInt32();
                                        double rmssd = root.TryGetProperty("rmssd", out var rmssdEl) ? rmssdEl.GetDouble() : _measureModule.GenerateRmssd();
                                        string signalJson = root.GetProperty("ecgSignal").GetRawText();
                                        
                                        bool uploadSuccess = await _apiClient.UploadDataAsync(_config.MacAddress, signalJson, heartRate, rmssd);
                                        if (uploadSuccess)
                                        {
                                            File.Delete(file);
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.WriteLine($"[UPLOAD TIMER] Successfully uploaded & deleted cached file: {fileName}");
                                            Console.ResetColor();
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.WriteLine($"[UPLOAD TIMER] API upload returned failure for: {fileName}. File retained in cache.");
                                            Console.ResetColor();
                                        }
                                    }
                                }
                                catch (Exception parseEx)
                                {
                                    // Fallback if file is in old format or not nested
                                    Console.WriteLine($"[UPLOAD TIMER] Non-wrapped file format or parsing error on {fileName}: {parseEx.Message}. Retrying as raw JSON...");
                                    try
                                    {
                                        string rawContent = await File.ReadAllTextAsync(file);
                                        int defaultHr = _measureModule.GenerateHeartRate();
                                        double defaultRmssd = _measureModule.GenerateRmssd();
                                        bool uploadSuccess = await _apiClient.UploadDataAsync(_config.MacAddress, rawContent, defaultHr, defaultRmssd);
                                        if (uploadSuccess)
                                        {
                                            File.Delete(file);
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.WriteLine($"[UPLOAD TIMER] Successfully uploaded raw content file & deleted: {fileName}");
                                            Console.ResetColor();
                                        }
                                    }
                                    catch (Exception fallbackEx)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"[UPLOAD TIMER ERROR] Fallback upload failed for {fileName}: {fallbackEx.Message}");
                                        Console.ResetColor();
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("[UPLOAD TIMER] Cache directory does not exist yet.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[UPLOAD TIMER ERROR] {ex.Message}");
                        Console.ResetColor();
                    }
                }
            });
        }
    }
}
